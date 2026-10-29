namespace PropostaCredito.Application.Common.Abstractions;

/// <summary>
/// Publica eventos de integração. A implementação grava no Outbox: o evento só sai para o
/// RabbitMQ depois que <see cref="IUnitOfWork.SaveChangesAsync"/> confirma a transação.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : class;
}
