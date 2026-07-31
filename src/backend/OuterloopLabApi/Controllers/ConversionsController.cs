using Microsoft.AspNetCore.Mvc;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using System.Net.Mime;

namespace OuterloopLabApi.Controllers;

[ApiController]
[Route("api/conversions")]
public sealed class ConversionsController : ControllerBase
{
  private readonly CurrencyConversionService _service;

  public ConversionsController(CurrencyConversionService service)
  {
    _service = service;
  }

  [HttpPost]
  [Consumes(MediaTypeNames.Application.Json)]
  [ProducesResponseType(typeof(ConversionResultResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<ConversionResultResponse>> Create([FromBody] ConversionRequest? request)
  {
    if (request is null)
      return BadRequest(CreateProblem("Invalid request payload.", StatusCodes.Status400BadRequest));

    if (string.IsNullOrWhiteSpace(request.FromCurrency) || string.IsNullOrWhiteSpace(request.ToCurrency))
      return BadRequest(CreateProblem("Currency codes are required.", StatusCodes.Status400BadRequest));

    var from = request.FromCurrency.Trim().ToUpperInvariant();
    var to = request.ToCurrency.Trim().ToUpperInvariant();

    if (request.Amount <= 0)
      return BadRequest(CreateProblem("Amount must be greater than 0.", StatusCodes.Status400BadRequest));

    if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
      return BadRequest(CreateProblem("From and To currency must be different.", StatusCodes.Status400BadRequest));

    try
    {
      var result = await _service.ConvertAsync(new ConversionRequest(request.Amount, from, to));
      return Ok(result);
    }
    catch (CurrencyRateParseException)
    {
      return StatusCode(
        StatusCodes.Status503ServiceUnavailable,
        CreateProblem("Currency provider returned an unusable payload.", StatusCodes.Status503ServiceUnavailable));
    }
    catch (CurrencyRateProviderUnavailableException)
    {
      return StatusCode(
        StatusCodes.Status503ServiceUnavailable,
        CreateProblem("Currency provider is unavailable.", StatusCodes.Status503ServiceUnavailable));
    }
  }

  [HttpGet("{auditId}")]
  [ProducesResponseType(typeof(ConversionResultResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<ConversionResultResponse>> Get(string auditId)
  {
    if (string.IsNullOrWhiteSpace(auditId))
      return BadRequest(CreateProblem("auditId is required.", StatusCodes.Status400BadRequest));

    var record = await _service.GetByAuditIdAsync(auditId);
    if (record is null)
      return NotFound(CreateProblem("Audit record not found.", StatusCodes.Status404NotFound));

    return Ok(record);
  }

  private static ProblemDetails CreateProblem(string detail, int status)
  {
    return new ProblemDetails
    {
      Type = "about:blank",
      Title = "Request could not be completed.",
      Status = status,
      Detail = detail
    };
  }
}
