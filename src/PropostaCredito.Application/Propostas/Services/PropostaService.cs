using Microsoft.Extensions.Logging;
using PropostaCredito.Application.Common.Abstractions;
using PropostaCredito.Application.Common.Exceptions;
using PropostaCredito.Application.Contracts;
using PropostaCredito.Application.Propostas.Dtos;
using PropostaCredito.Application.Propostas.Repositories;
using PropostaCredito.Domain.Propostas;
using PropostaCredito.Domain.Score;

namespace PropostaCredito.Application.Propostas.Services;

public sealed partial class PropostaService(
    IPropostaRepository repository,
    IScoreCalculator scoreCalculator,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PropostaService> logger) : IPropostaService
{
    public async Task GerarAsync(ClienteCadastrado evento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ValidarMensagem(evento);

        // Idempotência de negócio: o mesmo cliente nunca recebe duas propostas, mesmo que o evento
        // seja republicado. (Reentregas da mesma mensagem já são barradas pelo Inbox do MassTransit.)
        if (await repository.ExistePorClienteIdAsync(evento.ClienteId, cancellationToken))
        {
            LogPropostaJaExistente(logger, evento.ClienteId);
            return;
        }

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        var score = scoreCalculator.Calcular(
            new DadosScore(evento.RendaMensal, evento.DataNascimento, DateOnly.FromDateTime(agora)));

        var proposta = Proposta.Gerar(evento.ClienteId, evento.Nome, score, agora);

        repository.Adicionar(proposta);
        await eventPublisher.PublishAsync(
            new PropostaGerada(
                proposta.Id,
                proposta.ClienteId,
                proposta.NomeCliente,
                proposta.Score,
                proposta.Status.ToString(),
                proposta.QuantidadeCartoes,
                proposta.LimitePorCartao,
                proposta.CriadaEm),
            cancellationToken);

        // Proposta e evento (Outbox) são gravados na mesma transação
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogPropostaGerada(logger, proposta.Id, proposta.ClienteId, proposta.Score, proposta.Status);
    }

    public async Task<PropostaResponse> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var proposta = await repository.ObterPorClienteIdAsync(clienteId, cancellationToken)
            ?? throw new NotFoundException(
                $"Nenhuma proposta encontrada para o cliente {clienteId}. " +
                "Se o cadastro acabou de ser feito, a proposta pode ainda estar em processamento.");

        return PropostaResponse.De(proposta);
    }

    /// <summary>Dados que nenhuma retentativa vai corrigir: vão direto para a DLQ.</summary>
    private static void ValidarMensagem(ClienteCadastrado evento)
    {
        if (evento.ClienteId == Guid.Empty)
            throw new MensagemInvalidaException("ClienteCadastrado sem ClienteId.");

        if (string.IsNullOrWhiteSpace(evento.Nome) || evento.Nome.Trim().Length > Proposta.NomeClienteTamanhoMaximo)
            throw new MensagemInvalidaException($"ClienteCadastrado {evento.ClienteId} com nome inválido.");

        if (evento.RendaMensal <= 0)
            throw new MensagemInvalidaException($"ClienteCadastrado {evento.ClienteId} com renda inválida.");

        if (evento.DataNascimento == default)
            throw new MensagemInvalidaException($"ClienteCadastrado {evento.ClienteId} sem data de nascimento.");
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Proposta {PropostaId} gerada para o cliente {ClienteId}: score {Score}, {Status}")]
    private static partial void LogPropostaGerada(ILogger logger, Guid propostaId, Guid clienteId, int score, StatusProposta status);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cliente {ClienteId} já possui proposta; evento ignorado (idempotência)")]
    private static partial void LogPropostaJaExistente(ILogger logger, Guid clienteId);
}
