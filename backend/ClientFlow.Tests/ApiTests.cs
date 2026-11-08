using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClientFlow.Application.Organizations;
using ClientFlow.Infrastructure;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ClientFlow.Tests;

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_is_public(string path)
    {
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/me")]
    [InlineData("GET", "/api/organizations")]
    [InlineData("POST", "/api/organizations")]
    [InlineData("GET", "/api/organizations/11111111-1111-1111-1111-111111111111")]
    [InlineData("PUT", "/api/organizations/11111111-1111-1111-1111-111111111111")]
    public async Task All_business_routes_require_authentication(string method, string path)
    {
        using var client = factory.Client();
        var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { name = "Test" }) };

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("expired")]
    [InlineData("signature")]
    [InlineData("malformed")]
    [InlineData("unsigned")]
    public async Task Invalid_tokens_are_rejected(string kind)
    {
        var token = kind switch
        {
            "audience" => factory.Token("auth0|alice", audience: "wrong"),
            "issuer" => factory.Token("auth0|alice", issuer: "https://wrong.test/"),
            "expired" => factory.Token("auth0|alice", expired: true),
            "signature" => factory.Token("auth0|alice", wrongKey: true),
            "unsigned" => "eyJhbGciOiJub25lIn0.eyJzdWIiOiJhdXRoMHxhbGljZSJ9.",
            _ => "not-a-token"
        };
        using var client = factory.Client(token: token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Machine_tokens_and_missing_subject_are_forbidden()
    {
        foreach (var token in new[] { factory.Token("app@clients"), factory.Token("machine", machine: true), factory.Token("", missingSubject: true) })
        {
            using var client = factory.Client(token: token);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/me")).StatusCode);
        }
    }

    [Fact]
    public async Task User_can_create_list_select_and_rename_their_organizations()
    {
        var subject = "auth0|" + Guid.NewGuid();
        using var client = factory.Client(subject);
        var before = await client.GetFromJsonAsync<JsonElement>("/api/me");

        Assert.Equal(JsonValueKind.Null, before.GetProperty("userId").ValueKind);

        var created = await client.PostAsJsonAsync("/api/organizations", new
        {
            name = "  My agency  "
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var org = (await created.Content.ReadFromJsonAsync<OrganizationResponse>())!;

        Assert.Equal("My agency", org.Name);
        Assert.Equal("Admin", org.Role);

        Assert.Equal($"/api/organizations/{org.Id}", created.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);

        var updated = await client.PutAsJsonAsync(created.Headers.Location, new
        {
            name = "New name",
            version = org.Version
        });

        Assert.Equal("New name", (await updated.Content.ReadFromJsonAsync<OrganizationResponse>())!.Name);
        await client.PostAsJsonAsync("/api/organizations", new
        {
            name = "Second agency"
        });
        var list = await client.GetFromJsonAsync<JsonElement>("/api/organizations?pageSize=1");

        Assert.Equal(2, list.GetProperty("total").GetInt32());
        Assert.Equal(1, list.GetProperty("items").GetArrayLength());

        var after = await client.GetFromJsonAsync<JsonElement>("/api/me");

        Assert.NotEqual(Guid.Empty, after.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task Outsider_cannot_read_update_or_list_another_organization()
    {
        using var owner = factory.Client("auth0|" + Guid.NewGuid());
        using var outsider = factory.Client("auth0|" + Guid.NewGuid());
        var result = await owner.PostAsJsonAsync("/api/organizations", new
        {
            name = "Private agency"
        });
        var path = result.Headers.Location!;

        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync(path, new
        {
            name = "Attack"
        })).StatusCode);
        var list = await outsider.GetFromJsonAsync<JsonElement>("/api/organizations");

        Assert.Equal(0, list.GetProperty("total").GetInt32());
        Assert.Equal("Private agency", (await owner.GetFromJsonAsync<OrganizationResponse>(path))!.Name);
    }

    [Fact]
    public async Task Member_can_read_but_cannot_rename()
    {
        var subject = "auth0|member-" + Guid.NewGuid();
        Guid orgId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
            var owner = new AppUser("auth0|owner-" + Guid.NewGuid());
            var member = new AppUser(subject);
            var organization = new Organization("Member access", owner.Id);
            organization.AddMember(member.Id, owner.Id);
            orgId = organization.Id;
            db.Users.AddRange(owner, member);
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
        }
        using var client = factory.Client(subject);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/organizations/{orgId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/organizations/{orgId}", new
        {
            name = "No"
        })).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_organization_names_return_validation_problem(string? name)
    {
        using var client = factory.Client("auth0|validation");
        var response = await client.PostAsJsonAsync("/api/organizations", new
        {
            name
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Long_names_and_malformed_json_are_rejected()
    {
        using var client = factory.Client("auth0|validation");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/organizations", new
        {
            name = new string('x', 161)
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/organizations", new StringContent("{", Encoding.UTF8, "application/json"))).StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647")]
    public async Task Invalid_pagination_is_rejected(string query)
    {
        using var client = factory.Client("auth0|validation");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/organizations?" + query)).StatusCode);
    }

    [Fact]
    public async Task Development_openapi_describes_controller_routes()
    {
        using var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();

        var specification = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = specification.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/me", out _));
        Assert.True(paths.TryGetProperty("/api/organizations", out var organizations));
        Assert.True(organizations.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty("/api/organizations/{organizationId}", out _));
    }

    [Fact]
    public async Task Invalid_creation_does_not_provision_a_user()
    {
        using var client = factory.Client("auth0|invalid-" + Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/organizations", new
        {
            name = " "
        });
        var identity = await client.GetFromJsonAsync<JsonElement>("/api/me");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, identity.GetProperty("userId").ValueKind);
    }

    [Fact]
    public async Task Invalid_rename_does_not_change_the_organization()
    {
        using var client = factory.Client("auth0|rename-" + Guid.NewGuid());

        var created = await client.PostAsJsonAsync("/api/organizations", new
        {
            name = "Original"
        });
        var path = created.Headers.Location!;

        var response = await client.PutAsJsonAsync(path, new
        {
            name = " "
        });
        var organization = await client.GetFromJsonAsync<OrganizationResponse>(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Original", organization!.Name);
    }

    [Fact]
    public void PostgreSql_model_has_no_pending_migration_changes()
    {
        using var db = new ClientFlowDbContext(new DbContextOptionsBuilder<ClientFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=not-used").Options);

        Assert.False(db.Database.HasPendingModelChanges());
    }
}


