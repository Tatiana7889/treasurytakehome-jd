using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ttb.LabelReview.Api.Models;
using Ttb.LabelReview.Api.Services;
using Xunit;

namespace Ttb.LabelReview.Tests
{
    public class ComparisonTests
    {
        private static ComparisonService BuildSut(double threshold = 0.85)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LabelReview:FuzzyMatchThreshold"] = threshold.ToString()
                })
                .Build();

            return new ComparisonService(config, NullLogger<ComparisonService>.Instance);
        }

        private static ApplicationData BuildApplication() => new()
        {
            BrandName = "Bold Whiskey",
            ClassType = "Straight Bourbon Whiskey",
            AlcoholContent = "45% ALC/VOL",
            NetContents = "750 mL",
            BottlerOrProducerName = "Acme Distillery",
            BottlerOrProducerAddress = "123 Main St, Louisville, KY"
        };

        [Fact]
        public void Compare_ReportsMatch_WhenAlcoholContentIsIdentical()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var extracted = new ExtractedFields { AlcoholContent = "45% ALC/VOL" };

            var results = sut.Compare(application, "irrelevant raw text", extracted);
            var alcoholResult = results.Single(r => r.FieldName == "Alcohol Content");

            alcoholResult.Status.Should().Be(MatchStatus.Match);
        }

        [Fact]
        public void Compare_ReportsMismatch_WhenAlcoholContentDiffers()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var extracted = new ExtractedFields { AlcoholContent = "40% ALC/VOL" };

            var results = sut.Compare(application, "irrelevant raw text", extracted);
            var alcoholResult = results.Single(r => r.FieldName == "Alcohol Content");

            alcoholResult.Status.Should().Be(MatchStatus.Mismatch);
        }

        [Fact]
        public void Compare_ReportsNotFound_WhenAlcoholContentMissingFromLabel()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var extracted = new ExtractedFields { AlcoholContent = null };

            var results = sut.Compare(application, "irrelevant raw text", extracted);
            var alcoholResult = results.Single(r => r.FieldName == "Alcohol Content");

            alcoholResult.Status.Should().Be(MatchStatus.NotFound);
        }

        [Fact]
        public void Compare_FindsBrandName_ViaFuzzyTextSearch()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var rawText = "BOLD WHISKEY STRAIGHT BOURBON WHISKEY 45% ALC/VOL 750 mL";
            var extracted = new ExtractedFields { AlcoholContent = "45% ALC/VOL", NetContents = "750 mL" };

            var results = sut.Compare(application, rawText, extracted);
            var brandResult = results.Single(r => r.FieldName == "Brand Name");

            brandResult.Status.Should().Be(MatchStatus.Match);
        }

        [Fact]
        public void Compare_FlagsMissingGovernmentWarning()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var extracted = new ExtractedFields { GovernmentWarningPresent = false };

            var results = sut.Compare(application, "BOLD WHISKEY 45% ALC/VOL", extracted);
            var warningResult = results.Single(r => r.FieldName == "Government Warning");

            warningResult.Status.Should().Be(MatchStatus.NotFound);
        }

        [Fact]
        public void Compare_MatchesGovernmentWarning_WhenStatutoryTextPresent()
        {
            var sut = BuildSut();
            var application = BuildApplication();
            var extracted = new ExtractedFields
            {
                GovernmentWarningPresent = true,
                GovernmentWarningText = application.GovernmentWarningText
            };

            var results = sut.Compare(application, "irrelevant raw text", extracted);
            var warningResult = results.Single(r => r.FieldName == "Government Warning");

            warningResult.Status.Should().Be(MatchStatus.Match);
        }
    }
}
