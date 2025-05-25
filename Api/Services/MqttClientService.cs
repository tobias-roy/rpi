using Api.Models;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace Api.Services;

public interface IMqttClientService
{
    event MqttClientService.MessageReceivedHandlerAsync OnMessageReceivedAsync;
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken);
}

public sealed class MqttClientService : IMqttClientService, IDisposable
{
    // Fields
    private readonly MqttClientFactory _mqttFactory;
    private readonly IMqttClient _client;
    private readonly ILogger<MqttClientService> _logger;
    private bool _disposedValue;
    private readonly IOptionsMonitor<MqttBrokerOptions> _mqttBrokerOptions;

    // Properties

    public delegate Task MessageReceivedHandlerAsync(string topic, string payload);

    public event MessageReceivedHandlerAsync? OnMessageReceivedAsync;

    public MqttClientService(ILogger<MqttClientService> logger, IOptionsMonitor<MqttBrokerOptions> mqttBrokerOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mqttBrokerOptions = mqttBrokerOptions ?? throw new ArgumentNullException(nameof(mqttBrokerOptions));

        _mqttFactory = new();
        _client = _mqttFactory.CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += MqttClientOnApplicationMessageReceivedAsync;

        // async void, could potentially lead to unhandled exceptions
        _mqttBrokerOptions.OnChange(async void (_) =>
        {
            _logger.LogInformation("MQTT Broker options changed.");
            if (_client.IsConnected)
            {
                _logger.LogInformation("Client is connected. Disconnecting...");
                await DisconnectAsync(CancellationToken.None);
            }

            _logger.LogInformation("Waiting for 5 seconds before reconnecting...");
            await Task.Delay(5000);

            await ConnectAsync(CancellationToken.None);
        });
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
        {
            _logger.LogInformation("Client is already connected.");
            return;
        }
        try
        {
            MqttClientOptions? mqttClientOptions = _mqttFactory.CreateClientOptionsBuilder()
                .WithTcpServer(_mqttBrokerOptions.CurrentValue.Host, _mqttBrokerOptions.CurrentValue.Port)
                .WithCredentials(_mqttBrokerOptions.CurrentValue.Username, _mqttBrokerOptions.CurrentValue.Password)
                .WithClientId(_mqttBrokerOptions.CurrentValue.ClientId)
                .WithTlsOptions(new MqttClientTlsOptions()
                {
                    UseTls = _mqttBrokerOptions.CurrentValue.UseTls ?? false,
                })
                .Build();

            await _client.ConnectAsync(mqttClientOptions, cancellationToken);
            _logger.LogInformation("Connected to MQTT broker.");
            await SubscribeAsync(cancellationToken);
            _logger.LogInformation("Subscribed to topics.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("error connecting");
            _logger.LogError(ex, "Failed to connect to MQTT broker.");
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected == false)
        {
            _logger.LogInformation("Client is already disconnected.");
            return;
        }
        try
        {
            await _client.DisconnectAsync(
                _mqttFactory.CreateClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build(),
                cancellationToken
            );

            _logger.LogInformation("Disconnected from MQTT broker.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from MQTT broker.");
        }
    }

    public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (_client.IsConnected == false)
        {
            _logger.LogInformation("Client is not connected. Cannot publish.");
            return;
        }
        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await _client.PublishAsync(message, cancellationToken);
            _logger.LogInformation("Published message to topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message.");
        }
    }

    private async Task SubscribeAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected == false)
        {
            _logger.LogInformation("Client is not connected. Cannot subscribe.");
            return;
        }
        
        try
        {
            MqttClientSubscribeOptionsBuilder subscribeOptionsBuilder = _mqttFactory.CreateSubscribeOptionsBuilder();

            // Subscribe to all telemetry topics
            foreach (string topic in _mqttBrokerOptions.CurrentValue.TelemetryTopics ?? [])
            {
                subscribeOptionsBuilder = subscribeOptionsBuilder.WithTopicFilter(topic);
            }

            MqttClientSubscribeOptions subscribeOptions = subscribeOptionsBuilder.Build();

            _ = await _client.SubscribeAsync(subscribeOptions, cancellationToken);
            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", subscribeOptions.TopicFilters.Select(e => e.Topic)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic.");
        }
    }

    private async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        string topic = arg.ApplicationMessage.Topic;
        string payload = arg.ApplicationMessage.ConvertPayloadToString();
        _logger.LogDebug("Received Topic: {Topic}", topic);
        _logger.LogDebug("Received message: {Message}", payload);

        if (OnMessageReceivedAsync is not null)
        {
            await OnMessageReceivedAsync.Invoke(topic, payload);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _client.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}