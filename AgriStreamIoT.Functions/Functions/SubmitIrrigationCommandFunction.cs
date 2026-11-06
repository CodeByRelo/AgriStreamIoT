
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using AgriStreamIoT.Functions.Models;

namespace AgriStreamIoT.Functions.Functions;

public class SubmitIrrigationCommandFunction
{
    private readonly QueueClient _queueClient;

    public SubmitIrrigationCommandFunction(QueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    [Function("SubmitIrrigationCommand")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            Microsoft.Azure.Functions.Worker.AuthorizationLevel.Function,
            "post",
            Route = "irrigation/command")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        IrrigationCommandMessage? command;

        try
        {
            command = await req.ReadFromJsonAsync<IrrigationCommandMessage>(
                cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return new BadRequestObjectResult(
                new { message = "Invalid JSON request body." });
        }

        if (command == null ||
            command.DeviceId <= 0 ||
            string.IsNullOrWhiteSpace(command.FieldId) ||
            string.IsNullOrWhiteSpace(command.RequestedBy) ||
            string.IsNullOrWhiteSpace(command.Command))
        {
            return new BadRequestObjectResult(
                new { message = "Invalid irrigation command details." });
        }

        command.FieldId = command.FieldId.Trim();
        command.RequestedBy = command.RequestedBy.Trim();
        command.Command = command.Command.Trim();

        if (command.Command is not ("Start" or "Stop"))
        {
            return new BadRequestObjectResult(
                new { message = "Command must be Start or Stop." });
        }

        command.RequestedAtUtc = DateTime.UtcNow;

        await _queueClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var message = System.Text.Json.JsonSerializer.Serialize(command);

        await _queueClient.SendMessageAsync(
            message,
            cancellationToken: cancellationToken);

        return new ObjectResult(new
        {
            message = "Irrigation command successfully queued.",
            deviceId = command.DeviceId,
            fieldId = command.FieldId,
            command = command.Command,
            requestedAtUtc = command.RequestedAtUtc
        })
        {
            StatusCode = StatusCodes.Status202Accepted
        };
    }
}
