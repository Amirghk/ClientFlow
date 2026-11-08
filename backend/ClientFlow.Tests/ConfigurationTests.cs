using ClientFlow.Api;
using ClientFlow.Api.Configuration;
using ClientFlow.Infrastructure;
using ClientFlow.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClientFlow.Tests;

public sealed class ConfigurationTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("tenant.auth0.com", "")]
    [InlineData("https://tenant.auth0.com/", "audience")]
    public void Missing_or_malformed_Auth0_settings_fail_closed(string domain, string audience)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{Auth0Settings.SectionName}:Domain"] = domain,
            [$"{Auth0Settings.SectionName}:Audience"] = audience
        }).Build();
        using var provider = new ServiceCollection().RegisterAPI(config).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<Auth0Settings>>().Value);
    }

    [Fact]
    public void Missing_database_connection_fails_validation()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var provider = new ServiceCollection().RegisterInfrastructure(configuration).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DatabaseSettings>>().Value);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com/path")]
    public void Invalid_cors_origins_fail_validation(string origin)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{CorsSettings.SectionName}:AllowedOrigins:0"] = origin
        }).Build();
        using var provider = new ServiceCollection().RegisterAPI(configuration).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CorsSettings>>().Value);
    }
}
