namespace Api.Models;

public class D1Payload
{
    public int id { get; set; }
    public int co2 { get; set; }
    public float temperature { get; set; }
    public float humidity { get; set; }
    public string device { get; set; } = String.Empty;
    public DateTime? received_at { get; set; }
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