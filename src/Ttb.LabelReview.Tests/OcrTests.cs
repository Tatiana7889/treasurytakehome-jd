using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Ttb.LabelReview.Api.Services;
using Xunit;

namespace Ttb.LabelReview.Tests
{
    public class OcrTests
    {
        [Fact]
        public void OcrService_CanBeConstructed_WithDefaultConfiguration()
        {
            var logger = NullLogger<OcrService>.Instance;

            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns("./");

            var service = new OcrService(logger, env.Object);

            service.Should().NotBeNull();
        }
    }
}
