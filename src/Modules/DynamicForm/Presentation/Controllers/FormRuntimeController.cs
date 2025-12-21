using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
public class FormRuntimeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FormRuntimeController> _logger;

    public FormRuntimeController(IMediator mediator, ILogger<FormRuntimeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Evaluate form logic (calculations, visibility, validation) without submitting.
    /// </summary>
    [HttpPost("{formId}/evaluate")]
    public async Task<IActionResult> EvaluateForm([FromRoute] Guid formId, [FromBody] EvaluateFormRequest request, CancellationToken cancellationToken)
    {
        var command = new EvaluateFormCommand
        {
            FormId = formId,
            Context = request.Context ?? new(),
            CurrentData = request.CurrentData ?? new()
        };

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Errors);
    }
}

public class EvaluateFormRequest
{
    public Dictionary<string, object>? Context { get; set; }
    public Dictionary<string, object>? CurrentData { get; set; }
}
