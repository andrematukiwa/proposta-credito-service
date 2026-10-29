using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PropostaCredito.Application.Common.Abstractions;
using PropostaCredito.Application.Propostas.Repositories;
using PropostaCredito.Infrastructure.Messaging;
using PropostaCredito.Infrastructure.Messaging.Consumers;
using PropostaCredito.Infrastructure.Persistence;
using PropostaCredito.Infrastructure.Propostas;

namespace PropostaCredito.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "PropostaDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Falha ao subir se a connection string não estiver configurada (fail-fast)
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada (ConnectionStrings__{ConnectionStringName}).");

        services.AddDbContext<PropostaDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        // Mesma instância (por mensagem/requisição) do DbContext: repositório e Outbox na mesma transação
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PropostaDbContext>());
        services.AddScoped<IPropostaRepository, PropostaRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<PropostaDbContext>("postgres");
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<ClienteCadastradoConsumer, ClienteCadastradoConsumerDefinition>();

            // Entrega em segundo plano as mensagens gravadas no Outbox pelos consumers
            bus.AddEntityFrameworkOutbox<PropostaDbContext>(outbox => outbox.UsePostgres());

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitMq.Host, rabbitMq.Port, rabbitMq.VirtualHost, host =>
                {
                    host.Username(rabbitMq.Username);
                    host.Password(rabbitMq.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
