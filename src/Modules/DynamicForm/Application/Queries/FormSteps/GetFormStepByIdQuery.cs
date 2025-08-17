using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.FormSteps
{
    /// <summary>
    /// Query for retrieving a form step by its unique identifier.
    /// </summary>
    public class GetFormStepByIdQuery : IRequest<FormStepDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include inactive steps.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }
}