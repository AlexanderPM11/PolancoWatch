using PolancoWatch.API.Services;
using PolancoWatch.API.Hubs;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Infrastructure.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PolancoWatch.Infrastructure.Data;
using PolancoWatch.Domain.Entities;
using Docker.DotNet;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Custom Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IMetricsCollector, SystemMetricsCollector>();
builder.Services.AddSingleton<IMetricsBroadcaster, SignalRMetricsBroadcaster>();
builder.Services.AddHttpClient<TelegramAlertNotifier>();
builder.Services.AddSingleton<IAlertNotifier, SignalRAlertNotifier>();
builder.Services.AddSingleton<IAlertNotifier, ConsoleAlertNotifier>();
builder.Services.AddSingleton<IAlertNotifier, TelegramAlertNotifier>();
builder.Services.AddSingleton<IAlertNotifier, EmailAlertNotifier>();
builder.Services.AddSingleton<AlertEvaluatorHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<AlertEvaluatorHostedService>());
builder.Services.AddHostedService<SystemMetricsHostedService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();
builder.Services.AddSignalR();

// Configure Docker Client (Singleton)
builder.Services.AddSingleton<IDockerClient>(sp => {
    var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
        ? "npipe://./pipe/docker_engine" 
        : "unix:///var/run/docker.sock";
    return new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
});
// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_change_me_in_production_so_its_secure_enough_for_sha256";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // For local dev
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

// Allow CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.WithOrigins("https://polancowatch.apolanco.com", "http://polancowatch.apolanco.com", "http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Ensure Data Directory exists for SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("Data Source="))
{
    var dbPath = connectionString.Replace("Data Source=", "");
    var directory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate(); // Ensures migrations are applied

    if (!context.Users.Any())
    {
        string defaultPassword = "admin"; // Should be changed on first login or via env var
        context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            Email = "admin@example.com", // Default email for reset
            IsAdmin = true
        });
        
        // Seed default Alert Rules
        if (!context.AlertRules.Any())
        {
            context.AlertRules.AddRange(new List<AlertRule>
            {
                new AlertRule { MetricType = MetricType.Cpu, Threshold = 80, IsActive = true },
                new AlertRule { MetricType = MetricType.Memory, Threshold = 85, IsActive = true },
                new AlertRule { MetricType = MetricType.Disk, Threshold = 90, IsActive = true }
            });
        }
        
        
        // Seed default Notification Settings
        if (!context.NotificationSettings.Any())
        {
            context.NotificationSettings.Add(new NotificationSettings());
        }
        
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();
app.MapHub<MetricsHub>("/metricshub");
app.MapHub<LogsHub>("/logshub");

app.Run();
