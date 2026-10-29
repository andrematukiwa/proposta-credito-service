using MassTransit;
using PropostaCredito.Application.Common.Exceptions;
using PropostaCredito.Infrastructure.Persistence;

namespace PropostaCredito.Infrastructure.Messaging.Consumers;

/// <summary>
/// Resiliência do consumo de ClienteCadastrado:
/// <list type="bullet">
///   <item><b>Retry exponencial</b>: 5 retentativas com intervalos crescentes (de 1s até no máximo 30s)
///   para falhas transitórias, como o banco fora do ar por alguns segundos.</item>
///   <item><b>Dead-letter queue</b>: esgotadas as retentativas, o MassTransit move a mensagem para
///   <c>proposta-cliente-cadastrado_error</c>, com o motivo da falha nos headers. Nada se perde, e a
///   mensagem pode ser reprocessada depois.</item>
///   <item><b>Mensagem inválida</b> não é retentada: vai direto para a DLQ.</item>
///   <item><b>Inbox + Outbox</b>: reentregas são descartadas e PropostaGerada só é publicada se a
///   proposta for gravada.</item>
/// </list>
/// </summary>
internal sealed class ClienteCadastradoConsumerDefinition : ConsumerDefinition<ClienteCadastradoConsumer>
{
    public const string Fila = "proposta-cliente-cadastrado";

    public ClienteCadastradoConsumerDefinition() => EndpointName = Fila;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ClienteCadastradoConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(2));

            retry.Ignore<MensagemInvalidaException>();
        });

        endpointConfigurator.UseEntityFrameworkOutbox<PropostaDbContext>(context);
    }
}
