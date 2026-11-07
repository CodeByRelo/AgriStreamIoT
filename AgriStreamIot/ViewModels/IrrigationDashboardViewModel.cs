
using AgriStreamIoT.Models;

namespace AgriStreamIoT.ViewModels;

public class IrrigationDashboardViewModel
{
    public List<Device> Devices { get; set; } = new();

    public int DeviceId { get; set; }

    public string Command { get; set; } = "Start";
}
