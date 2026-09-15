namespace Ttb.LabelReview.Api.Services;
using Ttb.LabelReview.Api.Models;

public interface ILabelAnalysisService
{
    Task<LabelAnalysisResult> AnalyzeAsync(
        IFormFile labelImage,
        ApplicationData applicationData,
        CancellationToken ct);
}
