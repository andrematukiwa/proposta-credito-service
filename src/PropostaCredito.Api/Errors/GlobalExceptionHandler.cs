using PropostaCredito.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PropostaCredito.Api.Errors;

/// <summary>
/// Traduz exceções em respostas ProblemDetails (RFC 9457). Erros de negócio viram 4xx com a
/// mensagem original; qualquer outra exceção vira 500 sem expor detalhes internos.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var path = httpContext.Request.Path.ToString();

        // O cliente desistiu da requisição: não é falha do servidor, então nada de log de erro
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequisicaoCancelada(logger, path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
            LogErroNaoTratado(logger, exception, path);
        else
            LogErroDeNegocio(logger, status, path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Ocorreu um erro inesperado."
                : exception.Message
        };

        httpContext.Response.StatusCode = status;

        // Título, instance e traceId são preenchidos em ErrorHandlingExtensions (CustomizeProblemDetails)
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro não tratado em {Path}")]
    private static partial void LogErroNaoTratado(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Requisição recusada com {Status} em {Path}: {Motivo}")]
    private static partial void LogErroDeNegocio(ILogger logger, int status, string path, string motivo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Requisição cancelada pelo cliente em {Path}")]
    private static partial void LogRequisicaoCancelada(ILogger logger, string path);
}
