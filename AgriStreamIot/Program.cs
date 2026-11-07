using AgriStreamIoT.Data;
using AgriStreamIoT.Models;
using AgriStreamIoT.Services.AzureStorage;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Azure SQL Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Password hashing
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Database initializer
builder.Services.AddScoped<DbInitializer>();

// Azure Storage connection
var storageConnectionString =
    builder.Configuration["AzureStorage:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Azure Storage connection string is missing.");

// Azure Blob Storage
builder.Services.AddSingleton(
    new BlobServiceClient(storageConnectionString));

// Azure Table Storage
builder.Services.AddSingleton(
    new TableServiceClient(storageConnectionString));

// Azure Storage services
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddScoped<ProcessingAuditService>();

// HTTP client for Azure Functions
builder.Services.AddHttpClient();

// Cookie Authentication
builder.Services
    .AddAuthentication("AgriStreamCookie")
    .AddCookie("AgriStreamCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "AgriStream.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Authorization
builder.Services.AddAuthorization();

// Build application
var app = builder.Build();

// Initialize database and seed initial Admin
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<DbInitializer>();

    await initializer.InitializeAsync();
}

// HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();