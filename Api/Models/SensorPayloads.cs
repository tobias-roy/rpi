namespace Api.Models;

public class D1Payload
{
    public int Co2 { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public string Device { get; set; } = String.Empty;
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