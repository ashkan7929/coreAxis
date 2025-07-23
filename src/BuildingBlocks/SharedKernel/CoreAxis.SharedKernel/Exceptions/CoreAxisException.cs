using System;
using System.Collections.Generic;

namespace CoreAxis.SharedKernel.Exceptions
{
    /// <summary>
    /// Base exception class for all custom exceptions in the CoreAxis platform.
    /// </summary>
    public abstract class CoreAxisException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreAxisException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The error code.</param>
        protected CoreAxisException(string message, string code) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreAxisException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="code">The error code.</param>
        protected CoreAxisException(string message, Exception innerException, string code) : base(message, innerException)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Exception thrown when a requested entity is not found.
    /// </summary>
    public class EntityNotFoundException : CoreAxisException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
        /// </summary>
        /// <param name="entityType">The type of entity that was not found.</param>
        /// <param name="id">The ID of the entity that was not found.</param>
        public EntityNotFoundException(string entityType, object id)
            : base($"Entity of type {entityType} with ID {id} was not found.", "ENTITY_NOT_FOUND")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a business rule is violated.
    /// </summary>
    public class BusinessRuleViolationException : CoreAxisException
    {
        /// <summary>
        /// Gets the details of the business rule violations.
        /// </summary>
        public IReadOnlyList<string> Details { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public BusinessRuleViolationException(string message)
            : base(message, "BUSINESS_RULE_VIOLATION")
        {
            Details = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="details">The details of the business rule violations.</param>
        public BusinessRuleViolationException(string message, IEnumerable<string> details)
            : base(message, "BUSINESS_RULE_VIOLATION")
        {
            Details = new List<string>(details).AsReadOnly();
        }
    }

    /// <summary>
    /// Exception thrown when an operation is not permitted due to insufficient permissions.
    /// </summary>
    public class UnauthorizedAccessException : CoreAxisException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedAccessException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public UnauthorizedAccessException(string message)
            : base(message, "UNAUTHORIZED_ACCESS")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a validation error occurs.
    /// </summary>
    public class ValidationException : CoreAxisException
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", "VALIDATION_ERROR")
        {
            Errors = errors;
        }
    }
}