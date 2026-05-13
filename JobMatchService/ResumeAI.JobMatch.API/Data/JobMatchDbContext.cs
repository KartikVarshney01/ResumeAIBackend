using Microsoft.EntityFrameworkCore;
using ResumeAI.JobMatch.API.Models;

namespace ResumeAI.JobMatch.API.Data;

public class JobMatchDbContext : DbContext
{
    public JobMatchDbContext(DbContextOptions<JobMatchDbContext> options) : base(options) { }

    public DbSet<JobMatchEntity> JobMatches => Set<JobMatchEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobMatchEntity>(entity =>
        {
            entity.HasKey(j => j.JobMatchId);

            entity.Property(j => j.JobTitle)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(j => j.Company)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(j => j.JobDescription)
                  .HasColumnType("nvarchar(max)");

            entity.Property(j => j.Source)
                  .HasDefaultValue("MANUAL");

            entity.Property(j => j.Status)
                  .HasDefaultValue("SAVED");

            entity.Property(j => j.MatchScore)
                  .HasDefaultValue(0);

            entity.Property(j => j.IsRemote)
                  .HasDefaultValue(false);

            entity.Property(j => j.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(j => j.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(j => j.UserId);
            entity.HasIndex(j => j.ResumeId);
        });
    }
}