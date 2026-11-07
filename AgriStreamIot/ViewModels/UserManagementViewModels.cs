
using System.ComponentModel.DataAnnotations;

namespace AgriStreamIoT.ViewModels;

public class CreateUserViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Farmer";
}

public class EditUserViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Farmer";
}

public class ResetUserPasswordViewModel
{
    public int UserId { get; set; }

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserListViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
