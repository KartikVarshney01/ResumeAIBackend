using Microsoft.EntityFrameworkCore;
using ResumeAI.Export.API.Models;

namespace ResumeAI.Export.API.Data;

public class ExportDbContext : DbContext
{
    public ExportDbContext(DbContextOptions<ExportDbContext> options) : base(options) { }

    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExportJob>(entity =>
        {
            entity.HasKey(e => e.ExportJobId);

            entity.Property(e => e.Format)
                  .IsRequired();

            entity.Property(e => e.Status)
                  .HasDefaultValue("PENDING");

            entity.Property(e => e.FileSizeBytes)
                  .HasDefaultValue(0);

            entity.Property(e => e.RequestedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Index for fast user export history lookups
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ResumeId);
        });
    }
}