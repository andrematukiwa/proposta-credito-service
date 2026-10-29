using MassTransit;

namespace PropostaCredito.Application.Contracts;

/// <summary>
/// Evento consumido: publicado pelo Cadastro de Clientes.
/// <para>
/// Cópia local do contrato, sem pacote compartilhado entre os serviços. O que liga as duas
/// pontas é a URN/exchange "cliente-cadastrado" e os nomes dos campos, não o namespace C#.
/// </para>
/// </summary>
[MessageUrn("cliente-cadastrado")]
[EntityName("cliente-cadastrado")]
public sealed record ClienteCadastrado(
    Guid ClienteId,
    string Nome,
    DateOnly DataNascimento,
    decimal RendaMensal,
    DateTime CriadoEm);
