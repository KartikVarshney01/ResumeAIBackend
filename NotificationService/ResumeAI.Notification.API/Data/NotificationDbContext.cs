using Microsoft.EntityFrameworkCore;
using ResumeAI.Notification.API.Models;

namespace ResumeAI.Notification.API.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) 
        : base(options) { }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasKey(n => n.NotificationId);

            entity.Property(n => n.Type).IsRequired();

            entity.Property(n => n.Title).IsRequired();

            entity.Property(n => n.Message)
                  .HasColumnType("nvarchar(max)");

            entity.Property(n => n.IsRead)
                  .HasDefaultValue(false);

            entity.Property(n => n.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Fast lookup by user
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}