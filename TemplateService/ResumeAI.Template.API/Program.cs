using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeAI.Template.API.Data;
using ResumeAI.Template.API.Middleware;
using ResumeAI.Template.API.Models;
using ResumeAI.Template.API.Repositories;
using ResumeAI.Template.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TemplateDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("TemplateDb")));

// ── Memory Cache ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Dependency Injection ──────────────────────────────────────────────────────
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("PremiumOnly", p => p.RequireClaim("plan", "PREMIUM"));
    opt.AddPolicy("AdminOnly",   p => p.RequireRole("ADMIN"));
});

// ── CORS (allow any origin for dynamic tunnels) ──────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAngular", p =>
        p.SetIsOriginAllowed(origin => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title   = "ResumeAI Template API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Enter your JWT token"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Auto-run migrations ───────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
    db.Database.Migrate();

    if (db.ResumeTemplates.Count() < 7)
    {
        // Clear existing to ensure clean full seed
        foreach(var t in db.ResumeTemplates) db.ResumeTemplates.Remove(t);
        db.SaveChanges();

        db.ResumeTemplates.AddRange(new List<ResumeTemplate>
        {
            new ResumeTemplate
            {
                Name        = "Modern Professional",
                Description = "A clean modern template perfect for tech professionals",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Inter,sans-serif;max-width:800px;margin:0 auto;padding:40px;color:#333}.header{text-align:center;border-bottom:2px solid #7C6FE0;padding-bottom:20px;margin-bottom:30px}h1{font-size:28px;color:#1a1a2e}.role{font-size:16px;color:#7C6FE0}.email{font-size:14px;color:#666}",
                Category    = "MODERN",
                IsPremium   = false,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "Classic Professional",
                Description = "Traditional layout trusted by recruiters worldwide",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Georgia,serif;max-width:800px;margin:0 auto;padding:40px;color:#222}.header{border-bottom:1px solid #333;padding-bottom:16px;margin-bottom:24px}h1{font-size:26px}.role{font-size:15px;color:#555}.email{font-size:13px;color:#777}",
                Category    = "PROFESSIONAL",
                IsPremium   = false,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "Minimalist",
                Description = "Less is more — clean and distraction free",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:system-ui,sans-serif;max-width:760px;margin:0 auto;padding:48px;color:#111}.header{margin-bottom:32px}h1{font-size:24px;font-weight:400}.role{font-size:14px;color:#666}.email{font-size:13px;color:#888}",
                Category    = "MINIMALIST",
                IsPremium   = false,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "ATS Optimised",
                Description = "Designed to pass Applicant Tracking Systems",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Arial,sans-serif;max-width:800px;margin:0 auto;padding:36px;color:#000}.header{margin-bottom:20px}h1{font-size:22px}.role{font-size:14px}.email{font-size:13px}",
                Category    = "ATS-OPTIMISED",
                IsPremium   = false,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "Creative Dark",
                Description = "Bold dark design for creative professionals",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Inter,sans-serif;max-width:800px;margin:0 auto;padding:40px;background:#1a1828;color:#F0EEF8}.header{border-bottom:1px solid #2E2B44;padding-bottom:20px;margin-bottom:28px}h1{font-size:26px;color:#A99EEA}.role{font-size:15px;color:#7C6FE0}.email{font-size:13px;color:#9B97B8}",
                Category    = "CREATIVE",
                IsPremium   = false,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "Premium Executive",
                Description = "Sophisticated design for senior professionals",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Georgia,serif;max-width:800px;margin:0 auto;padding:48px;color:#1a1a2e}.header{text-align:center;border-bottom:3px double #7C6FE0;padding-bottom:24px;margin-bottom:32px}h1{font-size:32px;font-weight:700;letter-spacing:2px}.role{font-size:16px;color:#7C6FE0;font-style:italic}.email{font-size:13px;color:#666}",
                Category    = "PROFESSIONAL",
                IsPremium   = true,
                IsActive    = true
            },
            new ResumeTemplate
            {
                Name        = "Premium Creative Portfolio",
                Description = "Stand out with this stunning creative layout",
                HtmlLayout  = "<div class='resume'><div class='header'><h1>{{fullName}}</h1><p class='role'>{{targetJobTitle}}</p><p class='email'>{{email}}</p></div><div class='sections'>{{sections}}</div></div>",
                CssStyles   = ".resume{font-family:Poppins,sans-serif;max-width:800px;margin:0 auto;padding:40px;color:#2d2d2d}.header{background:linear-gradient(135deg,#7C6FE0,#A99EEA);padding:32px;margin:-40px -40px 32px;color:white;border-radius:0 0 20px 20px}h1{font-size:28px;font-weight:700;color:white}.role{font-size:15px;color:rgba(255,255,255,0.85)}.email{font-size:13px;color:rgba(255,255,255,0.7)}",
                Category    = "CREATIVE",
                IsPremium   = true,
                IsActive    = true
            }
        });
        db.SaveChanges();
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();