using PropostaCredito.Domain.Propostas;

namespace PropostaCredito.Application.Propostas.Dtos;

public sealed record PropostaResponse(
    Guid Id,
    Guid ClienteId,
    string NomeCliente,
    int Score,
    string Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    decimal LimiteTotal,
    DateTime CriadaEm)
{
    public static PropostaResponse De(Proposta proposta) => new(
        proposta.Id,
        proposta.ClienteId,
        proposta.NomeCliente,
        proposta.Score,
        proposta.Status.ToString(),
        proposta.QuantidadeCartoes,
        proposta.LimitePorCartao,
        proposta.LimiteTotal,
        proposta.CriadaEm);
}
