using CoreAxis.Modules.NotificationModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.NotificationModule.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationTemplate> Templates { get; set; }
    public DbSet<NotificationLog> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<NotificationTemplate>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Key).IsRequired().HasMaxLength(100);
            b.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<NotificationLog>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Recipient).IsRequired().HasMaxLength(255);
            b.HasIndex(e => e.SentAt);
        });
    }
}
