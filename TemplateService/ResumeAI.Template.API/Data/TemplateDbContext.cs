using Microsoft.EntityFrameworkCore;
using ResumeAI.Template.API.Models;

namespace ResumeAI.Template.API.Data;

public class TemplateDbContext : DbContext
{
    public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options) { }

    public DbSet<ResumeTemplate> ResumeTemplates => Set<ResumeTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResumeTemplate>(entity =>
        {
            entity.HasKey(t => t.TemplateId);

            entity.Property(t => t.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(t => t.Description)
                  .HasMaxLength(500);

            entity.Property(t => t.HtmlLayout)
                  .HasColumnType("nvarchar(max)");

            entity.Property(t => t.CssStyles)
                  .HasColumnType("nvarchar(max)");

            entity.Property(t => t.Category)
                  .HasDefaultValue("PROFESSIONAL");

            entity.Property(t => t.IsPremium)
                  .HasDefaultValue(false);

            entity.Property(t => t.IsActive)
                  .HasDefaultValue(true);

            entity.Property(t => t.UsageCount)
                  .HasDefaultValue(0);

            entity.Property(t => t.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // ── Seed default templates ────────────────────────────────────
            entity.HasData(
                new ResumeTemplate
                {
                    TemplateId  = 1,
                    Name        = "Modern Professional",
                    Description = "A clean modern template perfect for tech professionals",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Inter,sans-serif;max-width:800px;margin:0 auto;padding:40px;color:#333}.header{text-align:center;border-bottom:2px solid #7C6FE0;padding-bottom:20px;margin-bottom:30px}h1{font-size:28px;color:#1a1a2e}.role{font-size:16px;color:#7C6FE0}.email{font-size:14px;color:#666}",
                    Category    = "MODERN",
                    IsPremium   = false,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 2,
                    Name        = "Classic Professional",
                    Description = "Traditional layout trusted by recruiters worldwide",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Georgia,serif;max-width:800px;margin:0 auto;padding:40px;color:#222}.header{border-bottom:1px solid #333;padding-bottom:16px;margin-bottom:24px}h1{font-size:26px}.role{font-size:15px;color:#555}.email{font-size:13px;color:#777}",
                    Category    = "PROFESSIONAL",
                    IsPremium   = false,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 3,
                    Name        = "Minimalist",
                    Description = "Less is more — clean and distraction free",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:system-ui,sans-serif;max-width:760px;margin:0 auto;padding:48px;color:#111}.header{margin-bottom:32px}h1{font-size:24px;font-weight:400}.role{font-size:14px;color:#666}.email{font-size:13px;color:#888}",
                    Category    = "MINIMALIST",
                    IsPremium   = false,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 4,
                    Name        = "ATS Optimised",
                    Description = "Designed to pass Applicant Tracking Systems",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Arial,sans-serif;max-width:800px;margin:0 auto;padding:36px;color:#000}.header{margin-bottom:20px}h1{font-size:22px}.role{font-size:14px}.email{font-size:13px}",
                    Category    = "ATS-OPTIMISED",
                    IsPremium   = false,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 5,
                    Name        = "Creative Dark",
                    Description = "Bold dark design for creative professionals",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Inter,sans-serif;max-width:800px;margin:0 auto;padding:40px;background:#1a1828;color:#F0EEF8}.header{border-bottom:1px solid #2E2B44;padding-bottom:20px;margin-bottom:28px}h1{font-size:26px;color:#A99EEA}.role{font-size:15px;color:#7C6FE0}.email{font-size:13px;color:#9B97B8}",
                    Category    = "CREATIVE",
                    IsPremium   = false,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 6,
                    Name        = "Premium Executive",
                    Description = "Sophisticated design for senior professionals",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Georgia,serif;max-width:800px;margin:0 auto;padding:48px;color:#1a1a2e}.header{text-align:center;border-bottom:3px double #7C6FE0;padding-bottom:24px;margin-bottom:32px}h1{font-size:32px;font-weight:700;letter-spacing:2px}.role{font-size:16px;color:#7C6FE0;font-style:italic}.email{font-size:13px;color:#666}",
                    Category    = "PROFESSIONAL",
                    IsPremium   = true,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ResumeTemplate
                {
                    TemplateId  = 7,
                    Name        = "Premium Creative Portfolio",
                    Description = "Stand out with this stunning creative layout",
                    HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                    CssStyles   = ".resume{font-family:Poppins,sans-serif;max-width:800px;margin:0 auto;padding:40px;color:#2d2d2d}.header{background:linear-gradient(135deg,#7C6FE0,#A99EEA);padding:32px;margin:-40px -40px 32px;color:white;border-radius:0 0 20px 20px}h1{font-size:28px;font-weight:700;color:white}.role{font-size:15px;color:rgba(255,255,255,0.85)}.email{font-size:13px;color:rgba(255,255,255,0.7)}",
                    Category    = "CREATIVE",
                    IsPremium   = true,
                    IsActive    = true,
                    UsageCount  = 0,
                    CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}