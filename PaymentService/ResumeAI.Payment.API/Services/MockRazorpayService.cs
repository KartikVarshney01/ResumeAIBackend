namespace ResumeAI.Payment.API.Services;

public class MockRazorpayService : IPaymentGateway
{
    private readonly IConfiguration _config;

    public MockRazorpayService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> CreateOrder(decimal amount, string currency)
    {
        // Simulate API call delay
        await Task.Delay(100);
        
        // Return a mock order ID
        return $"order_mock_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        // In a mock implementation, we always return true if the signature starts with 'mock_sig'
        // or just return true for development purposes.
        return !string.IsNullOrEmpty(signature);
    }
}
