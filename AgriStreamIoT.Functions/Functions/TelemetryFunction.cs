using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AgriStreamIoT.Functions.Functions;

public class TelemetryFunction
{
    private readonly TableServiceClient _tableServiceClient;

    public TelemetryFunction(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    [Function("GetLatestTelemetry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            Microsoft.Azure.Functions.Worker.AuthorizationLevel.Function,
            "get",
            Route = "telemetry/latest")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var fieldId = req.Query["fieldId"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return new BadRequestObjectResult(
                new { message = "The fieldId query parameter is required." });
        }

        var table = _tableServiceClient.GetTableClient("Telemetry");

        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {fieldId}");

        await foreach (var entity in table.QueryAsync<TableEntity>(
            filter: filter,
            maxPerPage: 1,
            cancellationToken: cancellationToken))
        {
            var recordedAt = entity.TryGetValue(
                "RecordedAtUtc", out var dateValue)
                && dateValue is DateTimeOffset date
                    ? date
                    : DateTimeOffset.MinValue;

            return new OkObjectResult(new
            {
                fieldId = entity.PartitionKey,
                deviceId = entity.GetString("DeviceId"),
                temperatureCelsius = GetDouble(entity, "TemperatureCelsius"),
                soilMoisture = GetDouble(entity, "SoilMoisture"),
                humidityPercent = GetDouble(entity, "HumidityPercent"),
                recordedAtUtc = recordedAt
            });
        }

        return new NotFoundObjectResult(
            new { message = $"No telemetry readings found for field {fieldId}." });
    }

    private static double GetDouble(
        TableEntity entity,
        string propertyName)
    {
        if (!entity.TryGetValue(propertyName, out var value) || value == null)
            return 0;

        return Convert.ToDouble(value);
    }
}