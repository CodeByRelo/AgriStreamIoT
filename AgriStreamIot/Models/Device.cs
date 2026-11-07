using System.ComponentModel.DataAnnotations;

namespace AgriStreamIoT.Models;

public class Device
{
    [Key]
    public int DeviceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OperationalStatus { get; set; } = "Offline";

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}