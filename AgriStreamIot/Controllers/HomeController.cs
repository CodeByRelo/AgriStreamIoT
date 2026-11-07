
using AgriStreamIoT.Data;
using AgriStreamIoT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using AgriStreamIot.Models;

namespace AgriStreamIot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var totalDevices = await _context.Devices.CountAsync();

            var onlineDevices = await _context.Devices
                .CountAsync(d => d.OperationalStatus == "Online");

            var offlineDevices = totalDevices - onlineDevices;

            var totalFields = await _context.Devices
                .Select(d => d.FieldId)
                .Distinct()
                .CountAsync();

            var recentDevices = await _context.Devices
                .OrderByDescending(d => d.LastUpdated)
                .Take(5)
                .Select(d => new DeviceSummaryViewModel
                {
                    DeviceId = d.DeviceId,
                    FieldId = d.FieldId,
                    OperationalStatus = d.OperationalStatus,
                    LastUpdated = d.LastUpdated
                })
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalDevices = totalDevices,
                OnlineDevices = onlineDevices,
                OfflineDevices = offlineDevices,
                TotalFields = totalFields,
                RecentDevices = recentDevices
            };

            return View(model);
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                });
        }
    }
}
