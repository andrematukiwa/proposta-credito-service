using PropostaCredito.Application.Contracts;
using PropostaCredito.Application.Propostas.Dtos;

namespace PropostaCredito.Application.Propostas.Services;

public interface IPropostaService
{
    /// <summary>Gera a proposta de um cliente recém-cadastrado. Idempotente: reprocessar o mesmo cliente não duplica.</summary>
    Task GerarAsync(ClienteCadastrado evento, CancellationToken cancellationToken = default);

    Task<PropostaResponse> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
