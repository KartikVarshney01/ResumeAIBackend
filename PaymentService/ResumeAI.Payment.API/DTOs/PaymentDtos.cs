namespace ResumeAI.Payment.API.DTOs;

public record CreateOrderRequest(int UserId, decimal Amount, string PlanName);
public record CreateOrderResponse(string OrderId, decimal Amount, string Currency, string KeyId);

public record VerifyPaymentRequest(
    string RazorpayOrderId, 
    string RazorpayPaymentId, 
    string RazorpaySignature,
    int UserId
);

public record PaymentStatusResponse(string Status, string? TransactionId);
