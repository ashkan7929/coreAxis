using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Infrastructure.Data;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = null!;
    public DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions { get; set; } = null!;
    public DbSet<WorkflowRun> WorkflowRuns { get; set; } = null!;
    public DbSet<WorkflowRunStep> WorkflowRunSteps { get; set; } = null!;
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WorkflowDefinition", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(int.MaxValue);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<WorkflowDefinitionVersion>(entity =>
        {
            entity.ToTable("WorkflowDefinitionVersion", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DslJson).IsRequired();
            entity.Property(e => e.SchemaVersion).HasDefaultValue(1);
            entity.Property(e => e.VersionNumber).IsRequired();
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(128);
            entity.HasOne<WorkflowDefinition>()
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.WorkflowDefinitionId);
            entity.HasIndex(e => new { e.WorkflowDefinitionId, e.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRun", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.InputContextJson).IsRequired();
            entity.Property(e => e.OutputContextJson);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.InitiatedBy).HasMaxLength(128);
            entity.Property(e => e.StartedAt).HasColumnType("datetime2");
            entity.Property(e => e.EndedAt).HasColumnType("datetime2");
            entity.Property(e => e.LastError);
            entity.HasIndex(e => e.DefinitionId);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<WorkflowRunStep>(entity =>
        {
            entity.ToTable("WorkflowRunStep", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepKey).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Attempt).HasDefaultValue(0);
            entity.Property(e => e.RequestJson);
            entity.Property(e => e.ResponseJson);
            entity.Property(e => e.Error);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(128);
            entity.Property(e => e.StartedAt).HasColumnType("datetime2");
            entity.Property(e => e.EndedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.RunId);
        });

        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("IdempotencyKey", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Route).IsRequired().HasMaxLength(256);
            entity.Property(e => e.BodyHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ResponseJson);
            entity.Property(e => e.StatusCode).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.Route, e.Key, e.BodyHash }).IsUnique();
        });
    }
}