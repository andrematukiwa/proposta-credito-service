using MassTransit;
using PropostaCredito.Application.Contracts;
using PropostaCredito.Application.Propostas.Services;

namespace PropostaCredito.Infrastructure.Messaging.Consumers;

/// <summary>Adaptador fino: recebe a mensagem e delega ao caso de uso, sem regra de negócio.</summary>
internal sealed class ClienteCadastradoConsumer(IPropostaService propostaService) : IConsumer<ClienteCadastrado>
{
    public Task Consume(ConsumeContext<ClienteCadastrado> context) =>
        propostaService.GerarAsync(context.Message, context.CancellationToken);
}
