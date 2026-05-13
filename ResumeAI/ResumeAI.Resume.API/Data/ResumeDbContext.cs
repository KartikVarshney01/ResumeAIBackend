using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Models;

namespace ResumeAI.Resume.API.Data;

public class ResumeDbContext : DbContext
{
    public ResumeDbContext(DbContextOptions<ResumeDbContext> options) : base(options) { }

    public DbSet<ResumeEntity> Resumes => Set<ResumeEntity>();
    public DbSet<ResumeSection> ResumeSections => Set<ResumeSection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResumeEntity>(entity =>
        {
            entity.HasKey(r => r.ResumeId);

            entity.Property(r => r.Title)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(r => r.Status)
                  .HasDefaultValue("DRAFT");

            entity.Property(r => r.Language)
                  .HasDefaultValue("en");

            entity.Property(r => r.IsPublic)
                  .HasDefaultValue(false);

            entity.Property(r => r.AtsScore)
                  .HasDefaultValue(0);

            entity.Property(r => r.ViewCount)
                  .HasDefaultValue(0);

            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(r => r.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(r => r.Sections)
                  .WithOne(s => s.Resume)
                  .HasForeignKey(s => s.ResumeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResumeSection>(entity =>
        {
            entity.HasKey(s => s.SectionId);

            entity.Property(s => s.SectionType).IsRequired();

            entity.Property(s => s.Title).IsRequired();

            entity.Property(s => s.Content)
                  .HasColumnType("nvarchar(max)");

            entity.Property(s => s.IsVisible).HasDefaultValue(true);

            entity.Property(s => s.AiGenerated).HasDefaultValue(false);

            entity.Property(s => s.DisplayOrder).HasDefaultValue(0);
        });
    }
}