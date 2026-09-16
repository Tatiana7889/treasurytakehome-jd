using System.Text;
using Ttb.LabelReview.Api.Models;

namespace Ttb.LabelReview.Api.Services;

public interface IComparisonService
{
    List<FieldComparisonResult> Compare(ApplicationData application, string rawOcrText, ExtractedFields extracted);
}

public class ComparisonService : IComparisonService
{
    private readonly double _fuzzyThreshold;
    private readonly ILogger<ComparisonService> _logger;

    public ComparisonService(IConfiguration configuration, ILogger<ComparisonService> logger)
    {
        _fuzzyThreshold = double.TryParse(configuration["LabelReview:FuzzyMatchThreshold"], out var t) ? t : 0.85;
        _logger = logger;
    }

    public List<FieldComparisonResult> Compare(ApplicationData application, string rawOcrText, ExtractedFields extracted)
    {
        var results = new List<FieldComparisonResult>
        {
            CompareFreeText("Brand Name", application.BrandName, rawOcrText),
            CompareFreeText("Class/Type", application.ClassType, rawOcrText),
            CompareExact("Alcohol Content", NormalizePercent(application.AlcoholContent), NormalizePercent(extracted.AlcoholContent)),
            CompareExact("Net Contents", NormalizeVolume(application.NetContents), NormalizeVolume(extracted.NetContents)),
            CompareGovernmentWarning(application.GovernmentWarningText, extracted),
            CompareFreeText("Bottler/Producer", application.BottlerOrProducerName, extracted.BottlerOrProducer ?? rawOcrText)
        };

        if (!string.IsNullOrWhiteSpace(application.CountryOfOrigin))
        {
            results.Add(CompareFreeText("Country of Origin", application.CountryOfOrigin, extracted.CountryOfOrigin ?? rawOcrText, isRequired: true));
        }

        return results;
    }

    private FieldComparisonResult CompareExact(string fieldName, string? expected, string? found)
    {
        if (string.IsNullOrWhiteSpace(found))
        {
            return new FieldComparisonResult
            {
                FieldName = fieldName,
                ExpectedValue = expected ?? string.Empty,
                Status = MatchStatus.NotFound,
                Reasoning = $"{fieldName} could not be located on the label. Verify manually."
            };
        }

        var isMatch = string.Equals(expected?.Trim(), found.Trim(), StringComparison.OrdinalIgnoreCase);
        return new FieldComparisonResult
        {
            FieldName = fieldName,
            ExpectedValue = expected ?? string.Empty,
            FoundValue = found,
            Status = isMatch ? MatchStatus.Match : MatchStatus.Mismatch,
            SimilarityScore = isMatch ? 1.0 : 0.0,
            Reasoning = isMatch
                ? $"{fieldName} on the label matches the application."
                : $"{fieldName} on the label (\"{found}\") does not match the application (\"{expected}\")."
        };
    }

    private FieldComparisonResult CompareFreeText(string fieldName, string expected, string haystack, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return new FieldComparisonResult
            {
                FieldName = fieldName,
                Status = MatchStatus.NotFound,
                IsRequiredField = isRequired,
                Reasoning = $"No {fieldName} was provided on the application to compare against."
            };
        }

        var score = FuzzyContains(haystack, expected);
        var status = score >= _fuzzyThreshold
            ? MatchStatus.Match
            : score >= _fuzzyThreshold * 0.6
                ? MatchStatus.PartialMatch
                : MatchStatus.NotFound;

