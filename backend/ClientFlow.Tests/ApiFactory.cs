using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using ClientFlow.Infrastructure;
using ClientFlow.Api.Configuration;
using ClientFlow.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace ClientFlow.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://clientflow-test.auth0.com/";
    public const string Audience = "https://clientflow.test/api";
    private readonly RSA rsa = RSA.Create(2048);
    private readonly SqliteConnection sqlite = new("Data Source=:memory:");
    private readonly string? postgres = Environment.GetEnvironmentVariable("CLIENTFLOW_TEST_POSTGRES");
    private string? testConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting is visible while the minimal host is being constructed.
        builder.UseSetting($"{Auth0Settings.SectionName}:Domain", "clientflow-test.auth0.com");
        builder.UseSetting($"{Auth0Settings.SectionName}:Audience", Audience);
        builder.UseSetting($"{DatabaseSettings.SectionName}:ClientFlow", "Host=localhost;Database=unused");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ClientFlowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ClientFlowDbContext>>();

            services.AddDbContext<ClientFlowDbContext>(options =>
            {
                if (postgres is null)
                {
                    options.UseSqlite(sqlite);
                }
                else
                {
                    options.UseNpgsql(testConnection);
                }
            });

            // Exercise the real JWT bearer handler with an offline test issuer/key.
            // No bypass handler or unsigned token support exists in the application.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                config.SigningKeys.Add(new RsaSecurityKey(rsa) { KeyId = "test-key" });
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
            });
        });
    }

    public async Task InitializeAsync()
    {
        if (postgres is null)
        {
            await sqlite.OpenAsync();
        }
        else
        {
            var connection = new NpgsqlConnectionStringBuilder(postgres)
            {
                Database = "clientflow_tests_" + Guid.NewGuid().ToString("N")
            };
            testConnection = connection.ConnectionString;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientFlowDbContext>();

        if (postgres is null)
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }
    }

    public HttpClient Client(string? subject = null, string? token = null)
    {
        var client = CreateClient();

        if (subject is not null || token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Token(subject!));
        }

        return client;
    }

    public string Token(string subject, string? audience = null, string? issuer = null, bool expired = false,
        bool wrongKey = false, bool machine = false, bool missingSubject = false)
    {
        using var otherKey = wrongKey ? RSA.Create(2048) : null;
        var claims = new List<Claim>();

        if (!missingSubject)
        {
            claims.Add(new Claim("sub", subject));
        }

        if (machine)
        {
            claims.Add(new Claim("gty", "client-credentials"));
        }

        var jwt = new JwtSecurityToken(issuer ?? Issuer, audience ?? Audience, claims,
            DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(new RsaSecurityKey(otherKey ?? rsa) { KeyId = "test-key" }, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (postgres is not null)
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ClientFlowDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
        await sqlite.DisposeAsync();
        rsa.Dispose();
    }
}

