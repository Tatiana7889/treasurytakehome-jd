using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ttb.LabelReview.Api.Models;
using Ttb.LabelReview.Api.Services;
using System.Text.Json.Serialization;

namespace Ttb.LabelReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelAnalysisController : ControllerBase
{
    private readonly IImagePreprocessingService _preprocessing;
    private readonly IOcrService _ocr;
    private readonly IFieldExtractionService _extraction;
    private readonly IComparisonService _comparison;
    private readonly ILogger<LabelAnalysisController> _logger;
    private readonly IConfiguration _configuration;

    public LabelAnalysisController(
        IImagePreprocessingService preprocessing,
        IOcrService ocr,
        IFieldExtractionService extraction,
        IComparisonService comparison,
        IConfiguration configuration,
        ILogger<LabelAnalysisController> logger)
    {
        _preprocessing = preprocessing;
        _ocr = ocr;
        _extraction = extraction;
        _comparison = comparison;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a single label image against the supplied application data
    /// and returns a field-by-field comparison plus an overall recommendation.
    /// </summary>
    /// <param name="labelImage">JPG, PNG, or single-page PDF of the label artwork.</param>
    /// <param name="applicationDataJson">JSON-serialized <see cref="ApplicationData"/>.</param>
    [HttpPost("analyze")]
    [RequestSizeLimit(20_000_000)]
    [ProducesResponseType(typeof(LabelAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LabelAnalysisResult>> Analyze(
        IFormFile labelImage,
        [FromForm] string applicationDataJson,
        CancellationToken ct)
    {
        if (labelImage is null || labelImage.Length == 0)
        {
            return BadRequest("A label image file is required.");
        }

        ApplicationData? applicationData;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            applicationData = JsonSerializer.Deserialize<ApplicationData>(applicationDataJson, options);
        }
        catch (JsonException)
        {
            return BadRequest("applicationDataJson is not valid JSON.");
        }

        if (applicationData is null)
        {
            return BadRequest("applicationDataJson could not be parsed into application data.");
        }

        var allowedTypes = _configuration.GetSection("LabelReview:AllowedFileTypes").Get<string[]>() ?? Array.Empty<string>();
        var extension = Path.GetExtension(labelImage.FileName).ToLowerInvariant();
        if (!allowedTypes.Contains(extension))
        {
            return BadRequest($"File type '{extension}' is not supported. Allowed types: {string.Join(", ", allowedTypes)}");
        }
        
        var stopwatch = Stopwatch.StartNew();
    try{
        var result = await AnalyzeLabelAsync(labelImage, applicationData, ct);
        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
          _logger.LogInformation("RAW JSON RECEIVED: " + applicationDataJson);

        return Ok(result);
    }
    catch(Exception ex)
        {
            _logger.LogInformation("RAW JSON RECEIVED: " + applicationDataJson);

           _logger.LogError(ex, "Error analyzing label");
        return StatusCode(500, ex.ToString()); 
        }
    }

    internal async Task<LabelAnalysisResult> AnalyzeLabelAsync(
        IFormFile labelImage, ApplicationData applicationData, CancellationToken ct)
    {
        var result = new LabelAnalysisResult { LabelFileName = labelImage.FileName };

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
        {
            return ReviewOutcome.NeedsManualReview;
        }

        var requiredResults = result.FieldResults.Where(f => f.IsRequiredField).ToList();

        if (requiredResults.Any(f => f.Status is MatchStatus.Mismatch or MatchStatus.NotFound))
        {
            return ReviewOutcome.Fail;
        }

        if (requiredResults.Any(f => f.Status == MatchStatus.PartialMatch))
        {
            return ReviewOutcome.NeedsManualReview;
        }

        return ReviewOutcome.Pass;
    }
}
