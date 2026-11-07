
namespace AgriStreamIoT.ViewModels;

public class DashboardViewModel
{
    public int TotalDevices { get; set; }

    public int OnlineDevices { get; set; }

    public int OfflineDevices { get; set; }

    public int TotalFields { get; set; }

    public List<DeviceSummaryViewModel> RecentDevices { get; set; } = new();
}

public class DeviceSummaryViewModel
{
    public int DeviceId { get; set; }

    public string FieldId { get; set; } = string.Empty;

    public string OperationalStatus { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }
}