        return new FieldComparisonResult
        {
            FieldName = fieldName,
            ExpectedValue = expected,
            FoundValue = status == MatchStatus.NotFound ? null : expected,
            Status = status,
            SimilarityScore = score,
            IsRequiredField = isRequired,
            Reasoning = status switch
            {
                MatchStatus.Match => $"{fieldName} \"{expected}\" was found on the label.",
                MatchStatus.PartialMatch => $"{fieldName} \"{expected}\" was partially matched on the label (similarity {score:P0}). Recommend manual confirmation.",
                _ => $"{fieldName} \"{expected}\" was not found on the label."
            }
        };
    }

    private FieldComparisonResult CompareGovernmentWarning(string expectedWarning, ExtractedFields extracted)
    {
        if (!extracted.GovernmentWarningPresent || string.IsNullOrWhiteSpace(extracted.GovernmentWarningText))
        {
            return new FieldComparisonResult
            {
                FieldName = "Government Warning",
                ExpectedValue = expectedWarning,
                Status = MatchStatus.NotFound,
                Reasoning = "The mandatory Government Warning statement was not detected on the label. " +
                            "This is a required disclosure under 27 CFR 16.21."
            };
        }

        var score = FuzzyContains(extracted.GovernmentWarningText, expectedWarning);
        var status = score >= 0.9 ? MatchStatus.Match : score >= 0.6 ? MatchStatus.PartialMatch : MatchStatus.Mismatch;

        return new FieldComparisonResult
        {
            FieldName = "Government Warning",
            ExpectedValue = expectedWarning,
            FoundValue = extracted.GovernmentWarningText,
            Status = status,
            SimilarityScore = score,
            Reasoning = status switch
            {
                MatchStatus.Match => "Government Warning statement text matches the required statutory language.",
                MatchStatus.PartialMatch => "Government Warning statement was found but wording deviates slightly from the statutory text. Recommend manual review.",
                _ => "Government Warning statement was found but does not match the required statutory language."
            }
        };
    }

    /// <summary>
    /// Case-insensitive substring/token-overlap heuristic. This is a
    /// prototype-grade stand-in for a proper edit-distance or embedding
    /// similarity measure — adequate for demoing the workflow, not
    /// production-grade fuzzy matching.
    ///
    /// Token matching is tolerant of OCR noise: a needle token doesn't have
    /// to appear verbatim in the haystack. It's scored against the best
    /// Levenshtein-similarity match among haystack tokens, so a misread like
    /// "8OLD" still earns partial credit toward "BOLD" instead of being
    /// treated as a total miss just because it isn't a byte-for-byte match.
    /// </summary>
    /// <summary>
    /// Normalizes text before fuzzy comparison: uppercases, trims, and
    /// strips apostrophes/quote marks entirely rather than trying to match
    /// them exactly. OCR is specifically unreliable on small punctuation —
    /// a straight apostrophe (') vs. a typographic one (') are different
    /// Unicode characters that won't string-match, and OCR frequently
    /// drops apostrophes altogether or misreads them as stray characters.
    /// "STONE'S THROW" and "STONES THROW" should be treated as the same
    /// brand name for compliance-matching purposes; the apostrophe isn't
    /// content that changes what's being verified.
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        var upper = text.ToUpperInvariant().Trim();
        var sb = new StringBuilder(upper.Length);
        foreach (var c in upper)
        {
            // Strip straight ('), curly ('/'), and backtick-style apostrophes/quotes.
            if (c is '\'' or '\u2018' or '\u2019' or '`')
                continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static double FuzzyContains(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle)) return 0;

        var normalizedHaystack = NormalizeForComparison(haystack);
        var normalizedNeedle = NormalizeForComparison(needle);

        if (normalizedHaystack.Contains(normalizedNeedle)) return 1.0;

        var needleTokens = normalizedNeedle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (needleTokens.Length == 0) return 0;

        var haystackTokens = normalizedHaystack.Split(
            new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (haystackTokens.Length == 0) return 0;

        // Per needle token: exact-substring hit scores 1.0; otherwise fall
        // back to the best Levenshtein similarity against any haystack token,
        // so single-character OCR misreads don't zero out the whole token.
        var totalScore = 0.0;
        foreach (var needleToken in needleTokens)
        {
            if (haystackTokens.Any(t => t.Contains(needleToken)))
            {
                totalScore += 1.0;
                continue;
            }

            var bestSimilarity = haystackTokens.Max(t => TokenSimilarity(needleToken, t));
            totalScore += bestSimilarity;
        }

        return totalScore / needleTokens.Length;
    }

    /// <summary>
    /// Normalized Levenshtein similarity in [0, 1]; 1.0 = identical strings.
    /// Only worth crediting when the tokens are reasonably close in length —
    /// otherwise very short haystack tokens ("A", "OF") trivially "match"
    /// anything with a small edit distance and inflate scores.
    /// </summary>
    private static double TokenSimilarity(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        if (Math.Min(a.Length, b.Length) < Math.Max(a.Length, b.Length) * 0.5) return 0;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }

    private static string? NormalizePercent(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(" ", "").ToUpperInvariant();

    private static string? NormalizeVolume(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(" ", "").ToUpperInvariant();
}
