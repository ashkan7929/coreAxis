using CoreAxis.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.Forms;

public class PrefillFormCommand : IRequest<Result<Dictionary<string, object>>>
{
    public Guid FormId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public List<PrefillStepDto> Steps { get; set; } = new();
}

public class PrefillStepDto
{
    public Guid ApiMethodId { get; set; }
    public Guid MappingId { get; set; }
    public Dictionary<string, object> InputContext { get; set; } = new();
}
