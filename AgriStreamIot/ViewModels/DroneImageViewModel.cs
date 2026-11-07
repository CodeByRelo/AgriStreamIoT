namespace AgriStreamIoT.ViewModels;

public class DroneImageViewModel
{
    public string BlobName { get; set; } = string.Empty;

    public string ProcessingStatus { get; set; } = "Awaiting processing";

    public string Message { get; set; } = "Waiting for Azure Function.";

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public long? FileSizeBytes { get; set; }
}