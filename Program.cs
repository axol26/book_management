// using book_management.Models;
using book_management.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Load .env file before configuration setup
var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
DotNetEnv.Env.Load(dotenv);

// Add configuration sources
builder.Configuration
    .SetBasePath(root)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(prefix: "")  // Remove prefix requirement
    .Build();

// Construct connection string directly from environment variables
var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                      $"User Id={Environment.GetEnvironmentVariable("DB_USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                      "TrustServerCertificate=True;Encrypt=True";

// Override the connection string in configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Debug final connection string (mask password)
var debugConnectionString = connectionString.Replace(Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "", "***");

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<LoginDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSession(); // Add this line to enable session
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
