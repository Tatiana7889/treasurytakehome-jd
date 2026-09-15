using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ttb.LabelReview.Api.Services;
using Xunit;

namespace Ttb.LabelReview.Tests
{
    public class ExtractionTests
    {
        private readonly FieldExtractionService _sut = new(NullLogger<FieldExtractionService>.Instance);

        [Theory]
        [InlineData("BOLD WHISKEY 13.5% ALC/VOL 750 mL", "13.5% ALC/VOL")]
        [InlineData("ALC 40% BY VOL", "40% ALC/VOL")]
        [InlineData("12.0 % ALCOHOL BY VOLUME", "12.0% ALC/VOL")]
        public void Extract_FindsAlcoholContent_InVariousFormats(string ocrText, string expected)
        {
            var result = _sut.Extract(ocrText);

            result.AlcoholContent.Should().Be(expected);
        }

        [Fact]
        public void Extract_ReturnsNullAlcoholContent_WhenNotPresent()
        {
            var result = _sut.Extract("BOLD WHISKEY BOTTLED BY ACME DISTILLERY");

            result.AlcoholContent.Should().BeNull();
        }

        [Theory]
        [InlineData("NET CONTENTS 750 mL", "750 mL")]
        [InlineData("12 FL OZ", "12 FL OZ")]
        public void Extract_FindsNetContents(string ocrText, string expected)
        {
            var result = _sut.Extract(ocrText);

            result.NetContents.Should().Be(expected);
        }

        [Fact]
        public void Extract_DetectsGovernmentWarningHeader()
        {
            var text = "GOVERNMENT WARNING: (1) ACCORDING TO THE SURGEON GENERAL...";

            var result = _sut.Extract(text);

            result.GovernmentWarningPresent.Should().BeTrue();
        }

        [Fact]
        public void Extract_DoesNotFlagGovernmentWarning_WhenAbsent()
        {
            var result = _sut.Extract("BOLD WHISKEY 40% ALC/VOL 750 mL");

            result.GovernmentWarningPresent.Should().BeFalse();
        }

        [Fact]
        public void Extract_FindsBottlerLine()
        {
            var result = _sut.Extract("PRODUCED AND BOTTLED BY ACME DISTILLERY, LOUISVILLE, KY");

            result.BottlerOrProducer.Should().Contain("ACME DISTILLERY");
        }

        [Fact]
        public void Extract_FindsCountryOfOrigin()
        {
            var result = _sut.Extract("PRODUCT OF SCOTLAND");

            result.CountryOfOrigin.Should().Contain("SCOTLAND");
        }
    }
}