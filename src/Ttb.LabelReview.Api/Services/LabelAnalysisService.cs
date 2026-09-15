using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ttb.LabelReview.Api.Models;

namespace Ttb.LabelReview.Api.Services;

public class LabelAnalysisService : ILabelAnalysisService
{
    private readonly IImagePreprocessingService _preprocessing;
    private readonly IOcrService _ocr;
    private readonly IFieldExtractionService _extraction;
    private readonly IComparisonService _comparison;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LabelAnalysisService> _logger;

    public LabelAnalysisService(
        IImagePreprocessingService preprocessing,
        IOcrService ocr,
        IFieldExtractionService extraction,
        IComparisonService comparison,
        IConfiguration configuration,
        ILogger<LabelAnalysisService> logger)
    {
        _preprocessing = preprocessing;
        _ocr = ocr;
        _extraction = extraction;
        _comparison = comparison;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LabelAnalysisResult> AnalyzeAsync(
        IFormFile labelImage,
        ApplicationData applicationData,
        CancellationToken ct)
    {
        var result = new LabelAnalysisResult
        {
            LabelFileName = labelImage.FileName
        };

        await using var stream = labelImage.OpenReadStream();
        var preprocessed = await _preprocessing.PrepareForOcrAsync(stream, ct);
        var ocrOutput = await _ocr.ExtractTextAsync(preprocessed, ct);

        result.RawOcrText = ocrOutput.Text;
        result.OcrConfidence = ocrOutput.Confidence;

        var minConfidence = _configuration.GetValue<double>("Ocr:MinimumConfidence", 0.55);
        if (ocrOutput.Confidence < minConfidence)
        {
            result.Warnings.Add(
                $"OCR confidence ({ocrOutput.Confidence:P0}) is below the configured threshold ({minConfidence:P0}). " +
                "Extracted text may be unreliable; manual review is recommended regardless of field results.");
        }

        var extracted = _extraction.Extract(ocrOutput.Text);
        result.FieldResults = _comparison.Compare(applicationData, ocrOutput.Text, extracted);

        result.Outcome = DetermineOutcome(result);
        return result;
    }

    private static ReviewOutcome DetermineOutcome(LabelAnalysisResult result)
    {
        if (result.Warnings.Count > 0)
            return ReviewOutcome.NeedsManualReview;

        var required = result.FieldResults.Where(f => f.IsRequiredField).ToList();

        if (required.Any(f => f.Status is MatchStatus.Mismatch or MatchStatus.NotFound))
            return ReviewOutcome.Fail;

        if (required.Any(f => f.Status == MatchStatus.PartialMatch))
            return ReviewOutcome.NeedsManualReview;

        return ReviewOutcome.Pass;
    }
}
