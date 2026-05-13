using Microsoft.EntityFrameworkCore;
using ResumeAI.Auth.API.Models;

namespace ResumeAI.Auth.API.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.HasIndex(u => u.Email).IsUnique(); // No Duplicate Emails

            entity.Property(u => u.FullName).IsRequired().HasMaxLength(120);

            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);

            entity.Property(u => u.Role).HasDefaultValue("USER");

            entity.Property(u => u.Provider).HasDefaultValue("LOCAL");

            entity.Property(u => u.IsActive).HasDefaultValue(true);

            entity.Property(u => u.SubscriptionPlan).HasDefaultValue("FREE");

            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}