
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStreamIoT.Functions.Functions;

public class DroneImageUploadFunction
{
    private const string TableName = "ProcessingAudit";
    private const string PartitionKey = "BLOB_PROCESSING";

    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<DroneImageUploadFunction> _logger;

    public DroneImageUploadFunction(
        TableServiceClient tableServiceClient,
        ILogger<DroneImageUploadFunction> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    [Function("DroneImageUpload")]
    public async Task Run(
        [BlobTrigger(
            "drone-images/{name}",
            Connection = "AzureWebJobsStorage")] Stream blob,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            var table = _tableServiceClient.GetTableClient(TableName);

            await table.CreateIfNotExistsAsync(
                cancellationToken);

            var processedAt = DateTimeOffset.UtcNow;

            var rowKey =
                (DateTime.MaxValue.Ticks -
                 processedAt.UtcDateTime.Ticks)
                .ToString("D19")
                + "_"
                + Guid.NewGuid().ToString("N");

            var entity = new TableEntity(PartitionKey, rowKey)
            {
                ["BlobName"] = name,
                ["Status"] = "Processed",
                ["Message"] = "Drone image uploaded and detected successfully.",
                ["ProcessedAtUtc"] = processedAt,
                ["FileSizeBytes"] = blob.CanSeek ? blob.Length : -1
            };

            await table.AddEntityAsync(
                entity,
                cancellationToken);

            _logger.LogInformation(
                "Drone image {BlobName} processed successfully. Size: {FileSize} bytes.",
                name,
                entity["FileSizeBytes"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process drone image {BlobName}.",
                name);

            throw;
        }
    }
}
