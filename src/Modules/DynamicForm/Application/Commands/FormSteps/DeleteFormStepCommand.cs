using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps
{
    /// <summary>
    /// Command for deleting a form step.
    /// </summary>
    public class DeleteFormStepCommand : IRequest<bool>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step to delete.
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
        /// Gets or sets the user who is deleting the step.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Deleted by cannot exceed 100 characters.")]
        public string DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a hard delete (permanent removal).
        /// If false, performs a soft delete by setting IsActive to false.
        /// </summary>
        public bool HardDelete { get; set; } = false;
    }
}