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
builder.Services.AddSingleton<IAlertNotifier, SignalRAlertNotifier>();
builder.Services.AddSingleton<IAlertNotifier, ConsoleAlertNotifier>();
builder.Services.AddSingleton<AlertEvaluatorHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<AlertEvaluatorHostedService>());
builder.Services.AddHostedService<SystemMetricsHostedService>();
builder.Services.AddSignalR();
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
        b => b.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated(); // Or Migrate() if using migrations

    if (!context.Users.Any())
    {
        string defaultPassword = "admin"; // Should be changed on first login or via env var
        context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            IsAdmin = true
        });
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();
app.MapHub<MetricsHub>("/metricshub");

app.Run();
