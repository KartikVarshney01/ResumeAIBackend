using Microsoft.EntityFrameworkCore;
using ResumeAI.Payment.API.Models;

namespace ResumeAI.Payment.API.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);
    }
}
