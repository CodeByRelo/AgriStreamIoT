using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AgriStreamIoT.Services.AzureStorage;

public class BlobStorageService
{
    private const string ContainerName = "drone-images";

    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        var container = _blobServiceClient.GetBlobContainerClient(ContainerName);

        await container.CreateIfNotExistsAsync(
            PublicAccessType.None);

        return container;
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("A valid file is required.");

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";

        var container = await GetContainerAsync();
        var blobClient = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            },
            cancellationToken);

        return blobName;
    }

    public async Task<List<string>> GetImagesAsync()
    {
        var container = await GetContainerAsync();
        var images = new List<string>();

        await foreach (var blob in container.GetBlobsAsync())
        {
            images.Add(blob.Name);
        }

        return images;
    }

    public async Task DeleteAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync();
        var blobClient = container.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(
            cancellationToken: cancellationToken);
    }
}