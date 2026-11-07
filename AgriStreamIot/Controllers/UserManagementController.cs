
using AgriStreamIoT.Data;
using AgriStreamIoT.Models;
using AgriStreamIoT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgriStreamIot.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private static readonly string[] AllowedRoles =
    {
        "Admin",
        "Farmer",
        "Agronomist"
    };

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserManagementController(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserListViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                Role = u.Role
            })
            .ToListAsync();

        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model.Username = model.Username.Trim();

        if (!AllowedRoles.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");

        if (!IsStrongPassword(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password must contain uppercase, lowercase, a number and a special character.");
        }

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == model.Username);

        if (usernameExists)
            ModelState.AddModelError(
                nameof(model.Username),
                "This username is already taken.");

        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            Username = model.Username,
            Role = model.Role
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "User account created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        return View(new EditUserViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        model.Username = model.Username.Trim();

        if (!AllowedRoles.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");

        var user = await _context.Users.FindAsync(model.UserId);

        if (user == null)
            return NotFound();

        var usernameExists = await _context.Users.AnyAsync(
            u => u.Username == model.Username &&
                 u.UserId != model.UserId);

        if (usernameExists)
            ModelState.AddModelError(
                nameof(model.Username),
                "This username is already taken.");

        var currentUsername = User.Identity?.Name;

        if (user.Username == currentUsername &&
            user.Role != model.Role)
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "You cannot change your own role.");
        }

        if (user.Role == "Admin" && model.Role != "Admin")
        {
            var adminCount = await _context.Users
                .CountAsync(u => u.Role == "Admin");

            if (adminCount <= 1)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "The system must have at least one Admin.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        user.Username = model.Username;
        user.Role = model.Role;

        await _context.SaveChangesAsync();

        TempData["Success"] = "User account updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetUserPasswordViewModel model)
    {
        if (!IsStrongPassword(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password must contain uppercase, lowercase, a number and a special character.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid password. Check the password requirements.";
            return RedirectToAction(nameof(Edit), new { id = model.UserId });
        }

        var user = await _context.Users.FindAsync(model.UserId);

        if (user == null)
            return NotFound();

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            model.Password);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Password reset successfully.";

        return RedirectToAction(nameof(Edit), new { id = user.UserId });
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 12
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(c => !char.IsLetterOrDigit(c));
    }
}
