namespace ClientFlow.Api.Configuration;

public sealed class TelemetrySettings
{
    public const string SectionName = "Telemetry";
    public string ServiceName { get; set; } = "ClientFlow.Api";
    public bool Enabled
    {
        get; set;
    }
    public string Endpoint { get; set; } = "";
}
