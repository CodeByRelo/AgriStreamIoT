namespace AgriStreamIoT.Models;

public class TelemetryReading
{
    public string FieldId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public double TemperatureCelsius { get; set; }

    public double SoilMoisture { get; set; }

    public double HumidityPercent { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}