namespace ClientFlow.Api.Configuration;

public sealed class ApiRateLimitSettings
{
    public const string SectionName = "RateLimiting";
    public const string PolicyName = "api";
    public int PermitLimit { get; set; } = 120;
    public int WindowSeconds { get; set; } = 60;
}
