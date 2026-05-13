using Azure.AI.OpenAI;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using StackExchange.Redis;
using ResumeAI.AI.API.Data;
using ResumeAI.AI.API.Models;
using ResumeAI.AI.API.Repositories;
using System.Text.Json;
using System.Text;

namespace ResumeAI.AI.API.Services;

public interface IAiService
{
    Task<AiRequest> GenerateSummary(int userId, int resumeId, string resumeContent);
    Task<AiRequest> ImproveBullet(int userId, int resumeId, string bulletText);
    Task<AiRequest> CheckAtsCompatibility(int userId, int resumeId, string resumeContent, string jobDescription);
    Task<AiRequest> TailorForJob(int userId, int resumeId, string resumeContent, string jobDescription);
    Task<AiRequest> SuggestSkills(int userId, int resumeId, string currentSkills, string jobDescription);
    Task<int> GetRemainingQuota(int userId, string plan);
    Task<(string Response, string Provider)> CallAi(string prompt);
}

public class AiService : IAiService
{
    private readonly IAiRequestRepository _repo;
    private readonly AiDbContext _ctx;
    private readonly IDatabase _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<AiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIClient? _openAiClient;
    private readonly AnthropicClient? _anthropicClient;

    private const int FREE_MONTHLY_LIMIT = 50;
    private const int PREMIUM_MONTHLY_LIMIT = 100;

