using PropostaCredito.Domain.Propostas;

namespace PropostaCredito.Application.Propostas.Repositories;

public interface IPropostaRepository
{
    Task<Proposta?> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);

    Task<bool> ExistePorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);

    /// <summary>Marca a proposta para inclusão; a gravação acontece em <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    void Adicionar(Proposta proposta);
}
