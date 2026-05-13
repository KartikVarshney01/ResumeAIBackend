namespace ResumeAI.Shared.Events;

public class PaymentSucceededMessage
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PlanName { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
