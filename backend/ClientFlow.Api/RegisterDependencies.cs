using System.Threading.RateLimiting;
using ClientFlow.Api.Auth;
using ClientFlow.Api.Configuration;
using ClientFlow.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClientFlow.Api;

public static class RegisterDependencies
{
    public static IServiceCollection RegisterAPI(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        services.AddExceptionHandler<ClientFlow.Api.Errors.ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        RegisterSettings(services, configuration);
        services.RegisterTelemetry(configuration);
        RegisterAuthentication(services);
        RegisterCors(services);
        RegisterRateLimiting(services);

        return services;
    }

    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Auth0Settings>()
            .Bind(configuration.GetSection(Auth0Settings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Domain)
                && Uri.CheckHostName(settings.Domain.Trim()) == UriHostNameType.Dns,
                "Set Auth0:Domain to your Auth0 hostname (without https:// or a path).")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience),
                "Set Auth0:Audience to your Auth0 API Identifier.")
            .ValidateOnStart();

        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .Validate(settings => settings.AllowedOrigins is not null && settings.AllowedOrigins.All(IsOrigin),
                "Cors:AllowedOrigins must contain HTTP(S) origins without paths or wildcards.")
            .ValidateOnStart();

        services.AddOptions<ApiRateLimitSettings>()
            .Bind(configuration.GetSection(ApiRateLimitSettings.SectionName))
            .Validate(settings => settings.PermitLimit > 0 && settings.WindowSeconds > 0,
                "RateLimiting permit limit and window must be positive.")
            .ValidateOnStart();
    }

    private static bool IsOrigin(string origin)
    {

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
            && string.IsNullOrEmpty(uri.UserInfo)
            && origin == uri.GetLeftPart(UriPartial.Authority);
    }

    private static void RegisterAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<Auth0Settings>>((options, configured) =>
            {
                var settings = configured.Value;
                options.Authority = $"https://{settings.Domain.Trim()}/";
                options.Audience = settings.Audience.Trim();
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    NameClaimType = "sub",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder().SetFallbackPolicy(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireAssertion(context =>
            {
                var subject = context.User.FindFirst("sub")?.Value;

                return !string.IsNullOrWhiteSpace(subject) && subject.Length <= 255
                    && !subject.EndsWith("@clients", StringComparison.Ordinal)
                    && context.User.FindFirst("gty")?.Value != "client-credentials";
            }).Build());
    }

    private static void RegisterCors(IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<CorsSettings>>((options, configured) =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = configured.Value.AllowedOrigins;

                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });
    }

    private static void RegisterRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<ApiRateLimitSettings>>((options, configured) =>
        {
            var settings = configured.Value;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ApiRateLimitSettings.PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    QueueLimit = 0
                }));
        });
    }
}
