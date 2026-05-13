using MassTransit;
using ResumeAI.Notification.API.Services;

namespace ResumeAI.Notification.API.Consumers;

// Message contract — must match AI service
public class AiRequestCompleteMessage
{
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public int ResumeId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AiCompleteConsumer : IConsumer<AiRequestCompleteMessage>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AiCompleteConsumer> _logger;

    public AiCompleteConsumer(
        INotificationService notificationService,
        ILogger<AiCompleteConsumer> logger)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    public async Task Consume(ConsumeContext<AiRequestCompleteMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Sending AI notification to user {UserId}", msg.UserId);

        var title = msg.RequestType switch
        {
            "GENERATE_SUMMARY" => "Summary Generated!",
            "IMPROVE_BULLET"   => "Bullet Point Improved!",
            "ATS_CHECK"        => "ATS Check Complete!",
            "TAILOR_JOB"       => "Resume Tailored!",
            "SKILL_SUGGEST"    => "Skills Suggested!",
            _                  => "AI Task Complete!"
        };

        await _notificationService.CreateAndSend(
            userId:    msg.UserId,
            type:      "AI_COMPLETE",
            title:     title,
            message:   $"Your AI request has been processed successfully.",
            actionUrl: $"/resumes/{msg.ResumeId}"
        );
    }
}