    public AiService(
        IAiRequestRepository repo,
        AiDbContext ctx,
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<AiService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _repo = repo;
        _ctx = ctx;
        _redis = redis.GetDatabase();
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        // Initialize clients only if keys are present
        var openAiKey = config["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAiKey) && openAiKey != "YOUR_OPENAI_API_KEY")
            _openAiClient = new OpenAIClient(openAiKey);

        var anthropicKey = config["Anthropic:ApiKey"];
        if (!string.IsNullOrEmpty(anthropicKey) && anthropicKey != "YOUR_ANTHROPIC_API_KEY")
            _anthropicClient = new AnthropicClient(anthropicKey);
    }

    public async Task<AiRequest> GenerateSummary(int userId, int resumeId, string resumeContent)
    {
        var prompt = $"Generate a professional resume summary for the following resume content:\n\n{resumeContent}\n\nWrite 3-4 sentences highlighting key skills and experience.";
        return await ProcessRequest(userId, resumeId, "GENERATE_SUMMARY", prompt);
    }

    public async Task<AiRequest> ImproveBullet(int userId, int resumeId, string bulletText)
    {
        var prompt = $"Improve this resume bullet point to be more impactful and quantifiable:\n\n{bulletText}\n\nMake it start with a strong action verb and include measurable results where possible.";
        return await ProcessRequest(userId, resumeId, "IMPROVE_BULLET", prompt);
    }

    public async Task<AiRequest> CheckAtsCompatibility(int userId, int resumeId, string resumeContent, string jobDescription)
    {
        var prompt = $"Analyze this resume for ATS compatibility against the job description.\n\nResume:\n{resumeContent}\n\nJob Description:\n{jobDescription}\n\nReturn a score from 0-100 and list missing keywords.";
        return await ProcessRequest(userId, resumeId, "ATS_CHECK", prompt);
    }

    public async Task<AiRequest> TailorForJob(int userId, int resumeId, string resumeContent, string jobDescription)
    {
        var prompt = $"Tailor this resume for the following job description.\n\nResume:\n{resumeContent}\n\nJob Description:\n{jobDescription}\n\nSuggest specific changes to improve match.";
        return await ProcessRequest(userId, resumeId, "TAILOR_JOB", prompt);
    }

    public async Task<AiRequest> SuggestSkills(int userId, int resumeId, string currentSkills, string jobDescription)
    {
        var prompt = $"Based on this job description, suggest additional skills to add to the resume.\n\nCurrent Skills:\n{currentSkills}\n\nJob Description:\n{jobDescription}\n\nList the top 10 missing skills.";
        return await ProcessRequest(userId, resumeId, "SKILL_SUGGEST", prompt);
    }

    public async Task<int> GetRemainingQuota(int userId, string plan)
    {
        var key = $"quota:{userId}:{DateTime.UtcNow:yyyy-MM}";
        var used = await _redis.StringGetAsync(key);
        var usedCount = (int)(used.HasValue ? (long)used : 0);
        var limit = plan == "PREMIUM" ? PREMIUM_MONTHLY_LIMIT : FREE_MONTHLY_LIMIT;
        
        var remaining = limit - usedCount;
        return remaining < 0 ? 0 : remaining;
    }

    public async Task<(string Response, string Provider)> CallAi(string prompt)
    {
        // 1. Try OpenAI
        if (_openAiClient != null)
        {
            try
            {
                return (await CallOpenAi(prompt), "OPENAI");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OpenAI failed: {Message}. Falling back to Anthropic.", ex.Message);
            }
        }

        // 2. Try Anthropic
        if (_anthropicClient != null)
        {
            try
            {
                return (await CallClaude(prompt), "ANTHROPIC");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Anthropic failed: {Message}. Falling back to Gemini.", ex.Message);
            }
        }

        // 3. Try Gemini
        var geminiKey = _config["Gemini:ApiKey"];
        if (!string.IsNullOrEmpty(geminiKey) && geminiKey != "YOUR_GEMINI_API_KEY")
        {
            try
            {
                return (await CallGemini(prompt, geminiKey), "GEMINI");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Gemini failed: {Message}. Falling back to Demo Mode.", ex.Message);
            }
        }

        // 4. Final Fallback: Demo Mode
        return (GetDemoResponse(prompt), "DEMO");
    }

    private string GetDemoResponse(string prompt)
    {
        if (prompt.Contains("professional resume summary"))
            return "Highly motivated and results-driven professional with over 5 years of experience in software development. Expert in building scalable web applications using modern frameworks and cloud technologies. Proven track record of delivering high-quality code and leading cross-functional teams to success.";
        
        if (prompt.Contains("Improve this resume bullet point"))
            return "Optimized database queries and implemented caching strategies, resulting in a 40% reduction in API response times and improved overall system stability for 100k+ monthly active users.";
            
        if (prompt.Contains("ATS compatibility"))
            return "ATS Score: 85/100\n\nMissing Keywords: Kubernetes, Docker, Microservices, CI/CD, Terraform.\n\nSuggestions: Add more specific achievements in your experience section and highlight your cloud infrastructure skills.";

        return "This is a simulated AI response for demo purposes. To get real AI suggestions, please provide a valid OpenAI, Anthropic, or Gemini API key in the appsettings.json file.";
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AiRequest> ProcessRequest(
        int userId, int resumeId, string requestType, string prompt)
    {
        // Check quota
        await EnforceQuota(userId);

        var request = new AiRequest
        {
            UserId = userId,
            ResumeId = resumeId,
            RequestType = requestType,
            Prompt = prompt,
            Status = "PROCESSING"
        };

        await _repo.Save(request);

        try
        {
            var (response, provider) = await CallAi(prompt);

            request.Response = response;
            request.Status = "COMPLETED";
            request.AiProvider = provider;
            request.CompletedAt = DateTime.UtcNow;

            await _repo.Update(request);

            // Increment Redis quota counter
            try 
            {
                var key = $"quota:{userId}:{DateTime.UtcNow:yyyy-MM}";
                await _redis.StringIncrementAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis quota increment failed: {Message}", ex.Message);
            }

            // Set TTL to end of month if not set
            try
            {
                var key = $"quota:{userId}:{DateTime.UtcNow:yyyy-MM}";
                var ttl = await _redis.KeyTimeToLiveAsync(key);
                if (ttl is null)
                {
                    var endOfMonth = new DateTime(
                        DateTime.UtcNow.Year,
                        DateTime.UtcNow.Month, 1)
                        .AddMonths(1) - DateTime.UtcNow;
                    await _redis.KeyExpireAsync(key, endOfMonth);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis TTL set failed: {Message}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            request.Status = "FAILED";
            request.Response = ex.Message;
            await _repo.Update(request);
        }

        return request;
    }

    private async Task EnforceQuota(int userId)
    {
        try 
        {
            var key = $"quota:{userId}:{DateTime.UtcNow:yyyy-MM}";
            var used = await _redis.StringGetAsync(key);
            var count = used.HasValue ? (int)(long)used : 0;

            if (count >= FREE_MONTHLY_LIMIT)
                throw new InvalidOperationException("Monthly AI quota exceeded. Upgrade to Premium.");
        }
        catch (RedisConnectionException)
        {
            _logger.LogWarning("Redis is unavailable. Bypassing quota enforcement.");
        }
    }

    private async Task<string> CallOpenAi(string prompt)
    {
        if (_openAiClient == null) throw new InvalidOperationException("OpenAI client not initialized.");

        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4o",
            Messages =
            {
                new ChatRequestSystemMessage("You are an expert resume writer and career coach."),
                new ChatRequestUserMessage(prompt)
            },
            MaxTokens = 1000
        };

        var response = await _openAiClient.GetChatCompletionsAsync(options);
        return response.Value.Choices[0].Message.Content;
    }

    private async Task<string> CallClaude(string prompt)
    {
        if (_anthropicClient == null) throw new InvalidOperationException("Anthropic client not initialized.");

        var messages = new List<Message>
        {
            new Message
            {
                Role    = RoleType.User,
                Content = new List<ContentBase>
                {
                    new TextContent { Text = prompt }
                }
            }
        };

        var request = new MessageParameters
        {
            Model = "claude-3-5-sonnet-20241022",
            MaxTokens = 1000,
            Messages = messages
        };

        var response = await _anthropicClient.Messages.GetClaudeMessageAsync(request);
        return response.Content[0].ToString() ?? string.Empty;
    }

    private async Task<string> CallGemini(string prompt, string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseString);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }
}