using CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackRequest request)
    {
        var command = new PaymentCallbackCommand(request.QuoteId, request.Success, request.TransactionId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }

        return Ok("Payment processed successfully");
    }
}

public class PaymentCallbackRequest
{
    public Guid QuoteId { get; set; }
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}
