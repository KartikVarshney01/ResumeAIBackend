using Azure.Storage.Blobs;
using ResumeAI.Export.API.Models;
using ResumeAI.Export.API.Rendering;
using ResumeAI.Export.API.Repositories;
using MassTransit;
using ResumeAI.Shared.Events;

namespace ResumeAI.Export.API.Services;

public interface IExportService
{
    Task<ExportJob> RequestExport(int userId, int resumeId, string format);
    Task<ExportJob?> GetJobById(int jobId);
    Task<IList<ExportJob>> GetUserExports(int userId);
    Task<IList<ExportJob>> GetResumeExports(int resumeId);
    Task ProcessExport(int jobId, ResumeData resumeData);
}

public class ExportService : IExportService
{
    private readonly IExportJobRepository _repo;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly IDocxRenderer _docxRenderer;
    private readonly IConfiguration _config;
    private readonly IPublishEndpoint _publishEndpoint;

    public ExportService(
        IExportJobRepository repo,
        IPdfRenderer pdfRenderer,
        IDocxRenderer docxRenderer,
        IConfiguration config,
        IPublishEndpoint publishEndpoint)
    {
        _repo         = repo;
        _pdfRenderer  = pdfRenderer;
        _docxRenderer = docxRenderer;
        _config       = config;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ExportJob> RequestExport(int userId, int resumeId, string format)
    {
        if (format != "PDF" && format != "DOCX")
            throw new InvalidOperationException("Format must be PDF or DOCX");

        var job = new ExportJob
        {
            UserId      = userId,
            ResumeId    = resumeId,
            Format      = format.ToUpper(),
            Status      = "PENDING",
            RequestedAt = DateTime.UtcNow
        };

        return await _repo.Save(job);
    }

    public async Task<ExportJob?> GetJobById(int jobId) =>
        await _repo.GetById(jobId);

    public async Task<IList<ExportJob>> GetUserExports(int userId) =>
        await _repo.GetByUserId(userId);

    public async Task<IList<ExportJob>> GetResumeExports(int resumeId) =>
        await _repo.GetByResumeId(resumeId);

    public async Task ProcessExport(int jobId, ResumeData resumeData)
    {
        var job = await _repo.GetById(jobId)
            ?? throw new KeyNotFoundException("Export job not found");

        job.Status = "PROCESSING";
        await _repo.Update(job);

        try
        {
            // Generate file bytes
            byte[] fileBytes = job.Format == "PDF"
                ? _pdfRenderer.Render(resumeData)
                : _docxRenderer.Render(resumeData);

            // Upload to Azure Blob Storage
            var fileUrl = await UploadToBlob(
                fileBytes,
                job.Format,
                job.UserId,
                job.ResumeId);

            job.Status        = "COMPLETED";
            job.FileUrl       = fileUrl;
            job.FileSizeBytes = fileBytes.Length;
            job.CompletedAt   = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.Status       = "FAILED";
            job.ErrorMessage = ex.Message;
        }

        await _repo.Update(job);

        // Notify other services (like Notifications)
        await _publishEndpoint.Publish(new ExportCompletedMessage
        {
            JobId    = job.ExportJobId,
            UserId   = job.UserId,
            ResumeId = job.ResumeId,
            Format   = job.Format,
            Status   = job.Status
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> UploadToBlob(
        byte[] fileBytes, string format, int userId, int resumeId)
    {
        var connectionString = _config["AzureStorage:ConnectionString"];
        var containerName    = _config["AzureStorage:ContainerName"] ?? "exports";

        // If no Azure storage configured — save locally for dev
        if (string.IsNullOrEmpty(connectionString) ||
            connectionString == "YOUR_AZURE_STORAGE_CONNECTION_STRING")
        {
            return await SaveLocally(fileBytes, format, userId, resumeId);
        }

        var blobClient = new BlobContainerClient(connectionString, containerName);
        await blobClient.CreateIfNotExistsAsync();

        var fileName = $"{userId}/{resumeId}/{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToLower()}";
        var blob     = blobClient.GetBlobClient(fileName);

        using var stream = new MemoryStream(fileBytes);
        await blob.UploadAsync(stream, overwrite: true);

        return blob.Uri.ToString();
    }

    private static async Task<string> SaveLocally(
        byte[] fileBytes, string format, int userId, int resumeId)
    {
        var dir  = Path.Combine("exports", userId.ToString(), resumeId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToLower()}";
        var path     = Path.Combine(dir, fileName);

        await File.WriteAllBytesAsync(path, fileBytes);
        return $"/exports/{userId}/{resumeId}/{fileName}";
    }
}