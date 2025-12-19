using CoreAxis.Modules.ProductOrderModule.Application.Commands.Quotes;
using CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(QuoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequestDto request)
    {
        var command = new CreateQuoteCommand(request.AssetCode, System.Text.Json.JsonSerializer.Serialize(request.ApplicationData));
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost("{quoteId}/pay")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiatePayment(Guid quoteId)
    {
        var command = new InitiatePaymentCommand(quoteId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { redirectUrl = result.Value });
    }
}
