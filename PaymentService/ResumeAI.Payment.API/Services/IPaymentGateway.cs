namespace ResumeAI.Payment.API.Services;

public interface IPaymentGateway
{
    Task<string> CreateOrder(decimal amount, string currency);
    bool VerifySignature(string orderId, string paymentId, string signature);
}
