using Razorpay.Api;

namespace ResumeAI.Payment.API.Services;

public class RazorpayService : IPaymentGateway
{
    private readonly IConfiguration _config;
    private readonly string _keyId;
    private readonly string _keySecret;

    public RazorpayService(IConfiguration config)
    {
        _config = config;
        _keyId = _config["Razorpay:KeyId"] ?? throw new ArgumentNullException("Razorpay:KeyId is missing");
        _keySecret = _config["Razorpay:KeySecret"] ?? throw new ArgumentNullException("Razorpay:KeySecret is missing");
    }

    public async Task<string> CreateOrder(decimal amount, string currency)
    {
        // Razorpay expects amount in paise (multiply by 100)
        var client = new RazorpayClient(_keyId, _keySecret);
        
        var options = new Dictionary<string, object>
        {
            { "amount", (int)(amount * 100) },
            { "currency", currency },
            { "receipt", $"receipt_{Guid.NewGuid().ToString("N").Substring(0, 10)}" }
        };

        return await Task.Run(() =>
        {
            Order order = client.Order.Create(options);
            return order["id"].ToString();
        });
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        try
        {
            string payload = orderId + "|" + paymentId;
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(_keySecret);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(payloadBytes);
                string generatedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return generatedSignature == signature.ToLower();
            }
        }
        catch
        {
            return false;
        }
    }
}
