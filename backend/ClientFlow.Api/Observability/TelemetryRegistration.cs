using ClientFlow.Api.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
namespace ClientFlow.Api;

internal static class TelemetryRegistration
{
    public static IServiceCollection RegisterTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelemetrySettings>().Bind(configuration.GetSection(TelemetrySettings.SectionName))
            .Validate(x => !x.Enabled || Uri.TryCreate(x.Endpoint, UriKind.Absolute, out _), "Telemetry endpoint must be an absolute URL when enabled.").ValidateOnStart();
        var settings = configuration.GetSection(TelemetrySettings.SectionName).Get<TelemetrySettings>() ?? new();
        var telemetry = services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService(settings.ServiceName));
        telemetry.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (settings.Enabled)
            {
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(settings.Endpoint));
            }
        });
        telemetry.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (settings.Enabled)
            {
                metrics.AddOtlpExporter(options => options.Endpoint = new Uri(settings.Endpoint));
            }
        });

        return services;
    }
}
