using System.ComponentModel.DataAnnotations;

namespace AgriStreamIoT.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "Farmer";
}