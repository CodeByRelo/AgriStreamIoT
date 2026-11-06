using System.Text.Json;
using AgriStreamIoT.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStreamIoT.Functions.Functions;

public class ProcessIrrigationCommandFunction
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessIrrigationCommandFunction> _logger;

    public ProcessIrrigationCommandFunction(
        IConfiguration configuration,
        ILogger<ProcessIrrigationCommandFunction> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger(
            "irrigation-commands",
            Connection = "AzureWebJobsStorage")] string message,
        CancellationToken cancellationToken)
    {
        var command = JsonSerializer.Deserialize<IrrigationCommandMessage>(
            message);

        if (command == null ||
            command.DeviceId <= 0 ||
            string.IsNullOrWhiteSpace(command.FieldId))
        {
            throw new InvalidOperationException(
                "Invalid irrigation queue message.");
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration["DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Azure SQL connection string is missing.");

        var operationalStatus = command.Command switch
        {
            "Start" => "Irrigating",
            "Stop" => "Idle",
            _ => throw new InvalidOperationException(
                "Unsupported irrigation command.")
        };

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE Devices
            SET OperationalStatus = @status,
                LastUpdated = SYSUTCDATETIME()
            WHERE DeviceId = @deviceId
              AND FieldId = @fieldId;
            """;

        await using var sqlCommand = new SqlCommand(sql, connection);

        sqlCommand.Parameters.AddWithValue(
            "@status", operationalStatus);

        sqlCommand.Parameters.AddWithValue(
            "@deviceId", command.DeviceId);

        sqlCommand.Parameters.AddWithValue(
            "@fieldId", command.FieldId);

        var rowsAffected = await sqlCommand.ExecuteNonQueryAsync(
            cancellationToken);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Device {command.DeviceId} was not found for field {command.FieldId}.");
        }

        _logger.LogInformation(
            "Irrigation command {Command} processed for device {DeviceId}. Status: {Status}",
            command.Command,
            command.DeviceId,
            operationalStatus);
    }
}