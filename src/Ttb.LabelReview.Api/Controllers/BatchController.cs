using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Ttb.LabelReview.Api.Models;
using Ttb.LabelReview.Api.Services;

namespace Ttb.LabelReview.Api.Controllers;

public class BatchManifestRow
{
    public string FileName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ClassType { get; set; } = string.Empty;
    public string AlcoholContent { get; set; } = string.Empty;
    public string NetContents { get; set; } = string.Empty;
    public string BottlerOrProducerName { get; set; } = string.Empty;
    public string BottlerOrProducerAddress { get; set; } = string.Empty;
    public string? CountryOfOrigin { get; set; }
    public string BeverageType { get; set; } = "DistilledSpirits";
}

[ApiController]
[Route("api/[controller]")]
public class BatchController : ControllerBase
{
    private readonly ILabelAnalysisService _labelAnalysis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BatchController> _logger;

    public BatchController(
        ILabelAnalysisService labelAnalysis,
        IConfiguration configuration,
        ILogger<BatchController> logger)
    {
        _labelAnalysis = labelAnalysis;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("process")]
    [RequestSizeLimit(200_000_000)]
    [ProducesResponseType(typeof(BatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchResult>> ProcessBatch(
        IFormFile labelsZip, IFormFile manifestCsv, CancellationToken ct)
    {
        if (labelsZip is null || labelsZip.Length == 0)
            return BadRequest("A ZIP file of label images is required.");

        if (manifestCsv is null || manifestCsv.Length == 0)
            return BadRequest("A CSV manifest mapping file names to application data is required.");

        List<BatchManifestRow> manifestRows;
        try
        {
            manifestRows = ReadManifest(manifestCsv);
            _logger.LogInformation($"labelsZip: {labelsZip?.FileName}, manifestCsv: {manifestCsv?.FileName}");

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse batch manifest CSV");
            return BadRequest($"Could not parse manifest CSV: {ex.Message}");
        }

        var maxItems = _configuration.GetValue<int>("LabelReview:BatchMaxItems", 100);
        if (manifestRows.Count > maxItems)
        {
            return BadRequest(
                $"Batch contains {manifestRows.Count} rows, which exceeds the configured maximum of {maxItems}.");
        }

        var batchResult = new BatchResult { TotalLabels = manifestRows.Count };

        using var zipStream = labelsZip!.OpenReadStream();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var row in manifestRows)
        {
            ct.ThrowIfCancellationRequested();

            var entry = archive.GetEntry(row.FileName);
            if (entry is null)
            {
                batchResult.SkippedFiles.Add(row.FileName);
                _logger.LogWarning("Manifest referenced {FileName} which was not found in the ZIP", row.FileName);
                continue;
            }

            var applicationData = MapToApplicationData(row);
            var formFile = await ExtractEntryAsFormFileAsync(entry, ct);

            var labelResult = await _labelAnalysis.AnalyzeAsync(formFile, applicationData, ct);
            batchResult.Results.Add(labelResult);

            switch (labelResult.Outcome)
            {
                case ReviewOutcome.Pass: batchResult.PassCount++; break;
                case ReviewOutcome.Fail: batchResult.FailCount++; break;
                default: batchResult.NeedsReviewCount++; break;
            }
        }

        return Ok(batchResult);
    }

    private static List<BatchManifestRow> ReadManifest(IFormFile manifestCsv)
    {
        using var reader = new StreamReader(manifestCsv.OpenReadStream());
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        return csv.GetRecords<BatchManifestRow>().ToList();
    }

    private static ApplicationData MapToApplicationData(BatchManifestRow row) => new()
    {
        BrandName = row.BrandName,
        ClassType = row.ClassType,
        AlcoholContent = row.AlcoholContent,
        NetContents = row.NetContents,
        BottlerOrProducerName = row.BottlerOrProducerName,
        BottlerOrProducerAddress = row.BottlerOrProducerAddress,
        CountryOfOrigin = row.CountryOfOrigin,
        BeverageType = Enum.TryParse<BeverageType>(row.BeverageType, true, out var bt)
            ? bt
            : BeverageType.DistilledSpirits
    };

    private static async Task<IFormFile> ExtractEntryAsFormFileAsync(ZipArchiveEntry entry, CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using (var entryStream = entry.Open())
        {
            await entryStream.CopyToAsync(memoryStream, ct);
        }
        memoryStream.Position = 0;

        return new FormFile(memoryStream, 0, memoryStream.Length, "labelImage", entry.Name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }
}
