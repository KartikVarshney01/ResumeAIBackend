using MassTransit;
using ResumeAI.Export.API.Rendering;
using ResumeAI.Export.API.Services;
using ResumeAI.Shared.Events;

namespace ResumeAI.Shared.Events
{
    public class ExportRequestMessage
    {
        public int JobId { get; set; }
        public int UserId { get; set; }
        public int ResumeId { get; set; }
        public string Format { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? TargetJobTitle { get; set; }
        public IList<SectionMessage> Sections { get; set; } = new List<SectionMessage>();
    }

    public class SectionMessage
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ExportCompletedMessage
    {
        public int JobId { get; set; }
        public int UserId { get; set; }
        public int ResumeId { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

namespace ResumeAI.Export.API.Consumers
{
    public class ExportRequestConsumer : IConsumer<ExportRequestMessage>
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportRequestConsumer> _logger;

        public ExportRequestConsumer(IExportService exportService,
            ILogger<ExportRequestConsumer> logger)
        {
            _exportService = exportService;
            _logger        = logger;
        }

        public async Task Consume(ConsumeContext<ExportRequestMessage> context)
        {
            var msg = context.Message;

            _logger.LogInformation(
                "Processing export job {JobId} for user {UserId} in {Format}",
                msg.JobId, msg.UserId, msg.Format);

            // Map message to ResumeData for rendering
            var resumeData = new ResumeData
            {
                FullName       = msg.FullName,
                Email          = msg.Email,
                TargetJobTitle = msg.TargetJobTitle,
                Sections       = msg.Sections.Select(s => new SectionData
                {
                    Title   = s.Title,
                    Content = s.Content
                }).ToList()
            };

            await _exportService.ProcessExport(msg.JobId, resumeData);

            _logger.LogInformation(
                "Export job {JobId} completed successfully", msg.JobId);
        }
    }
}