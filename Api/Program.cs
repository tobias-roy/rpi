using Api.Models;
using Api.Services;
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

app.MapGet("/data", async (HttpContext ctx, ILogger<Program> _logger) =>
{
    try
    {
        var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
        await using var conn = new NpgsqlConnection(config.GetConnectionString("Postgres"));
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT * FROM wemos_data ORDER BY received_at DESC LIMIT 50", conn);
        var reader = await cmd.ExecuteReaderAsync();

        var results = new List<object>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                id = reader["id"],
                device = reader["device"],
                co2 = reader["co2"],
                temperature = reader["temperature"],
                humidity = reader["humidity"],
                received_at = reader["received_at"]
            });
        }

        _logger.LogInformation("Successfully retrieved {Count} data records", results.Count);
        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while retrieving telemetry data");
        return Results.Problem("An error occurred while retrieving data");
    }
});

app.Run();