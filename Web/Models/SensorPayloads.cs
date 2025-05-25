using System.Text.Json.Serialization;

namespace Web.Models;

public class D1Payload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("co2")]
    public int Co2 { get; set; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public float Humidity { get; set; }

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("received_at")]
    public DateTime Received_At { get; set; }
}

public class ShellyVoltage
{
    public float Voltage { get; set; }
    public string DeviceId { get; set; } = "";
}

public class ShellyTelemetry
{
    public float Celcius { get; set; }
    public string DeviceId { get; set; } = "";
}