using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class CommissionRuleVersion : EntityBase
{
    public Guid RuleSetId { get; private set; }
    public int Version { get; private set; }
    public string SchemaJson { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; } = false;
    public DateTime? PublishedAt { get; private set; }
    public string? PublishedBy { get; private set; }
    
    // Navigation properties
    public virtual CommissionRuleSet RuleSet { get; private set; } = null!;
    
    private CommissionRuleVersion() { } // For EF Core
    
    public CommissionRuleVersion(Guid ruleSetId, int version, string schemaJson)
    {
        RuleSetId = ruleSetId;
        Version = version;
        SchemaJson = schemaJson;
        CreatedOn = DateTime.UtcNow;
        
        ValidateSchemaJson();
        ValidateVersion();
    }
    
    public void UpdateSchema(string schemaJson)
    {
        if (IsPublished)
            throw new InvalidOperationException("Cannot update schema of published version");
            
        SchemaJson = schemaJson;
        LastModifiedOn = DateTime.UtcNow;
        ValidateSchemaJson();
    }
    
    public void Publish(string publishedBy)
    {
        if (IsPublished)
            throw new InvalidOperationException("Version is already published");
            
        if (string.IsNullOrWhiteSpace(publishedBy))
            throw new ArgumentException("Published by is required", nameof(publishedBy));
            
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        PublishedBy = publishedBy;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    private void ValidateSchemaJson()
    {
        if (string.IsNullOrWhiteSpace(SchemaJson))
            throw new ArgumentException("Schema JSON is required", nameof(SchemaJson));
            
        // Additional JSON validation can be added here
    }
    
    private void ValidateVersion()
    {
        if (Version < 1)
            throw new ArgumentException("Version must be greater than 0", nameof(Version));
    }
}