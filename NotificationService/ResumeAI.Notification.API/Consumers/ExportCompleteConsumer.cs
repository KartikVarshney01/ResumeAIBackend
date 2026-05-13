using MassTransit;
using ResumeAI.Notification.API.Services;
using ResumeAI.Shared.Events;

namespace ResumeAI.Shared.Events
{
    public class ExportCompletedMessage
    {
        public int JobId { get; set; }
        public int UserId { get; set; }
        public int ResumeId { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

namespace ResumeAI.Notification.API.Consumers
{
    public class ExportCompleteConsumer : IConsumer<ExportCompletedMessage>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ExportCompleteConsumer> _logger;

        public ExportCompleteConsumer(
            INotificationService notificationService,
            ILogger<ExportCompleteConsumer> logger)
        {
            _notificationService = notificationService;
            _logger              = logger;
        }

        public async Task Consume(ConsumeContext<ExportCompletedMessage> context)
        {
            var msg = context.Message;

            if (msg.Status != "COMPLETED") return;

            _logger.LogInformation(
                "Sending export completion notification to user {UserId} for job {JobId}", 
                msg.UserId, msg.JobId);

            await _notificationService.CreateAndSend(
                userId:    msg.UserId,
                type:      "EXPORT_COMPLETE",
                title:     "Your Resume is Ready!",
                message:   $"Your {msg.Format} export has been generated successfully.",
                actionUrl: $"/export"
            );
        }
    }
}