using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ttb.LabelReview.Api.Services;

public interface IVisionFallbackService
{
    /// <summary>
    /// TT 09/15/26: Asks a multimodal vision model to read the brand name directly off the
    /// label image. Used as a fallback when OCR + fuzzy matching can't
    /// </summary>
    Task<string?> ReadBrandNameAsync(byte[] imageBytes, string contentType, CancellationToken ct = default);
}

public class GeminiVisionFallbackService : IVisionFallbackService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiVisionFallbackService> _logger;

    public GeminiVisionFallbackService(
        HttpClient httpClient, IConfiguration configuration, ILogger<GeminiVisionFallbackService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> ReadBrandNameAsync(byte[] imageBytes, string contentType, CancellationToken ct = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini:ApiKey is not configured; skipping vision fallback for brand name.");
            return null;
        }

        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            text = "This is a photo of an alcoholic beverage label. Identify only the " +
                                   "brand name / wordmark as printed on the label (the large stylized " +
                                   "logo text, not the class/type line below it). Respond with just the " +
                                   "brand name text and nothing else. If you cannot confidently read it, " +
                                   "respond with exactly: UNKNOWN"
                        },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = contentType,
                                data = Convert.ToBase64String(imageBytes)
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0,
                maxOutputTokens = 32
            }
        };

        try
        {
            using var response = await _httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Gemini vision fallback returned {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var parsed = await JsonSerializer.DeserializeAsync<GeminiResponse>(stream, cancellationToken: ct);

            var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                return null;

            return text;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Gemini vision fallback call failed");
            return null;
        }
    }

    // Minimal DTOs for the slice of the Gemini response we need.
    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
