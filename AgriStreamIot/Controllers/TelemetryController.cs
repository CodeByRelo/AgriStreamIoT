
using System.Net;
using System.Net.Http.Json;
using AgriStreamIoT.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStreamIot.Controllers;

[Authorize(Roles = "Admin,Farmer,Agronomist")]
public class TelemetryController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TelemetryController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string fieldId = "FIELD-001",
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AzureFunctions:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            TempData["Error"] = "Azure Functions URL is not configured.";
            return View(new List<TelemetryReading>());
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();

            var requestUrl =
                $"{baseUrl.TrimEnd('/')}/api/telemetry/latest?fieldId={Uri.EscapeDataString(fieldId)}";

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                requestUrl);

            var functionKey =
    _configuration["AzureFunctions:TelemetryFunctionKey"];

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            using var response = await client.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var reading = await response.Content
                    .ReadFromJsonAsync<TelemetryReading>(
                        cancellationToken: cancellationToken);

                return View(
                    reading == null
                        ? new List<TelemetryReading>()
                        : new List<TelemetryReading> { reading });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] =
                    $"No telemetry readings found for field {fieldId}.";
            }
            else
            {
                TempData["Error"] =
                    $"Telemetry Function returned HTTP {(int)response.StatusCode}.";
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            TempData["Error"] =
                "Could not connect to the Azure Telemetry Function.";
        }

        return View(new List<TelemetryReading>());
    }
}
