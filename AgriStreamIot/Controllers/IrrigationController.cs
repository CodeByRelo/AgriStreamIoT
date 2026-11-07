
using System.Net.Http.Json;
using AgriStreamIoT.Data;
using AgriStreamIoT.Models;
using AgriStreamIoT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStreamIoT.Controllers;

[Authorize]
public class IrrigationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IrrigationController> _logger;

    public IrrigationController(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<IrrigationController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new IrrigationDashboardViewModel
        {
            Devices = await _context.Devices
                .AsNoTracking()
                .OrderBy(d => d.FieldId)
                .ThenBy(d => d.DeviceId)
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCommand(
        IrrigationDashboardViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.DeviceId <= 0 ||
            model.Command is not ("Start" or "Stop"))
        {
            TempData["Error"] = "Please select a valid device and command.";
            return RedirectToAction(nameof(Index));
        }

        var device = await _context.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.DeviceId == model.DeviceId,
                cancellationToken);

        if (device == null)
        {
            TempData["Error"] = "The selected device could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var baseUrl = _configuration["AzureFunctions:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            TempData["Error"] = "Azure Functions URL is not configured.";
            return RedirectToAction(nameof(Index));
        }

        var payload = new IrrigationCommand
        {
            DeviceId = device.DeviceId,
            FieldId = device.FieldId,
            Command = model.Command,
            RequestedBy = User.Identity?.Name ?? "Unknown",
            RequestedAtUtc = DateTime.UtcNow
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/api/irrigation/command")
            {
                Content = JsonContent.Create(payload)
            };

            var functionKey =
    _configuration["AzureFunctions:IrrigationFunctionKey"];

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            using var response = await client.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    $"{model.Command} irrigation command accepted for {device.FieldId}.";
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "Irrigation Function returned HTTP {StatusCode}: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                TempData["Error"] =
                    $"The Irrigation Function returned HTTP {(int)response.StatusCode}.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the Irrigation Function.");

            TempData["Error"] =
                "Could not connect to the Azure Irrigation Function.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Irrigation Function request timed out.");

            TempData["Error"] =
                "The irrigation request timed out.";
        }

        return RedirectToAction(nameof(Index));
    }
}
