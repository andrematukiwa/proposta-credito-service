using PropostaCredito.Application.Common.Abstractions;
using MassTransit;

namespace PropostaCredito.Infrastructure.Messaging;

/// <summary>
/// Com o Bus Outbox ativo, o IPublishEndpoint com escopo da requisição não envia direto ao
/// RabbitMQ: grava a mensagem no Outbox do DbContext, e ela só é entregue após o commit.
/// </summary>
internal sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(message, cancellationToken);
}
