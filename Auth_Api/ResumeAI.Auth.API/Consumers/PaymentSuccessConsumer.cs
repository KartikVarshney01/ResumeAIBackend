using MassTransit;
using Microsoft.EntityFrameworkCore;
using ResumeAI.Auth.API.Data;

namespace ResumeAI.Shared.Events
{
    public class PaymentSucceededMessage
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PlanName { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

namespace ResumeAI.Auth.API.Consumers
{
    public class PaymentSuccessConsumer : IConsumer<ResumeAI.Shared.Events.PaymentSucceededMessage>
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<PaymentSuccessConsumer> _logger;

        public PaymentSuccessConsumer(AuthDbContext context, ILogger<PaymentSuccessConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ResumeAI.Shared.Events.PaymentSucceededMessage> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Processing payment success for User: {UserId}, Plan: {PlanName}", msg.UserId, msg.PlanName);

            var user = await _context.Users.FindAsync(msg.UserId);
            if (user != null)
            {
                user.SubscriptionPlan = "PREMIUM";
                user.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} upgraded to PREMIUM successfully", msg.UserId);
            }
            else
            {
                _logger.LogWarning("User {UserId} not found for payment upgrade", msg.UserId);
            }
        }
    }
}
