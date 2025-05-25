using Microsoft.Extensions.Options;
using System.Text.Json;
using MQTTnet;
using Api.Models;
using Npgsql;

namespace Api.Services;

public class MqttBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<MqttBackgroundService> _logger;
    private readonly IMqttClientService _mqttClientService;
     private readonly IConfiguration _config;
    private readonly IOptionsMonitor<MqttBrokerOptions> _mqttBrokerOptions;

    public MqttBackgroundService(
        ILogger<MqttBackgroundService> logger,
        IMqttClientService mqttClientService,
        IOptionsMonitor<MqttBrokerOptions> mqttBrokerOptions,
        IConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mqttClientService = mqttClientService ?? throw new ArgumentNullException(nameof(mqttClientService));
        _mqttBrokerOptions = mqttBrokerOptions ?? throw new ArgumentNullException(nameof(mqttBrokerOptions));
        _mqttClientService.OnMessageReceivedAsync += OnMessageReceivedAsync;
        _config = config;
    }

    private async Task OnMessageReceivedAsync(string topic, string payload)
    {
        _logger.LogInformation("Message received on topic {Topic}: {Payload}", topic, payload);

        string[] telemetryTopics = _mqttBrokerOptions.CurrentValue.TelemetryTopics ?? [];

        if (!telemetryTopics.Any(t => MqttTopicFilterComparer.Compare(topic, t) == MqttTopicFilterCompareResult.IsMatch))
        {
            _logger.LogInformation("Unhandled topic: {Topic} with payload: {Payload}", topic, payload);
            return;
        }

        _logger.LogInformation("Telemetry topic matched: {Topic}", topic);

        // Handle topic-specific logic
        switch (true)
        {
            case true when MqttTopicFilterComparer.Compare(topic, "/home/birdie/") == MqttTopicFilterCompareResult.IsMatch:
                await HandleBirdiePayloadAsync(payload);
                break;

            case true when MqttTopicFilterComparer.Compare(topic, "/shellies/+/adc/0/") == MqttTopicFilterCompareResult.IsMatch:
                _logger.LogInformation("Matched Shelly ADC topic: {Topic}", topic);
                // TODO: await HandleShellyAdcPayloadAsync(payload);
                break;

            case true when MqttTopicFilterComparer.Compare(topic, "/shellies/+/ext_temperatures/") == MqttTopicFilterCompareResult.IsMatch:
                _logger.LogInformation("Matched Shelly external temperature topic: {Topic}", topic);
                // TODO: await HandleShellyTemperaturePayloadAsync(payload);
                break;

            default:
                _logger.LogWarning("Topic matched general filter but has no specific handler: {Topic}", topic);
                break;
        }
    }

    private async Task HandleBirdiePayloadAsync(string payload)
    {
        _logger.LogInformation("Handling /home/birdie/ payload...");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            D1Payload? d1Payload = JsonSerializer.Deserialize<D1Payload>(payload, options);
            _logger.LogInformation("Deserialized payload: {@d1Payload}", d1Payload);
            if (d1Payload is null || string.IsNullOrWhiteSpace(d1Payload.Device))
            {
                _logger.LogWarning("Deserialized Birdie payload is null or missing 'device'");
                return;
            }

            await using var conn = new NpgsqlConnection(_config.GetConnectionString("Postgres"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
            INSERT INTO wemos_data (device, co2, temperature, humidity, received_at) 
            VALUES (@device, @co2, @temp, @hum, @received_at)", conn);

            cmd.Parameters.AddWithValue("device", d1Payload.Device);
            cmd.Parameters.AddWithValue("co2", d1Payload.Co2);
            cmd.Parameters.AddWithValue("temp", d1Payload.Temperature);
            cmd.Parameters.AddWithValue("hum", d1Payload.Humidity);
            cmd.Parameters.AddWithValue("received_at", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Successfully added Birdie telemetry to DB: {Device}", d1Payload.Device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle /home/birdie/ payload.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _mqttClientService.ConnectAsync(stoppingToken);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        // TODO release managed resources here
        await _mqttClientService.DisconnectAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}