using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropostaCredito.Application.Propostas.Services;
using PropostaCredito.Domain.Score;

namespace PropostaCredito.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IScoreCalculator, RendaIdadeScoreCalculator>();
        services.AddScoped<IPropostaService, PropostaService>();

        return services;
    }
}
