using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Payment.API.Models;

public enum PaymentStatus
{
    Pending,
    Captured,
    Failed,
    Refunded
}

public class PaymentTransaction
{
    [Key]
    public int TransactionId { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public string Currency { get; set; } = "INR";
    
    public string? RazorpayOrderId { get; set; }
    
    public string? RazorpayPaymentId { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public string? PlanName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
