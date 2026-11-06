using Azure.Data.Tables;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues.Models;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var storageConnectionString =
    builder.Configuration["AzureStorageConnectionString"]
    ?? throw new InvalidOperationException(
        "Azure Storage connection string is missing.");

builder.Services.AddSingleton(
    new TableServiceClient(storageConnectionString));

var queueOptions = new QueueClientOptions
{
    MessageEncoding = QueueMessageEncoding.Base64
};

builder.Services.AddSingleton(
    new QueueClient(
        storageConnectionString,
        "irrigation-commands",
        queueOptions));

if (!string.IsNullOrEmpty(
    Environment.GetEnvironmentVariable(
        "APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();