using Microsoft.EntityFrameworkCore;
using ResumeAI.AI.API.Models;

namespace ResumeAI.AI.API.Data;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options) { }

    public DbSet<AiRequest> AiRequests => Set<AiRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiRequest>(entity =>
        {
            entity.HasKey(a => a.RequestId);

            entity.Property(a => a.RequestType)
                  .IsRequired();

            entity.Property(a => a.Prompt)
                  .HasColumnType("nvarchar(max)");

            entity.Property(a => a.Response)
                  .HasColumnType("nvarchar(max)");

            entity.Property(a => a.Status)
                  .HasDefaultValue("PENDING");

            entity.Property(a => a.AiProvider)
                  .HasDefaultValue("OPENAI");

            entity.Property(a => a.TokensUsed)
                  .HasDefaultValue(0);

            entity.Property(a => a.RequestedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Index for fast user quota lookups
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => new { a.UserId, a.RequestedAt });
        });
    }
}