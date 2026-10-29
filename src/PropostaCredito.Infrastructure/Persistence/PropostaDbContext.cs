using MassTransit;
using Microsoft.EntityFrameworkCore;
using PropostaCredito.Application.Common.Abstractions;

namespace PropostaCredito.Infrastructure.Persistence;

public sealed class PropostaDbContext(DbContextOptions<PropostaDbContext> options) : DbContext(options), IUnitOfWork
{
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropostaDbContext).Assembly);

        // Inbox: descarta reentregas da mesma mensagem (MessageId já processado).
        // Outbox: PropostaGerada é gravada na mesma transação da proposta e publicada após o commit.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
