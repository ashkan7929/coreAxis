using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.Forms
{
    /// <summary>
    /// Query to get form event handlers.
    /// </summary>
    public class GetFormEventHandlersQuery : IRequest<Result<FormEventHandlersDto>>
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }
    }

    /// <summary>
    /// DTO for form event handlers information.
    /// </summary>
    public class FormEventHandlersDto
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the form-specific handlers.
        /// </summary>
        public List<FormEventHandlerInfo> FormHandlers { get; set; } = new();

        /// <summary>
        /// Gets or sets the global handlers.
        /// </summary>
        public List<FormEventHandlerInfo> GlobalHandlers { get; set; } = new();

        /// <summary>
        /// Gets or sets the total number of handlers.
        /// </summary>
        public int TotalHandlers { get; set; }
    }

    /// <summary>
    /// Information about a form event handler.
    /// </summary>
    public class FormEventHandlerInfo
    {
        /// <summary>
        /// Gets or sets the handler type name.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the handler assembly name.
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this is a global handler.
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Gets or sets the handler description (if available).
        /// </summary>
        public string? Description { get; set; }
    }
}