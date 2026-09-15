namespace Ttb.LabelReview.Api.Models;

/// <summary>
/// The full result of analyzing a single label image against its
/// corresponding application data.
/// </summary>
public class LabelAnalysisResult
{
    public string LabelFileName { get; set; } = string.Empty;

    /// <summary>
    /// Raw OCR output before field extraction, kept for auditability and
    /// so a reviewer can spot OCR errors versus genuine label problems.
    /// </summary>
    public string RawOcrText { get; set; } = string.Empty;

    public double OcrConfidence { get; set; }

    public List<FieldComparisonResult> FieldResults { get; set; } = new();

    /// <summary>
    /// Overall recommendation, derived from the individual field results.
    /// This is a decision aid for the compliance agent, not a final ruling.
    /// </summary>
    public ReviewOutcome Outcome { get; set; } = ReviewOutcome.NeedsManualReview;

    public DateTimeOffset ProcessedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public long ProcessingTimeMs { get; set; }

    public List<string> Warnings { get; set; } = new();
}

public enum ReviewOutcome
{
    /// <summary>All required fields matched within tolerance.</summary>
    Pass,

    /// <summary>One or more required fields mismatched or were missing.</summary>
    Fail,

    /// <summary>OCR confidence too low, or ambiguous results, to auto-decide.</summary>
    NeedsManualReview
}
