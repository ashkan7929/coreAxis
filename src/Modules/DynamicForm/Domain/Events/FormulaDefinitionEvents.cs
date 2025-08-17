using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a new formula definition is created.
    /// </summary>
    public class FormulaDefinitionCreatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string Expression { get; }
        public string ReturnType { get; }
        public string TenantId { get; }
        public string CreatedBy { get; }

        public FormulaDefinitionCreatedEvent(Guid formulaDefinitionId, string name, string expression, string returnType, string tenantId, string createdBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            Expression = expression;
            ReturnType = returnType;
            TenantId = tenantId;
            CreatedBy = createdBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is updated.
    /// </summary>
    public class FormulaDefinitionUpdatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string Expression { get; }
        public string ReturnType { get; }
        public string TenantId { get; }
        public string UpdatedBy { get; }

        public FormulaDefinitionUpdatedEvent(Guid formulaDefinitionId, string name, string expression, string returnType, string tenantId, string updatedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            Expression = expression;
            ReturnType = returnType;
            TenantId = tenantId;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is deleted.
    /// </summary>
    public class FormulaDefinitionDeletedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string DeletedBy { get; }

        public FormulaDefinitionDeletedEvent(Guid formulaDefinitionId, string name, string tenantId, string deletedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            DeletedBy = deletedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is activated.
    /// </summary>
    public class FormulaDefinitionActivatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string ActivatedBy { get; }

        public FormulaDefinitionActivatedEvent(Guid formulaDefinitionId, string name, string tenantId, string activatedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            ActivatedBy = activatedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is deactivated.
    /// </summary>
    public class FormulaDefinitionDeactivatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string DeactivatedBy { get; }

        public FormulaDefinitionDeactivatedEvent(Guid formulaDefinitionId, string name, string tenantId, string deactivatedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            DeactivatedBy = deactivatedBy;
        }
    }

    /// <summary>
    /// Event raised when a new formula version is created.
    /// </summary>
    public class FormulaVersionCreatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public Guid FormulaDefinitionId { get; }
        public int VersionNumber { get; }

        public FormulaVersionCreatedEvent(Guid formulaVersionId, Guid formulaDefinitionId, int versionNumber)
        {
            FormulaVersionId = formulaVersionId;
            FormulaDefinitionId = formulaDefinitionId;
            VersionNumber = versionNumber;
        }
    }

    /// <summary>
    /// Event raised when a formula version expression is updated.
    /// </summary>
    public class FormulaVersionExpressionUpdatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public string OldExpression { get; }
        public string NewExpression { get; }

        public FormulaVersionExpressionUpdatedEvent(Guid formulaVersionId, string oldExpression, string newExpression)
        {
            FormulaVersionId = formulaVersionId;
            OldExpression = oldExpression;
            NewExpression = newExpression;
        }
    }

    /// <summary>
    /// Event raised when a formula version description is updated.
    /// </summary>
    public class FormulaVersionDescriptionUpdatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public string? Description { get; }

        public FormulaVersionDescriptionUpdatedEvent(Guid formulaVersionId, string? description)
        {
            FormulaVersionId = formulaVersionId;
            Description = description;
        }
    }

    /// <summary>
    /// Event raised when a formula version validation rules are updated.
    /// </summary>
    public class FormulaVersionValidationRulesUpdatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public string? ValidationRules { get; }

        public FormulaVersionValidationRulesUpdatedEvent(Guid formulaVersionId, string? validationRules)
        {
            FormulaVersionId = formulaVersionId;
            ValidationRules = validationRules;
        }
    }

    /// <summary>
    /// Event raised when a formula version dependencies are updated.
    /// </summary>
    public class FormulaVersionDependenciesUpdatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public string? Dependencies { get; }

        public FormulaVersionDependenciesUpdatedEvent(Guid formulaVersionId, string? dependencies)
        {
            FormulaVersionId = formulaVersionId;
            Dependencies = dependencies;
        }
    }

    /// <summary>
    /// Event raised when a formula version metadata is updated.
    /// </summary>
    public class FormulaVersionMetadataUpdatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public string? Metadata { get; }

        public FormulaVersionMetadataUpdatedEvent(Guid formulaVersionId, string? metadata)
        {
            FormulaVersionId = formulaVersionId;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Event raised when a formula version is activated.
    /// </summary>
    public class FormulaVersionActivatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public Guid FormulaDefinitionId { get; }

        public FormulaVersionActivatedEvent(Guid formulaVersionId, Guid formulaDefinitionId)
        {
            FormulaVersionId = formulaVersionId;
            FormulaDefinitionId = formulaDefinitionId;
        }
    }

    /// <summary>
    /// Event raised when a formula version is deactivated.
    /// </summary>
    public class FormulaVersionDeactivatedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public Guid FormulaDefinitionId { get; }

        public FormulaVersionDeactivatedEvent(Guid formulaVersionId, Guid formulaDefinitionId)
        {
            FormulaVersionId = formulaVersionId;
            FormulaDefinitionId = formulaDefinitionId;
        }
    }

    /// <summary>
    /// Event raised when a formula version is published.
    /// </summary>
    public class FormulaVersionPublishedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public Guid FormulaDefinitionId { get; }
        public Guid PublishedBy { get; }

        public FormulaVersionPublishedEvent(Guid formulaVersionId, Guid formulaDefinitionId, Guid publishedBy)
        {
            FormulaVersionId = formulaVersionId;
            FormulaDefinitionId = formulaDefinitionId;
            PublishedBy = publishedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula version is unpublished.
    /// </summary>
    public class FormulaVersionUnpublishedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public Guid FormulaDefinitionId { get; }

        public FormulaVersionUnpublishedEvent(Guid formulaVersionId, Guid formulaDefinitionId)
        {
            FormulaVersionId = formulaVersionId;
            FormulaDefinitionId = formulaDefinitionId;
        }
    }

    /// <summary>
    /// Event raised when a formula version is executed.
    /// </summary>
    public class FormulaVersionExecutedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }
        public bool Success { get; }
        public double ExecutionTimeMs { get; }
        public string? Error { get; }

        public FormulaVersionExecutedEvent(Guid formulaVersionId, bool success, double executionTimeMs, string? error)
        {
            FormulaVersionId = formulaVersionId;
            Success = success;
            ExecutionTimeMs = executionTimeMs;
            Error = error;
        }
    }

    /// <summary>
    /// Event raised when a formula version execution history is cleared.
    /// </summary>
    public class FormulaVersionExecutionHistoryClearedEvent : DomainEvent
    {
        public Guid FormulaVersionId { get; }

        public FormulaVersionExecutionHistoryClearedEvent(Guid formulaVersionId)
        {
            FormulaVersionId = formulaVersionId;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is published.
    /// </summary>
    public class FormulaDefinitionPublishedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string Version { get; }
        public string PublishedBy { get; }

        public FormulaDefinitionPublishedEvent(Guid formulaDefinitionId, string name, string tenantId, string version, string publishedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            Version = version;
            PublishedBy = publishedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is unpublished.
    /// </summary>
    public class FormulaDefinitionUnpublishedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string UnpublishedBy { get; }

        public FormulaDefinitionUnpublishedEvent(Guid formulaDefinitionId, string name, string tenantId, string unpublishedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            UnpublishedBy = unpublishedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition validation rules are updated.
    /// </summary>
    public class FormulaDefinitionValidationRulesUpdatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string? ValidationRules { get; }
        public string ModifiedBy { get; }

        public FormulaDefinitionValidationRulesUpdatedEvent(Guid formulaDefinitionId, string name, string tenantId, string? validationRules, string modifiedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            ValidationRules = validationRules;
            ModifiedBy = modifiedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition dependencies are updated.
    /// </summary>
    public class FormulaDefinitionDependenciesUpdatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string? Dependencies { get; }
        public string ModifiedBy { get; }

        public FormulaDefinitionDependenciesUpdatedEvent(Guid formulaDefinitionId, string name, string tenantId, string? dependencies, string modifiedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            Dependencies = dependencies;
            ModifiedBy = modifiedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition parameters are updated.
    /// </summary>
    public class FormulaDefinitionParametersUpdatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string? Parameters { get; }
        public string ModifiedBy { get; }

        public FormulaDefinitionParametersUpdatedEvent(Guid formulaDefinitionId, string name, string tenantId, string? parameters, string modifiedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            Parameters = parameters;
            ModifiedBy = modifiedBy;
        }
    }

    /// <summary>
    /// Event raised when a formula definition is deprecated.
    /// </summary>
    public class FormulaDefinitionDeprecatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string Reason { get; }

        public FormulaDefinitionDeprecatedEvent(Guid formulaDefinitionId, string name, string tenantId, string reason)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            Reason = reason;
        }
    }

    /// <summary>
    /// Event raised when a new version of a formula definition is created.
    /// </summary>
    public class FormulaDefinitionVersionCreatedEvent : DomainEvent
    {
        public Guid FormulaDefinitionId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public int OldVersion { get; }
        public int NewVersion { get; }
        public string VersionedBy { get; }

        public FormulaDefinitionVersionCreatedEvent(Guid formulaDefinitionId, string name, string tenantId, int oldVersion, int newVersion, string versionedBy)
        {
            FormulaDefinitionId = formulaDefinitionId;
            Name = name;
            TenantId = tenantId;
            OldVersion = oldVersion;
            NewVersion = newVersion;
            VersionedBy = versionedBy;
        }
    }
}