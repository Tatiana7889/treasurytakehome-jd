using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace Ttb.LabelReview.Api.Services;

public interface IImagePreprocessingService
{
    Task<Pix> PrepareForOcrAsync(Stream imageStream, CancellationToken ct = default);
}

public class ImagePreprocessingService : IImagePreprocessingService
{
    private readonly ILogger<ImagePreprocessingService> _logger;

    public ImagePreprocessingService(ILogger<ImagePreprocessingService> logger)
    {
        _logger = logger;
    }

    public async Task<Pix> PrepareForOcrAsync(Stream imageStream, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(imageStream, ct);

        // Upscale small images for better OCR accuracy
        const int minDimension = 1500;
        if (image.Width < minDimension && image.Height < minDimension)
        {
            var scale = (double)minDimension / Math.Max(image.Width, image.Height);
            image.Mutate(x => x.Resize((int)(image.Width * scale), (int)(image.Height * scale)));
        }

        // 09/15/2026 TT: Preprocessing pipeline for imperfect photos
        //
        // NOTE: A fixed global BinaryThreshold used to run here. On labels with
        // uneven/textured backgrounds (e.g. mottled dark label art), a single
        // global cutoff turns bright background texture into foreground noise
        // and drops dim letter strokes into the background, badly degrading OCR.
        // Tesseract performs its own adaptive (Otsu-based) binarization on
        // grayscale input, so we leave binarization to it and only do mild
        // contrast/sharpening here.
        image.Mutate(x => x
            .Grayscale()
            .Contrast(1.3f)
            .GaussianSharpen(0.5f)
        );

        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms, ct);
        ms.Position = 0;

        _logger.LogDebug("Preprocessed image to {Width}x{Height} for OCR", image.Width, image.Height);

        return Pix.LoadFromMemory(ms.ToArray());
    }
}
