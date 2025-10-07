using CoreAxis.SharedKernel;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.Modules.DynamicForm.Domain.Events;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities;

public class FormulaVersion : EntityBase
{
    public Guid FormulaDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Expression { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ChangeLog { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public string? ValidationRules { get; private set; }
    public string? Dependencies { get; private set; }
    public string? Metadata { get; private set; }
    public int ExecutionCount { get; private set; }
    public DateTime? LastExecutedAt { get; private set; }
    public double? AverageExecutionTime { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastErrorAt { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    // Navigation Properties
    public virtual FormulaDefinition FormulaDefinition { get; private set; } = null!;
    public virtual ICollection<FormulaEvaluationLog> EvaluationLogs { get; private set; } = new List<FormulaEvaluationLog>();

    private FormulaVersion() { } // For EF Core

    public FormulaVersion(
        Guid formulaDefinitionId,
        int versionNumber,
        string expression,
        string tenantId,
        string? description = null,
        string? changeLog = null)
    {
        FormulaDefinitionId = formulaDefinitionId;
        VersionNumber = versionNumber;
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        Description = description;
        ChangeLog = changeLog;
        IsActive = false;
        IsPublished = false;
        ExecutionCount = 0;

        AddDomainEvent(new FormulaVersionCreatedEvent(Id, FormulaDefinitionId, VersionNumber));
    }

    public void UpdateExpression(string expression, string? changeLog = null)
    {
        if (IsPublished)
            throw new InvalidOperationException("Cannot update expression of a published formula version");

        var oldExpression = Expression;
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        ChangeLog = changeLog;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionExpressionUpdatedEvent(Id, oldExpression, expression));
    }

    public void UpdateDescription(string? description)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException("Cannot edit description of a published formula version");
        }
        Description = description;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionDescriptionUpdatedEvent(Id, description));
    }

    public void SetValidationRules(string? validationRules)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException("Cannot edit validation rules of a published formula version");
        }
        ValidationRules = validationRules;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionValidationRulesUpdatedEvent(Id, validationRules));
    }

    public void SetDependencies(string? dependencies)
    {
        Dependencies = dependencies;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionDependenciesUpdatedEvent(Id, dependencies));
    }

    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionMetadataUpdatedEvent(Id, metadata));
    }

    public void Activate()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Cannot activate an unpublished formula version");

        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionActivatedEvent(Id, FormulaDefinitionId));
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionDeactivatedEvent(Id, FormulaDefinitionId));
    }

    public void Publish(Guid publishedBy)
    {
        if (IsPublished)
            throw new InvalidOperationException("Formula version is already published");

        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        PublishedBy = publishedBy;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionPublishedEvent(Id, FormulaDefinitionId, publishedBy));
    }

    public void Unpublish()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Formula version is not published");

        if (IsActive)
            throw new InvalidOperationException("Cannot unpublish an active formula version");

        IsPublished = false;
        PublishedAt = null;
        PublishedBy = null;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionUnpublishedEvent(Id, FormulaDefinitionId));
    }

    public void SetEffectivePeriod(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new InvalidOperationException("EffectiveFrom cannot be after EffectiveTo");

        EffectiveFrom = from;
        EffectiveTo = to;
        LastModifiedOn = DateTime.UtcNow;
    }

    public bool IsEffectiveAt(DateTime dateUtc)
    {
        var fromOk = !EffectiveFrom.HasValue || EffectiveFrom.Value <= dateUtc;
        var toOk = !EffectiveTo.HasValue || EffectiveTo.Value >= dateUtc;
        return fromOk && toOk;
    }

    public void RecordExecution(double executionTimeMs, bool success, string? error = null)
    {
        ExecutionCount++;
        LastExecutedAt = DateTime.UtcNow;
        
        // Calculate running average
        if (AverageExecutionTime.HasValue)
        {
            AverageExecutionTime = ((AverageExecutionTime.Value * (ExecutionCount - 1)) + executionTimeMs) / ExecutionCount;
        }
        else
        {
            AverageExecutionTime = executionTimeMs;
        }

        if (!success)
        {
            LastError = error;
            LastErrorAt = DateTime.UtcNow;
        }
        else
        {
            LastError = null;
            LastErrorAt = null;
        }

        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionExecutedEvent(Id, success, executionTimeMs, error));
    }

    public bool CanBeDeleted()
    {
        return !IsActive && !IsPublished;
    }

    public bool HasErrors()
    {
        return !string.IsNullOrEmpty(LastError);
    }

    public TimeSpan? GetTimeSinceLastExecution()
    {
        return LastExecutedAt.HasValue ? DateTime.UtcNow - LastExecutedAt.Value : null;
    }

    public double GetSuccessRate()
    {
        if (ExecutionCount == 0) return 0;
        
        var errorCount = EvaluationLogs.Count(log => !log.IsSuccess);
        return ((double)(ExecutionCount - errorCount) / ExecutionCount) * 100;
    }

    public void ValidateExpression()
    {
        if (string.IsNullOrWhiteSpace(Expression))
            throw new ArgumentException("Expression cannot be null or empty", nameof(Expression));

        // Additional expression validation logic can be added here
        // This could include syntax validation, security checks, etc.
    }

    public FormulaExpression GetFormulaExpression()
    {
        return new FormulaExpression(Expression);
    }

    public void ClearExecutionHistory()
    {
        ExecutionCount = 0;
        LastExecutedAt = null;
        AverageExecutionTime = null;
        LastError = null;
        LastErrorAt = null;
        LastModifiedOn = DateTime.UtcNow;

        AddDomainEvent(new FormulaVersionExecutionHistoryClearedEvent(Id));
    }
}