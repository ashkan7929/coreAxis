using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

[ApiController]
[Route("api/admin/forms/field-types")]
[Authorize]
public class FieldTypesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetFieldTypes()
    {
        var types = Enum.GetValues<FieldType>();
        var result = types.Select(t => new
        {
            Type = t.ToString(),
            Label = t.ToString(), // Could use DisplayAttribute if available
            DefaultConfig = GetDefaultConfig(t),
            ValidationRules = GetAllowedValidationRules(t)
        });
        
        return Ok(result);
    }

    private object GetDefaultConfig(FieldType type)
    {
        // Return a basic schema/config object for the frontend designer
        return new 
        {
            label = "New " + type,
            placeholder = "",
            defaultValue = GetDefaultValueForType(type),
            isRequired = false,
            isVisible = true,
            isReadOnly = false
        };
    }
    
    private object? GetDefaultValueForType(FieldType type)
    {
        return type switch
        {
            FieldType.Boolean => false,
            FieldType.Number => 0,
            _ => null
        };
    }

    private string[] GetAllowedValidationRules(FieldType type)
    {
        var common = new[] { "required" };
        return type switch
        {
            FieldType.Text or FieldType.Email or FieldType.Password or FieldType.Textarea => 
                common.Concat(new[] { "minLength", "maxLength", "pattern" }).ToArray(),
            FieldType.Number => 
                common.Concat(new[] { "min", "max" }).ToArray(),
            FieldType.Date or FieldType.DateTime => 
                common.Concat(new[] { "minDate", "maxDate" }).ToArray(),
            _ => common
        };
    }
}
