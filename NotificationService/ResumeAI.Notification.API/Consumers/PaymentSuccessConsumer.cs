using MassTransit;
using ResumeAI.Notification.API.Services;

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

namespace ResumeAI.Notification.API.Consumers
{
    public class PaymentSuccessConsumer : IConsumer<ResumeAI.Shared.Events.PaymentSucceededMessage>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentSuccessConsumer> _logger;

        public PaymentSuccessConsumer(
            INotificationService notificationService,
            ILogger<PaymentSuccessConsumer> logger)
        {
            _notificationService = notificationService;
            _logger              = logger;
        }

        public async Task Consume(ConsumeContext<ResumeAI.Shared.Events.PaymentSucceededMessage> context)
        {
            var msg = context.Message;

            _logger.LogInformation(
                "Processing payment success notification for user {UserId}, amount {Amount}", 
                msg.UserId, msg.Amount);

            await _notificationService.CreateAndSend(
                userId:    msg.UserId,
                type:      "PAYMENT_SUCCESS",
                title:     "Payment Successful!",
                message:   $"Thank you for upgrading to {msg.PlanName}. Your payment of {msg.Currency} {msg.Amount} was successful.",
                actionUrl: $"/dashboard"
            );
        }
    }
}
