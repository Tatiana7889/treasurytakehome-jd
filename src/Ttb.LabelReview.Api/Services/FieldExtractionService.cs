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
            var pct = NormalizeOcrDigits(alcoholMatch.Groups["value"].Value.Trim());
            result.AlcoholContent = $"{pct}% ALC/VOL";
        }

        // -----------------------------
        // NET CONTENTS
        // -----------------------------
        var netMatch = NetContentsRegex().Match(normalized);
        if (netMatch.Success)
        {
            result.NetContents = NormalizeOcrUnit(netMatch.Groups["value"].Value.Trim());
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

    /// <summary>
    /// Corrects the A/4 OCR confusion within an already-matched alcohol
    /// percentage value (e.g. "A5" -> "45"). Scoped to just this captured
    /// substring, not applied to the document at large, since blindly
    /// replacing every "A" with "4" would corrupt unrelated text (it would
    /// even break the literal word "ALC" a few characters away).
    /// </summary>
    private static string NormalizeOcrDigits(string value) =>
        value.Replace('A', '4').Replace('a', '4');

    /// <summary>
    /// Cleans up a captured net-contents value so "750MI", "750M", and
    /// "750mL" all normalize to a consistent "750ML" — matching how the
    /// unit alternation in NetContentsRegex tolerates the same OCR
    /// confusions (l/i and dropped "L") on the way in.
    /// </summary>
    private static string NormalizeOcrUnit(string value)
    {
        var upper = value.ToUpperInvariant();
        if (System.Text.RegularExpressions.Regex.IsMatch(upper, @"\d\s*M[I1]?$"))
            return System.Text.RegularExpressions.Regex.Replace(upper, @"M[I1]?$", "ML");
        return upper;
    }

    // ============================================================
    // Regex Patterns
    // ============================================================

    // Alcohol content: matches all test formats.
    // OCR on bold/stylized display fonts frequently misreads the numeral
    // "4" as the letter "A" (confirmed against a real label). The value
    // group here accepts A/a in that position too — scoped tightly
    // (immediately followed by "%") to avoid false positives elsewhere in
    // the document; NormalizeOcrDigits() below then corrects it back to
    // "4" only within the matched value itself, never touching the rest
    // of the text.
    private static Regex AlcoholContentRegex() => new(
    // 1) "45% ALC/VOL" or "45% ALC / VOL" (also tolerates "A5%" misread)
    @"(?<value>[\dAa]{1,2}(\.\d)?)\s*%\s*ALC\s*/?\s*VOL\b"
    + "|" +
    // 2) "45% ALC BY VOL"
    @"(?<value>[\dAa]{1,2}(\.\d)?)\s*%\s*ALC\s*BY\s*VOL\b"
    + "|" +
    // 3) "ALC 45% BY VOL"
    @"ALC\s*(?<value>[\dAa]{1,2}(\.\d)?)\s*%\s*BY\s*VOL\b"
    + "|" +
    // 4) "12.0 % ALCOHOL BY VOLUME"
    @"(?<value>[\dAa]{1,2}(\.\d)?)\s*%\s*ALCOHOL\s*BY\s*VOLUME\b"
    + "|" +
    // 5) "45% ABV"
    @"(?<value>[\dAa]{1,2}(\.\d)?)\s*%\s*ABV\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Net contents: matches "NET CONTENTS 750 mL" and "12 FL OZ".
    // OCR frequently misreads "mL" as "mi" (l/i confusion) or drops the
    // "L" entirely, leaving bare "m" (confirmed against a real label).
    // The unit alternation below tolerates both; NormalizeOcrUnit()
    // corrects the captured value back to a clean "mL"/"L" form.
    private static Regex NetContentsRegex() => new(
        @"NET\s*CONTENTS\s*(?<value>\d+(\.\d+)?\s*(mL|m[Ii1]|m\b|L|FL\s*OZ))"
        + "|" +
        @"(?<value>\d+(\.\d+)?\s*(mL|m[Ii1]|m\b|L|FL\s*OZ))",
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
