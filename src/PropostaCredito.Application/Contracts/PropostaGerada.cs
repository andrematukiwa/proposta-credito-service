using MassTransit;

namespace PropostaCredito.Application.Contracts;

/// <summary>
/// Evento publicado para toda proposta gerada, aprovada ou negada. Quem decide se há cartão
/// a emitir é o consumidor (Cartão de Crédito), pelo <see cref="Status"/> e pela quantidade.
/// Publicar também as negadas deixa o evento útil para auditoria e futuros consumidores.
/// </summary>
/// <param name="Status">"Aprovada" ou "Negada". String, e não enum, para o contrato não depender
/// de um tipo C# compartilhado entre os serviços.</param>
[MessageUrn("proposta-gerada")]
[EntityName("proposta-gerada")]
public sealed record PropostaGerada(
    Guid PropostaId,
    Guid ClienteId,
    string NomeCliente,
    int Score,
    string Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    DateTime GeradaEm);
