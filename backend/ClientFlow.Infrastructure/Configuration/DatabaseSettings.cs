namespace ClientFlow.Infrastructure.Configuration;

public sealed class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";
    public string ClientFlow { get; set; } = string.Empty;
}
