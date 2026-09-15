using Tesseract;

namespace Ttb.LabelReview.Api.Services;

public sealed record OcrOutput(string Text, float Confidence);

public interface IOcrService
{
    Task<OcrOutput> ExtractTextAsync(Pix pix, CancellationToken ct = default);
}

public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly string _tessDataPath;
    private readonly string _language;

    public OcrService(ILogger<OcrService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _tessDataPath = Path.Combine(env.ContentRootPath, "tessdata");
        _language = "eng";
    }

    public Task<OcrOutput> ExtractTextAsync(Pix pix, CancellationToken ct = default)
    {
        // Tesseract wrapper is synchronous; isolate per call
        return Task.Run(() =>
        {
            using var engine = new TesseractEngine(_tessDataPath, _language, EngineMode.LstmOnly);
            using var page = engine.Process(pix);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            _logger.LogInformation("OCR completed with mean confidence {Confidence:P0}", confidence);
            return new OcrOutput(text, confidence);
        }, ct);
    }
}

