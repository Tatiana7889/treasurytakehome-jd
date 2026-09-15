namespace Ttb.LabelReview.Api.Models;

/// <summary>
/// Aggregate result for a batch submission (ZIP of label images plus a CSV
/// mapping file names to application data).
/// </summary>
public class BatchResult
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString("N");

    public int TotalLabels { get; set; }

    public int PassCount { get; set; }

    public int FailCount { get; set; }

    public int NeedsReviewCount { get; set; }

    public List<LabelAnalysisResult> Results { get; set; } = new();

    public List<string> SkippedFiles { get; set; } = new();

    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
