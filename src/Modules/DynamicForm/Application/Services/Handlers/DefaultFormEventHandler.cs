using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services.Handlers
{
    /// <summary>
    /// Default implementation of form event handler that provides basic functionality.
    /// </summary>
    [Description("Default form event handler that provides basic validation and logging functionality.")]
    public class DefaultFormEventHandler : FormEventHandlerBase
    {
        private readonly ILogger<DefaultFormEventHandler> _logger;

        public DefaultFormEventHandler(ILogger<DefaultFormEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public override async Task<FormEventResult> OnInitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Form {FormId} initialized by user {UserId}", context.FormId, context.UserId);

            // Perform basic initialization logic
            var result = new FormEventResult
            {
                Success = true,
                Message = "Form initialized successfully",
                UpdateFormData = false
            };

            // Add initialization timestamp to metadata
            if (!context.Metadata.ContainsKey("InitializedAt"))
            {
                context.Metadata["InitializedAt"] = DateTime.UtcNow;
            }

            return await Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override async Task<FormEventResult> OnChangeAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Form {FormId} field {FieldName} changed by user {UserId}", 
                context.FormId, context.ChangedField, context.UserId);

            var result = new FormEventResult
            {
                Success = true,
                Message = $"Field '{context.ChangedField}' changed successfully",
                UpdateFormData = false
            };

            // Perform basic field validation
            if (!string.IsNullOrEmpty(context.ChangedField) && context.FormData.ContainsKey(context.ChangedField))
            {
                var fieldValue = context.FormData[context.ChangedField];
                
                // Example: Basic required field validation
                if (IsRequiredField(context.ChangedField) && (fieldValue == null || string.IsNullOrWhiteSpace(fieldValue.ToString())))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Field '{context.ChangedField}' is required";
                    result.ValidationErrors = new List<string> { "This field is required" };
                }
            }

            return await Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override async Task<FormEventResult> BeforeSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Form {FormId} about to be submitted by user {UserId}", context.FormId, context.UserId);

            var result = new FormEventResult
            {
                Success = true,
                Message = "Pre-submission validation passed",
                UpdateFormData = false
            };

            var validationErrors = new Dictionary<string, List<string>>();

            // Perform comprehensive form validation before submission
            foreach (var field in context.FormData)
            {
                var fieldErrors = ValidateField(field.Key, field.Value);
                if (fieldErrors.Any())
                {
                    validationErrors[field.Key] = fieldErrors;
                }
            }

            if (validationErrors.Any())
            {
                result.Success = false;
                result.ErrorMessage = "Form validation failed";
                result.ValidationErrors = validationErrors.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}")).ToList();
                
                // Cancel submission if validation fails
                context.Cancel = true;
                context.CancellationReason = "Form validation failed";
            }
            else
            {
                // Add submission metadata
                context.Metadata["PreSubmissionValidatedAt"] = DateTime.UtcNow;
                context.Metadata["ValidatedBy"] = nameof(DefaultFormEventHandler);
            }

            return await Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override async Task<FormEventResult> AfterSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Form {FormId} submitted successfully by user {UserId}", context.FormId, context.UserId);

            var result = new FormEventResult
            {
                Success = true,
                Message = "Form submitted successfully",
                UpdateFormData = false
            };

            // Add post-submission metadata
            context.Metadata["SubmittedAt"] = DateTime.UtcNow;
            context.Metadata["ProcessedBy"] = nameof(DefaultFormEventHandler);

            // Log submission for audit purposes
            _logger.LogInformation("Form submission completed for form {FormId} with {FieldCount} fields", 
                context.FormId, context.FormData.Count);

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Determines if a field is required.
        /// This is a basic implementation - in a real scenario, this would check the form schema.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>True if the field is required.</returns>
        private static bool IsRequiredField(string fieldName)
        {
            // Basic implementation - in reality, this would check the form schema
            var requiredFields = new[] { "email", "name", "title", "firstName", "lastName" };
            return requiredFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates a single field value.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <returns>List of validation errors.</returns>
        private static List<string> ValidateField(string fieldName, object? fieldValue)
        {
            var errors = new List<string>();

            // Basic validation rules
            if (IsRequiredField(fieldName) && (fieldValue == null || string.IsNullOrWhiteSpace(fieldValue.ToString())))
            {
                errors.Add("This field is required");
            }

            // Email validation
            if (fieldName.Equals("email", StringComparison.OrdinalIgnoreCase) && fieldValue != null)
            {
                var email = fieldValue.ToString();
                if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
                {
                    errors.Add("Please enter a valid email address");
                }
            }

            return errors;
        }

        /// <summary>
        /// Basic email validation.
        /// </summary>
        /// <param name="email">The email to validate.</param>
        /// <returns>True if the email is valid.</returns>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}