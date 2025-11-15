using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Domain.Entities.Action> Actions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<AccessLog> AccessLogs { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent as it's not an entity but a base class for domain events
        modelBuilder.Ignore<DomainEvent>();

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);

            entity.Property(e => e.NationalCode).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.NationalCode).IsUnique();

            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.HasIndex(e => e.PhoneNumber);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => new { e.PageId, e.ActionId }).IsUnique();

            entity.HasOne(p => p.Page)
                .WithMany(pg => pg.Permissions)
                .HasForeignKey(p => p.PageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Action)
                .WithMany(a => a.Permissions)
                .HasForeignKey(p => p.ActionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Page entity
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure Action entity
        modelBuilder.Entity<Domain.Entities.Action>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure UserRole junction entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RolePermission junction entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserPermission junction entity
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PermissionId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AccessLog entity
        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            
            // Configure foreign key relationship with User
            entity.HasOne(e => e.User)
                .WithMany(u => u.AccessLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OtpCode entity
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MobileNumber).IsRequired().HasMaxLength(15);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Purpose).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            // Create indexes for better performance
            entity.HasIndex(e => new { e.MobileNumber, e.Purpose, e.IsUsed });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedOn);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit fields for entities that inherit from EntityBase
        var entries = ChangeTracker.Entries<EntityBase>();
        
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    // Set CreatedBy and LastModifiedBy to system for new entities
                    // For User entities, we'll set it to the user's own ID after it's generated
                    if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                    {
                        entry.Entity.CreatedBy = entry.Entity is User ? entry.Entity.Id.ToString() : "System";
                    }
                    if (string.IsNullOrEmpty(entry.Entity.LastModifiedBy))
                    {
                        entry.Entity.LastModifiedBy = entry.Entity is User ? entry.Entity.Id.ToString() : "System";
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    // Set LastModifiedBy to system if not already set
                    if (string.IsNullOrEmpty(entry.Entity.LastModifiedBy))
                    {
                        entry.Entity.LastModifiedBy = "System";
                    }
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}