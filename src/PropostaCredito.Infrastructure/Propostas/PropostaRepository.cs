using Microsoft.EntityFrameworkCore;
using PropostaCredito.Application.Propostas.Repositories;
using PropostaCredito.Domain.Propostas;
using PropostaCredito.Infrastructure.Persistence;

namespace PropostaCredito.Infrastructure.Propostas;

internal sealed class PropostaRepository(PropostaDbContext context) : IPropostaRepository
{
    public Task<Proposta?> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        context.Set<Proposta>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClienteId == clienteId, cancellationToken);

    public Task<bool> ExistePorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        context.Set<Proposta>().AnyAsync(p => p.ClienteId == clienteId, cancellationToken);

    public void Adicionar(Proposta proposta) => context.Set<Proposta>().Add(proposta);
}
