using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.Modules.DynamicForm.Domain.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a formula definition in the system.
    /// </summary>
    public class FormulaDefinition : EntityBase
    {
        private readonly List<FormulaEvaluationLog> _evaluationLogs = new List<FormulaEvaluationLog>();

        /// <summary>
        /// Gets or sets the name of the formula.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the formula.
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the formula expression.
        /// </summary>
        [Required]
        [MaxLength(5000)]
        public string Expression { get; set; }

        /// <summary>
        /// Gets or sets the version of the formula.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the return type of the formula (number, string, boolean, date).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the input parameters definition as JSON.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets the category of the formula.
        /// </summary>
        [MaxLength(100)]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the tags for the formula as JSON array.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the formula is published and available for use.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the formula is deprecated.
        /// </summary>
        public bool IsDeprecated { get; set; } = false;

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the formula as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the validation rules for input parameters as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets example usage of the formula as JSON.
        /// </summary>
        public string Examples { get; set; }

        /// <summary>
        /// Gets or sets the performance metrics as JSON (execution time, memory usage, etc.).
        /// </summary>
        public string PerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the dependencies of this formula (other formulas it depends on) as JSON array.
        /// </summary>
        public string Dependencies { get; set; }

        /// <summary>
        /// Gets the collection of evaluation logs.
        /// </summary>
        public virtual IReadOnlyCollection<FormulaEvaluationLog> EvaluationLogs => _evaluationLogs.AsReadOnly();

        /// <summary>
        /// Gets the collection of formula versions.
        /// </summary>
        public virtual ICollection<FormulaVersion> Versions { get; private set; } = new List<FormulaVersion>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaDefinition"/> class.
        /// </summary>
        protected FormulaDefinition()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaDefinition"/> class.
        /// </summary>
        /// <param name="name">The name of the formula.</param>
        /// <param name="expression">The formula expression.</param>
        /// <param name="returnType">The return type of the formula.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="createdBy">The user who created the formula.</param>
        public FormulaDefinition(string name, string expression, string returnType, string tenantId, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Formula name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Formula expression cannot be null or empty.", nameof(expression));
            if (string.IsNullOrWhiteSpace(returnType))
                throw new ArgumentException("Return type cannot be null or empty.", nameof(returnType));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            Id = Guid.NewGuid();
            Name = name;
            Expression = expression;
            ReturnType = returnType;
            TenantId = tenantId;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormulaDefinitionCreatedEvent(Id, Name, Expression, ReturnType, TenantId, createdBy));
        }

        /// <summary>
        /// Updates the formula with new information.
        /// </summary>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="expression">The new expression.</param>
        /// <param name="returnType">The new return type.</param>
        /// <param name="modifiedBy">The user who modified the formula.</param>
        public void Update(string name, string description, string expression, string returnType, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Formula name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Formula expression cannot be null or empty.", nameof(expression));
            if (string.IsNullOrWhiteSpace(returnType))
                throw new ArgumentException("Return type cannot be null or empty.", nameof(returnType));

            Name = name;
            Description = description;
            Expression = expression;
            ReturnType = returnType;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionUpdatedEvent(Id, Name, Expression, ReturnType, TenantId, modifiedBy));
        }

        /// <summary>
        /// Publishes the formula making it available for use.
        /// </summary>
        /// <param name="publishedBy">The user who published the formula.</param>
        public void Publish(string publishedBy)
        {
            if (IsPublished)
                throw new InvalidOperationException("Formula is already published.");

            if (IsDeprecated)
                throw new InvalidOperationException("Cannot publish a deprecated formula.");

            IsPublished = true;
            LastModifiedBy = publishedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionPublishedEvent(Id, Name, TenantId, Version.ToString(), publishedBy));
        }

        /// <summary>
        /// Unpublishes the formula making it unavailable for new use.
        /// </summary>
        /// <param name="unpublishedBy">The user who unpublished the formula.</param>
        public void Unpublish(string unpublishedBy)
        {
            if (!IsPublished)
                throw new InvalidOperationException("Formula is not published.");

            IsPublished = false;
            LastModifiedBy = unpublishedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionUnpublishedEvent(Id, Name, TenantId, unpublishedBy));
        }

        /// <summary>
        /// Deprecates the formula.
        /// </summary>
        /// <param name="deprecatedBy">The user who deprecated the formula.</param>
        /// <param name="reason">The reason for deprecation.</param>
        public void Deprecate(string deprecatedBy, string reason = null)
        {
            if (IsDeprecated)
                throw new InvalidOperationException("Formula is already deprecated.");

            IsDeprecated = true;
            IsPublished = false; // Deprecated formulas cannot be published
            LastModifiedBy = deprecatedBy;
            LastModifiedOn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata ?? "{}");
                metadata["deprecationReason"] = reason;
                metadata["deprecatedAt"] = DateTime.UtcNow;
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);
            }

            AddDomainEvent(new FormulaDefinitionDeprecatedEvent(Id, Name, TenantId, reason));
        }

        /// <summary>
        /// Creates a new version of the formula.
        /// </summary>
        /// <param name="newExpression">The new expression for the version.</param>
        /// <param name="versionedBy">The user who created the version.</param>
        public void CreateVersion(string newExpression, string versionedBy)
        {
            if (string.IsNullOrWhiteSpace(newExpression))
                throw new ArgumentException("New expression cannot be null or empty.", nameof(newExpression));

            Version++;
            Expression = newExpression;
            IsPublished = false; // New version needs to be published again
            LastModifiedBy = versionedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionVersionCreatedEvent(Id, Name, TenantId, Version - 1, Version, versionedBy));
        }

        /// <summary>
        /// Sets the parameters definition for the formula.
        /// </summary>
        /// <param name="parameters">The parameters definition as JSON.</param>
        /// <param name="modifiedBy">The user who set the parameters.</param>
        public void SetParameters(string parameters, string modifiedBy)
        {
            Parameters = parameters;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionParametersUpdatedEvent(Id, Name, TenantId, parameters, modifiedBy));
        }

        /// <summary>
        /// Sets the validation rules for the formula.
        /// </summary>
        /// <param name="validationRules">The validation rules as JSON.</param>
        /// <param name="modifiedBy">The user who set the validation rules.</param>
        public void SetValidationRules(string validationRules, string modifiedBy)
        {
            ValidationRules = validationRules;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionValidationRulesUpdatedEvent(Id, Name, TenantId, validationRules, modifiedBy));
        }

        /// <summary>
        /// Sets the dependencies for the formula.
        /// </summary>
        /// <param name="dependencies">The dependencies as JSON array.</param>
        /// <param name="modifiedBy">The user who set the dependencies.</param>
        public void SetDependencies(string dependencies, string modifiedBy)
        {
            Dependencies = dependencies;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaDefinitionDependenciesUpdatedEvent(Id, Name, TenantId, dependencies, modifiedBy));
        }

        /// <summary>
        /// Updates the performance metrics for the formula.
        /// </summary>
        /// <param name="performanceMetrics">The performance metrics as JSON.</param>
        /// <param name="modifiedBy">The user who updated the metrics.</param>
        public void UpdatePerformanceMetrics(string performanceMetrics, string modifiedBy)
        {
            PerformanceMetrics = performanceMetrics;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an evaluation log to the formula.
        /// </summary>
        /// <param name="evaluationLog">The evaluation log to add.</param>
        internal void AddEvaluationLog(FormulaEvaluationLog evaluationLog)
        {
            if (evaluationLog == null)
                throw new ArgumentNullException(nameof(evaluationLog));

            _evaluationLogs.Add(evaluationLog);
        }
    }

    /// <summary>
    /// Constants for formula return types.
    /// </summary>
    public static class FormulaReturnType
    {
        public const string Number = "number";
        public const string String = "string";
        public const string Boolean = "boolean";
        public const string Date = "date";
        public const string Array = "array";
        public const string Object = "object";
    }


}