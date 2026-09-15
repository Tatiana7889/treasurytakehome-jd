using System.Text.RegularExpressions;

namespace Ttb.LabelReview.Api.Utils;

/// <summary>
/// Centralized regex patterns used to locate TTB-required fields inside raw
/// OCR text. Keeping these in one place makes it easy to tune extraction
/// without hunting through service logic, and easy to unit test in isolation.
/// </summary>
public static class RegexPatterns
{
    // Matches things like "13.5% ALC/VOL", "ALC 13.5% BY VOL", "13.5% ALCOHOL BY VOLUME"
    public static readonly Regex AlcoholContent = new(
        @"(?<value>\d{1,2}(\.\d{1,2})?)\s*%\s*(ALC(OHOL)?[\s./]*(BY)?[\s./]*VOL(UME)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches net contents like "750 mL", "750ML", "12 FL OZ", "1 L", "12 x 355 mL"
    public static readonly Regex NetContents = new(
        @"(?<value>\d+(\.\d+)?\s*(mL|ML|ml|L|l|FL\.?\s?OZ\.?|fl\.?\s?oz\.?|OZ|oz))",
        RegexOptions.Compiled);

    // Matches the mandatory opening of the Government Warning statement.
    public static readonly Regex GovernmentWarningHeader = new(
        @"GOVERNMENT\s+WARNING\s*:?\s*\(?1\)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Loosely matches "Bottled by", "Produced by", "Imported by" lines,
    // capturing the remainder of the line as a name/address candidate.
    public static readonly Regex BottlerProducerLine = new(
        @"(BOTTLED|PRODUCED|DISTILLED|BREWED|IMPORTED)\s+(AND\s+BOTTLED\s+)?BY[:\s]+(?<value>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Country of origin, typically "Product of <Country>" or "Imported from <Country>"
    public static readonly Regex CountryOfOrigin = new(
        @"(PRODUCT\s+OF|IMPORTED\s+FROM)\s+(?<value>[A-Za-z\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Normalizes whitespace and OCR line-break artifacts so downstream
    /// regexes match reliably regardless of how Tesseract wrapped lines.
    /// </summary>
    public static string NormalizeForMatching(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();
}
