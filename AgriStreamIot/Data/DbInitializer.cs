
using AgriStreamIoT.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriStreamIoT.Data;

public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        // Apply any pending database migrations.
        await _context.Database.MigrateAsync();

        const string adminRole = "Admin";

        var adminUsername =
            _configuration["InitialAdmin:Username"]?.Trim();

        var adminPassword =
            _configuration["InitialAdmin:Password"];

        // Check whether the initial Admin already exists.
        if (!string.IsNullOrWhiteSpace(adminUsername))
        {
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == adminUsername &&
                    u.Role == adminRole);

            if (existingAdmin != null)
            {
                _logger.LogInformation(
                    "Initial Admin account already exists.");

                return;
            }
        }

        // Credentials are required only when creating the account.
        if (string.IsNullOrWhiteSpace(adminUsername) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Initial Admin credentials are missing. " +
                "Configure InitialAdmin:Username and " +
                "InitialAdmin:Password.");
        }

        if (adminPassword.Length < 12)
        {
            throw new InvalidOperationException(
                "The initial Admin password must contain at least 12 characters.");
        }

        // Create the initial Admin account.
        var admin = new User
        {
            Username = adminUsername,
            Role = adminRole
        };

        admin.PasswordHash = _passwordHasher.HashPassword(
            admin,
            adminPassword);

        _context.Users.Add(admin);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Initial Admin account created successfully.");
    }
}
