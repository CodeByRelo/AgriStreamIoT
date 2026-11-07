using AgriStreamIoT.Services.AzureStorage;
using AgriStreamIoT.ViewModels;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStreamIot.Controllers;

[Authorize]
public class DroneImagesController : Controller
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly BlobStorageService _blobStorageService;
    private readonly ProcessingAuditService _processingAuditService;

    public DroneImagesController(
        BlobStorageService blobStorageService,
        ProcessingAuditService processingAuditService)
    {
        _blobStorageService = blobStorageService;
        _processingAuditService = processingAuditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var images = await _blobStorageService.GetImagesAsync();

        var auditEntries =
            await _processingAuditService.GetRecentEntriesAsync(
                100,
                cancellationToken);

        var auditLookup = auditEntries
            .GroupBy(e => e.GetString("BlobName") ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.First());

        var model = images.Select(image =>
        {
            auditLookup.TryGetValue(image, out var audit);

            return new DroneImageViewModel
            {
                BlobName = image,
                ProcessingStatus =
                    audit?.GetString("Status") ?? "Awaiting processing",

                Message =
                    audit?.GetString("Message")
                    ?? "Waiting for Azure Function to process this image.",

                ProcessedAtUtc =
                    audit?.GetDateTimeOffset("ProcessedAtUtc"),

                FileSizeBytes =
                    audit?.GetInt64("FileSizeBytes")
            };
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxFileSize)
        {
            TempData["Error"] = "The image cannot exceed 10 MB.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedTypes = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        if (!allowedTypes.TryGetValue(
                extension, out var expectedContentType) ||
            !string.Equals(
                file.ContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "Only JPG, PNG and WEBP images are allowed.";

            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();

        var header = new byte[12];

        var bytesRead = await stream.ReadAsync(
            header,
            cancellationToken);

        var validImage = extension switch
        {
            ".jpg" or ".jpeg" =>
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,

            ".png" =>
                bytesRead >= 8 &&
                header.Take(8).SequenceEqual(
                    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),

            ".webp" =>
                bytesRead >= 12 &&
                System.Text.Encoding.ASCII.GetString(header, 0, 4)
                    == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(header, 8, 4)
                    == "WEBP",

            _ => false
        };

        if (!validImage)
        {
            TempData["Error"] =
                "The uploaded file is not a valid supported image.";

            return RedirectToAction(nameof(Index));
        }

        stream.Position = 0;

        try
        {
            var blobName = await _blobStorageService.UploadAsync(
                file,
                cancellationToken);

            TempData["Success"] =
                $"Image uploaded successfully: {blobName}. Azure processing has been triggered.";
        }
        catch (Exception ex)
        {
            // Log the actual exception in production.
            TempData["Error"] =
                "The image could not be uploaded. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}