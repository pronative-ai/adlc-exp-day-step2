using Microsoft.AspNetCore.Mvc;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Controllers;

[ApiController]
[Route("api/currency")]
public sealed class CurrencyController : ControllerBase
{
    private const string DefaultTenantId = "default";

    private readonly CurrencyConversionService _conversionService;

    public CurrencyController(CurrencyConversionService conversionService)
    {
        _conversionService = conversionService;
    }

    [HttpPost("convert")]
    [ProducesResponseType(typeof(ConversionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Convert(
        [FromBody] ConversionRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _conversionService.ConvertAsync(
                request, ResolveTenantId(tenantId), cancellationToken);
            return Ok(result);
        }
        catch (InvalidConversionException ex)
        {
            return BadRequest(CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidConversion", ex.Message));
        }
        catch (RateProviderUnavailableException ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                CreateProblemDetails(StatusCodes.Status503ServiceUnavailable, "RateProviderUnavailable", ex.Message));
        }
    }

    [HttpGet("audit/{auditId}")]
    [ProducesResponseType(typeof(AuditRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditRecord(
        string auditId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken)
    {
        var record = await _conversionService.GetAuditAsync(
            ResolveTenantId(tenantId), auditId, cancellationToken);

        if (record is null)
        {
            return NotFound(CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "AuditRecordNotFound",
                $"No audit record found for id '{auditId}'."));
        }

        return Ok(record);
    }

    private static string ResolveTenantId(string? tenantId)
        => string.IsNullOrWhiteSpace(tenantId) ? DefaultTenantId : tenantId.Trim();

    private ProblemDetails CreateProblemDetails(int status, string title, string detail)
        => ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title: title, detail: detail);
}
