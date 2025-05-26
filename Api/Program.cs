using Api;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

builder.Services.Configure<MqttBrokerOptions>(builder.Configuration.GetSection("MqttBroker"));
builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
builder.Services.AddHostedService<MqttBackgroundService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:5000", "http://192.168.0.194:5000") // Add your frontend URLs here
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "MQTT Api Online");

app.MapGet("/wemos/historical", async (
    DateTime? from,
    DateTime? to,
    AppDbContext db,
    ILogger<Program> logger
) =>
{
    try
    {
        from ??= DateTime.MinValue;
        to ??= DateTime.UtcNow;

        var data = await db.wemos_data
            .Where(x => x.received_at > from && x.received_at < to)
            .OrderByDescending(x => x.received_at)
            .ToListAsync();

        return Results.Ok(data);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving historical data");
        return Results.Problem("Failed to get historical data.");
    }
});

app.MapGet("/wemos", async (AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var results = await db.wemos_data
            .OrderByDescending(w => w.received_at)
            .ToListAsync();

        logger.LogInformation("Successfully retrieved {Count} data records", results.Count);
        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while retrieving telemetry data");
        return Results.Problem("An error occurred while retrieving data");
    }
});


app.Run();