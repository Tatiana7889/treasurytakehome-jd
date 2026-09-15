namespace Ttb.LabelReview.Api.Models;

/// <summary>
/// The outcome of comparing one required field between the application data
/// and the text extracted from the label image.
/// </summary>
public class FieldComparisonResult
{
    public string FieldName { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>
    /// The best-matching text found on the label, or null if the field
    /// could not be located at all.
    /// </summary>
    public string? FoundValue { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.NotFound;

    /// <summary>
    /// 0.0–1.0 similarity score between expected and found values.
    /// Not meaningful when Status is NotFound.
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Human-readable explanation shown to the reviewing agent, e.g.
    /// "Net contents on label (750mL) does not match application (700mL)."
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    public bool IsRequiredField { get; set; } = true;
}

public enum MatchStatus
{
    Match,
    PartialMatch,
    Mismatch,
    NotFound
}
