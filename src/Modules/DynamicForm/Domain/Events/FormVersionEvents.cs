using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.DynamicForm.Domain.Events;

/// <summary>
/// Event raised when a form version is created.
/// </summary>
public class FormVersionCreatedEvent : DomainEvent
{
    public Guid VersionId { get; }
    public Guid FormId { get; }
    public int Version { get; }
    public string TenantId { get; }

    public FormVersionCreatedEvent(Guid versionId, Guid formId, int version, string tenantId)
    {
        VersionId = versionId;
        FormId = formId;
        Version = version;
        TenantId = tenantId;
    }
}

/// <summary>
/// Event raised when a form version is activated.
/// </summary>
public class FormVersionActivatedEvent : DomainEvent
{
    public Guid VersionId { get; }
    public Guid FormId { get; }
    public int Version { get; }
    public string TenantId { get; }

    public FormVersionActivatedEvent(Guid versionId, Guid formId, int version, string tenantId)
    {
        VersionId = versionId;
        FormId = formId;
        Version = version;
        TenantId = tenantId;
    }
}

/// <summary>
/// Event raised when a form version is deactivated.
/// </summary>
public class FormVersionDeactivatedEvent : DomainEvent
{
    public Guid VersionId { get; }
    public Guid FormId { get; }
    public int Version { get; }
    public string TenantId { get; }

    public FormVersionDeactivatedEvent(Guid versionId, Guid formId, int version, string tenantId)
    {
        VersionId = versionId;
        FormId = formId;
        Version = version;
        TenantId = tenantId;
    }
}

/// <summary>
/// Event raised when a form version is published.
/// </summary>
public class FormVersionPublishedEvent : DomainEvent
{
    public Guid VersionId { get; }
    public Guid FormId { get; }
    public int Version { get; }
    public string TenantId { get; }

    public FormVersionPublishedEvent(Guid versionId, Guid formId, int version, string tenantId)
    {
        VersionId = versionId;
        FormId = formId;
        Version = version;
        TenantId = tenantId;
    }
}