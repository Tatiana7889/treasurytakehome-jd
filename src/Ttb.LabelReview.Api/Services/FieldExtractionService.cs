using Ttb.LabelReview.Api.Utils;
using System.Text.RegularExpressions;
namespace Ttb.LabelReview.Api.Services;
using Microsoft.Extensions.Logging;
public class ExtractedFields
{
    public string? AlcoholContent { get; set; }
    public string? NetContents { get; set; }
    public bool GovernmentWarningPresent { get; set; }
    public string? GovernmentWarningText { get; set; }
    public string? BottlerOrProducer { get; set; }
    public string? CountryOfOrigin { get; set; }
}

public interface IFieldExtractionService
{
    ExtractedFields Extract(string rawOcrText);
}

public class FieldExtractionService : IFieldExtractionService
{
    private readonly ILogger<FieldExtractionService> _logger;

    public FieldExtractionService(ILogger<FieldExtractionService> logger)
    {
        _logger = logger;
    }

    public ExtractedFields Extract(string rawOcrText)
    {
        var normalized = Normalize(rawOcrText);
        var result = new ExtractedFields();

        // -----------------------------
        // ALCOHOL CONTENT
        // -----------------------------
        var alcoholMatch = AlcoholContentRegex().Match(normalized);
        if (alcoholMatch.Success)
        {
            var pct = alcoholMatch.Groups["value"].Value.Trim();
            result.AlcoholContent = $"{pct}% ALC/VOL";
        }

        var netMatch = NetContentsRegex().Match(normalized);
        if (netMatch.Success)
        {
            var raw = netMatch.Groups["value"].Value.Trim();

            // Split amount + unit
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var amount = parts[0];
                var unit = parts[1];

                // Fix unit casing
                unit = unit.Equals("ML", StringComparison.OrdinalIgnoreCase)
                    ? "mL"
                    : unit;

                result.NetContents = $"{amount} {unit}";
            }
            else
            {
                // Fallback: preserve original
                result.NetContents = raw;
            }
        }

        // -----------------------------
        // GOVERNMENT WARNING
        // -----------------------------
        var warningMatch = GovernmentWarningRegex().Match(normalized);
        result.GovernmentWarningPresent = warningMatch.Success;

        if (warningMatch.Success)
        {
            var start = warningMatch.Index;
            var length = Math.Min(600, normalized.Length - start);
            result.GovernmentWarningText = normalized.Substring(start, length);
        }

        // -----------------------------
        // BOTTLER / PRODUCER
        // -----------------------------
        var bottlerMatch = BottlerRegex().Match(normalized);
        if (bottlerMatch.Success)
        {
            result.BottlerOrProducer = bottlerMatch.Groups["value"].Value.Trim();
        }

        // -----------------------------
        // COUNTRY OF ORIGIN
        // -----------------------------
        var originMatch = CountryRegex().Match(normalized);
        if (originMatch.Success)
        {
            result.CountryOfOrigin = originMatch.Groups["value"].Value.Trim();
        }

        _logger.LogDebug(
            "Extracted fields: alcohol={Alcohol}, net={Net}, warning={Warning}",
            result.AlcoholContent, result.NetContents, result.GovernmentWarningPresent);

        return result;
    }

    // ============================================================
    // Normalization
    // ============================================================
    private static string Normalize(string input)
    {
        return input
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("ALC.", "ALC")
            .Replace("VOL.", "VOL")
            .Replace("V0L", "VOL")   // OCR noise
            .Replace("BY V0L", "BY VOL")
            .ToUpperInvariant();
    }

    // ============================================================
    // Regex Patterns
    // ============================================================

    // Alcohol content: matches all test formats
    private static Regex AlcoholContentRegex() => new(
    // 1) "45% ALC/VOL" or "45% ALC / VOL"
    @"(?<value>\d{1,2}(\.\d)?)\s*%\s*ALC\s*/?\s*VOL\b"
    + "|" +
    // 2) "45% ALC BY VOL"
    @"(?<value>\d{1,2}(\.\d)?)\s*%\s*ALC\s*BY\s*VOL\b"
    + "|" +
    // 3) "ALC 45% BY VOL"
    @"ALC\s*(?<value>\d{1,2}(\.\d)?)\s*%\s*BY\s*VOL\b"
    + "|" +
    // 4) "12.0 % ALCOHOL BY VOLUME"
    @"(?<value>\d{1,2}(\.\d)?)\s*%\s*ALCOHOL\s*BY\s*VOLUME\b"
    + "|" +
    // 5) "45% ABV"
    @"(?<value>\d{1,2}(\.\d)?)\s*%\s*ABV\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Net contents: matches "NET CONTENTS 750 mL" and "12 FL OZ"
    private static Regex NetContentsRegex() => new(
        @"NET\s*CONTENTS\s*(?<value>\d+\s*(mL|L|FL\s*OZ))"
        + "|" +
        @"(?<value>\d+\s*(mL|L|FL\s*OZ))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Government warning header
    private static Regex GovernmentWarningRegex() => new(
        @"GOVERNMENT\s*WARNING",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Bottler / producer line
    private static Regex BottlerRegex() => new(
        @"(PRODUCED\s*AND\s*BOTTLED\s*BY|BOTTLED\s*BY|PRODUCED\s*BY)\s*(?<value>[^,]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Country of origin
    private static Regex CountryRegex() => new(
        @"PRODUCT\s*OF\s*(?<value>[A-Z\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
}
