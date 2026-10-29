using Microsoft.AspNetCore.Mvc;
using PropostaCredito.Application.Propostas.Dtos;
using PropostaCredito.Application.Propostas.Services;

namespace PropostaCredito.Api.Controllers;

[ApiController]
[Route("api/v1/propostas")]
public sealed class PropostasController(IPropostaService propostaService) : ControllerBase
{
    /// <summary>
    /// Consulta a proposta de crédito de um cliente. A proposta é gerada de forma assíncrona após
    /// o cadastro (consistência eventual): logo após cadastrar, pode ainda não existir (404).
    /// </summary>
    [HttpGet("{clienteId:guid}")]
    [ProducesResponseType<PropostaResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PropostaResponse>> ObterPorClienteId(Guid clienteId, CancellationToken cancellationToken) =>
        Ok(await propostaService.ObterPorClienteIdAsync(clienteId, cancellationToken));
}
