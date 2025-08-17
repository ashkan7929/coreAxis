using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.FormSteps
{
    /// <summary>
    /// Query for retrieving all form steps for a specific form.
    /// </summary>
    public class GetFormStepsByFormIdQuery : IRequest<IEnumerable<FormStepDto>>
    {
        /// <summary>
        /// Gets or sets the form identifier.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether to order steps by step number.
        /// </summary>
        public bool OrderByStepNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include only required steps.
        /// </summary>
        public bool RequiredOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include only skippable steps.
        /// </summary>
        public bool SkippableOnly { get; set; } = false;
    }
}