namespace AgriStreamIoT.Models;

public class IrrigationCommand
{
    public int DeviceId { get; set; }

    public string FieldId { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
}