using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.Forms;

public class EvaluateFormCommand : IRequest<Result<FormEvaluationResultDto>>
{
    public Guid FormId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public Dictionary<string, object> CurrentData { get; set; } = new();
}

public class FormEvaluationResultDto
{
    public Dictionary<string, object> Values { get; set; } = new();
    public Dictionary<string, bool> Visible { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
