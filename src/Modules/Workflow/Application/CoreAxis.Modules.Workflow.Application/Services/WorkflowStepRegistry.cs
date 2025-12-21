using CoreAxis.Modules.Workflow.Application.DTOs;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services;

public class WorkflowStepRegistry : IWorkflowStepRegistry
{
    private readonly List<WorkflowStepTypeDescriptor> _stepTypes;

    public WorkflowStepRegistry()
    {
        _stepTypes = new List<WorkflowStepTypeDescriptor>
        {
            CreateDescriptor("FormStep", "Form Task", "User input form", "Human Task", 
                new { type = "object", properties = new { formId = new { type = "string" }, assignee = new { type = "string" } } },
                producedPaths: new[] { "forms.{formId}" },
                signals: new[] { "FormSubmitted" }),
            
            CreateDescriptor("HumanTaskStep", "Manual Task", "General manual task", "Human Task",
                new { type = "object", properties = new { instructions = new { type = "string" }, assignee = new { type = "string" } } },
                signals: new[] { "HumanTaskCompleted" }),

            CreateDescriptor("ServiceTaskStep", "Service Task", "Call external service", "System",
                new { type = "object", properties = new { serviceUrl = new { type = "string" }, method = new { type = "string", @enum = new[] { "GET", "POST" } } } }),

            CreateDescriptor("DecisionStep", "Decision", "Conditional branching", "Logic",
                new { type = "object", properties = new { condition = new { type = "string" } } }),

            CreateDescriptor("CalculationStep", "Calculation", "Perform calculations", "Logic",
                new { type = "object", properties = new { expression = new { type = "string" }, outputVariable = new { type = "string" } } }),

            CreateDescriptor("WaitForEventStep", "Wait for Event", "Pause until event received", "Events",
                new { type = "object", properties = new { eventName = new { type = "string" }, timeoutSeconds = new { type = "integer" } } },
                signals: new[] { "{eventName}" }),

            CreateDescriptor("TimerStep", "Timer", "Wait for a duration", "Events",
                new { type = "object", properties = new { duration = new { type = "string" } } },
                signals: new[] { "Timer_{stepId}" }),
                
            CreateDescriptor("CompensationStep", "Compensation", "Compensate previous steps", "Error Handling",
                new { type = "object", properties = new { targetStepId = new { type = "string" } } })
        };
    }

    private WorkflowStepTypeDescriptor CreateDescriptor(string type, string name, string description, string category, object schema, string[]? requiredPaths = null, string[]? producedPaths = null, string[]? signals = null)
    {
        return new WorkflowStepTypeDescriptor
        {
            Type = type,
            Name = name,
            Description = description,
            Category = category,
            ConfigSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(schema)),
            RequiredContextPaths = requiredPaths?.ToList() ?? new List<string>(),
            ProducedContextPaths = producedPaths?.ToList() ?? new List<string>(),
            PauseSignalNames = signals?.ToList() ?? new List<string>()
        };
    }

    public IReadOnlyList<WorkflowStepTypeDescriptor> GetAllStepTypes()
    {
        return _stepTypes.AsReadOnly();
    }

    public WorkflowStepTypeDescriptor? GetStepType(string type)
    {
        return _stepTypes.FirstOrDefault(s => s.Type == type);
    }
}
