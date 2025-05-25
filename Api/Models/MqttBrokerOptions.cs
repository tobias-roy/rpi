namespace Api.Models;

public class MqttBrokerOptions
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool? UseTls { get; set; }
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string[]? TelemetryTopics { get; set; }
}