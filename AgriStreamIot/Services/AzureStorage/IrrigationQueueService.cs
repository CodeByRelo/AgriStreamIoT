using System.Text.Json;
using Azure.Storage.Queues;
using AgriStreamIoT.Models;

namespace AgriStreamIoT.Services.AzureStorage;

public class IrrigationQueueService
{
    private const string QueueName = "irrigation-commands";

    private readonly QueueServiceClient _queueServiceClient;

    public IrrigationQueueService(QueueServiceClient queueServiceClient)
    {
        _queueServiceClient = queueServiceClient;
    }

    private async Task<QueueClient> GetQueueAsync()
    {
        var queue = _queueServiceClient.GetQueueClient(QueueName);

        await queue.CreateIfNotExistsAsync();

        return queue;
    }

    public async Task SendCommandAsync(
        IrrigationCommand command,
        CancellationToken cancellationToken = default)
    {
        var queue = await GetQueueAsync();

        var message = JsonSerializer.Serialize(command);

        await queue.SendMessageAsync(
            message,
            cancellationToken: cancellationToken);
    }
}