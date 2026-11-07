using Azure.Data.Tables;

namespace AgriStreamIoT.Services.AzureStorage;

public class ProcessingAuditService
{
    private const string TableName = "ProcessingAudit";
    private const string PartitionKey = "BLOB_PROCESSING";

    private readonly TableServiceClient _tableServiceClient;

    public ProcessingAuditService(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    public async Task<List<TableEntity>> GetRecentEntriesAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        var table = _tableServiceClient.GetTableClient(TableName);

        var entries = new List<TableEntity>();

        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {PartitionKey}");

        await foreach (var entity in table.QueryAsync<TableEntity>(
            filter: filter,
            maxPerPage: count,
            cancellationToken: cancellationToken))
        {
            entries.Add(entity);

            if (entries.Count >= count)
                break;
        }

        return entries;
    }
}