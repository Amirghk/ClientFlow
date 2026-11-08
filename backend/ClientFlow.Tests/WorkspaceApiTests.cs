using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClientFlow.Tests;

public sealed class WorkspaceApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<JsonObject> Json(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");

        return JsonNode.Parse(body)!.AsObject();
    }

    private static string Id(JsonObject row) => row["id"]!.GetValue<string>();
    private static string Version(JsonObject row) => row["version"]!.GetValue<string>();

    private async Task<(HttpClient Client, string Root, JsonObject Organization)> Workspace()
    {
        var client = factory.Client("auth0|" + Guid.NewGuid());
        var organization = await Json(await client.PostAsJsonAsync("/api/organizations", new
        {
            name = "Workspace"
        }));

        return (client, "/api/organizations/" + Id(organization), organization);
    }

    private static Task<HttpResponseMessage> Company(HttpClient client, string root, string name = "North Studio") =>
        client.PostAsJsonAsync(root + "/companies", new
        {
            name
        });

    private static Task<HttpResponseMessage> Deal(HttpClient client, string root, string companyId, string currency = "USD", decimal value = 100, string? contactId = null) =>
        client.PostAsJsonAsync(root + "/opportunities", new
        {
            companyId,
            contactId,
            name = "New website",
            value,
            currency
        });

    [Fact]
    public async Task Client_work_flows_from_contact_to_won_deal_project_tasks_and_reporting()
    {
        var (client, root, _) = await Workspace();
        var profile = await Json(await client.PostAsync("/api/me", null));
        var company = await Json(await Company(client, root));
        var contact = await Json(await client.PostAsJsonAsync(root + "/contacts", new
        {
            companyId = Id(company),
            firstName = "Alex",
            lastName = "Lane",
            email = "alex@example.test"
        }));
        var deal = await Json(await Deal(client, root, Id(company), contactId: Id(contact)));
        deal = await Json(await client.PostAsJsonAsync(root + "/opportunities/" + Id(deal) + "/stage", new
        {
            stage = "Won",
            version = Version(deal)
        }));
        var project = await Json(await client.PostAsJsonAsync(root + "/opportunities/" + Id(deal) + "/convert", new
        {
            version = Version(deal)
        }));
        var repeated = await Json(await client.PostAsJsonAsync(root + "/opportunities/" + Id(deal) + "/convert", new
        {
            version = Version(deal)
        }));
        Assert.Equal(Id(project), Id(repeated));

        var taskPath = root + "/projects/" + Id(project) + "/tasks";
        var task = await Json(await client.PostAsJsonAsync(taskPath, new
        {
            title = "Kickoff",
            description = "Agree scope",
            assignedToUserId = Id(profile),
            status = "ToDo",
            priority = "High",
            dueDate = "2020-01-01"
        }));
        var dashboard = await Json(await client.GetAsync(root + "/dashboard"));
        Assert.Equal(1, dashboard["overdueTasks"]!.GetValue<int>());
        Assert.Equal(100, dashboard["values"]![0]!["wonValue"]!.GetValue<decimal>());

        await Json(await client.PutAsJsonAsync(taskPath + "/" + Id(task), new
        {
            title = "Kickoff",
            assignedToUserId = Id(profile),
            status = "Done",
            priority = "High",
            version = Version(task)
        }));
        var details = await Json(await client.GetAsync(root + "/projects/" + Id(project)));
        Assert.Equal(100, details["progress"]!.GetValue<int>());
        Assert.Equal(1, details["completedTasks"]!.GetValue<int>());
        Assert.NotEmpty(dashboard["activity"]!.AsArray());
    }

    [Fact]
    public async Task Archiving_preserves_history_blocks_new_relationships_and_supports_restore()
    {
        var (client, root, _) = await Workspace();
        var company = await Json(await Company(client, root));
        var deal = await Json(await Deal(client, root, Id(company)));
        var path = root + "/companies/" + Id(company);
        company = await Json(await client.PostAsJsonAsync(path + "/archive", new
        {
            version = Version(company)
        }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Deal(client, root, Id(company))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(root + "/opportunities/" + Id(deal))).StatusCode);
        Assert.Equal(0, (await Json(await client.GetAsync(root + "/companies")))["total"]!.GetValue<int>());
        Assert.Equal(1, (await Json(await client.GetAsync(root + "/companies?archived=true")))["total"]!.GetValue<int>());

        await Json(await client.PostAsJsonAsync(path + "/restore", new
        {
            version = Version(company)
        }));
        Assert.Equal(HttpStatusCode.Created, (await Deal(client, root, Id(company))).StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_reads_and_references_are_rejected()
    {
        var (alice, rootA, _) = await Workspace();
        var (bob, rootB, _) = await Workspace();
        var companyA = await Json(await Company(alice, rootA));
        var companyB = await Json(await Company(bob, rootB));
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync(rootB + "/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Deal(alice, rootA, Id(companyB))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync(rootA + "/companies/" + Id(companyB))).StatusCode);
        var contactB = await Json(await bob.PostAsJsonAsync(rootB + "/contacts", new
        {
            companyId = Id(companyB),
            firstName = "Other",
            lastName = "Tenant"
        }));
        Assert.Equal(HttpStatusCode.NotFound, (await Deal(alice, rootA, Id(companyA), contactId: Id(contactB))).StatusCode);
    }

    [Fact]
    public async Task Contact_must_match_company_and_stale_edits_return_problem_details()
    {
        var (client, root, _) = await Workspace();
        var first = await Json(await Company(client, root, "A"));
        var second = await Json(await Company(client, root, "B"));
        var contact = await Json(await client.PostAsJsonAsync(root + "/contacts", new
        {
            companyId = Id(first),
            firstName = "Alex",
            lastName = "Lane"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Deal(client, root, Id(second), contactId: Id(contact))).StatusCode);
        var path = root + "/companies/" + Id(first);
        await Json(await client.PutAsJsonAsync(path, new
        {
            name = "Updated",
            version = Version(first)
        }));
        var stale = await client.PutAsJsonAsync(path, new
        {
            name = "Lost update",
            version = Version(first)
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("application/problem+json", stale.Content.Headers.ContentType?.MediaType);
        Assert.True(stale.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("Updated", (await Json(await client.GetAsync(path)))["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Reports_keep_currencies_separate_and_exclude_lost_deals()
    {
        var (client, root, _) = await Workspace();
        var company = await Json(await Company(client, root));
        await Json(await Deal(client, root, Id(company), "USD", 125.50m));
        await Json(await Deal(client, root, Id(company), "EUR", 240));
        var lost = await Json(await Deal(client, root, Id(company), "USD", 999));
        await Json(await client.PostAsJsonAsync(root + "/opportunities/" + Id(lost) + "/stage", new
        {
            stage = "Lost",
            version = Version(lost)
        }));
        var report = await Json(await client.GetAsync(root + "/dashboard"));
        Assert.Equal(2, report["activeOpportunities"]!.GetValue<int>());
        var values = report["values"]!.AsArray();
        Assert.Equal(2, values.Count);
        Assert.Equal(125.50m, values.Single(v => v!["currency"]!.GetValue<string>() == "USD")!["openValue"]!.GetValue<decimal>());
        Assert.Equal(240m, values.Single(v => v!["currency"]!.GetValue<string>() == "EUR")!["openValue"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Invitation_is_single_use_members_cannot_administer_and_removal_clears_tasks()
    {
        var (admin, root, _) = await Workspace();
        var invitation = await Json(await admin.PostAsync(root + "/invitations", null));
        var member = factory.Client("auth0|" + Guid.NewGuid());
        var profile = await Json(await member.PostAsync("/api/me", null));
        var token = invitation["token"]!.GetValue<string>();
        await Json(await member.PostAsJsonAsync("/api/invitations/accept", new
        {
            token
        }));
        var third = factory.Client("auth0|" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await third.PostAsJsonAsync("/api/invitations/accept", new
        {
            token
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync(root + "/invitations", null)).StatusCode);
        var listing = await admin.GetStringAsync(root + "/invitations");
        Assert.DoesNotContain(token, listing);
        Assert.DoesNotContain("tokenHash", listing);
        var company = await Json(await Company(member, root));
        var project = await Json(await member.PostAsJsonAsync(root + "/projects", new
        {
            companyId = Id(company),
            name = "Delivery",
            status = "Active"
        }));
        var task = await Json(await member.PostAsJsonAsync(root + "/projects/" + Id(project) + "/tasks", new
        {
            title = "Assigned",
            status = "ToDo",
            priority = "Medium",
            assignedToUserId = Id(profile)
        }));
        var organization = await Json(await admin.GetAsync(root));
        var removed = await admin.PostAsJsonAsync(root + "/members/" + Id(profile) + "/remove", new
        {
            version = Version(organization)
        });
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Null((await Json(await admin.GetAsync(root + "/tasks/" + Id(task))))["assignedToUserId"]);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(root + "/companies")).StatusCode);
    }

    [Fact]
    public async Task Last_administrator_and_revoked_invitations_are_protected()
    {
        var (admin, root, organization) = await Workspace();
        var profile = await Json(await admin.PostAsync("/api/me", null));
        var memberPath = root + "/members/" + Id(profile);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(memberPath + "/role", new
        {
            role = "Member",
            version = Version(organization)
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(memberPath + "/remove", new
        {
            version = Version(organization)
        })).StatusCode);
        var invite = await Json(await admin.PostAsync(root + "/invitations", null));
        var invites = JsonNode.Parse(await admin.GetStringAsync(root + "/invitations"))!.AsArray();
        var stored = invites.Single()!.AsObject();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(root + "/invitations/" + Id(invite) + "/revoke", new
        {
            version = Version(stored)
        })).StatusCode);
        var outsider = factory.Client("auth0|" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await outsider.PostAsJsonAsync("/api/invitations/accept", new
        {
            token = invite["token"]!.GetValue<string>()
        })).StatusCode);
    }

    [Fact]
    public async Task Profiles_and_organization_updates_require_current_versions()
    {
        var (client, root, organization) = await Workspace();
        var profile = await Json(await client.PostAsync("/api/me", null));
        var edited = await Json(await client.PutAsJsonAsync("/api/me", new { displayName = "Alex Lane", version = Version(profile) }));
        Assert.Equal("Alex Lane", edited["displayName"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/me", new { displayName = "Stale", version = Version(profile) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(root, new { name = "Missing version" })).StatusCode);
        await Json(await client.PutAsJsonAsync(root, new { name = "Renamed", version = Version(organization) }));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root, new { name = "Stale", version = Version(organization) })).StatusCode);
        Assert.Contains("Alex Lane", await client.GetStringAsync(root + "/members"));
    }

    [Fact]
    public async Task Project_and_task_archival_preserves_records_and_prevents_new_tasks_on_archived_projects()
    {
        var (client, root, _) = await Workspace();
        var company = await Json(await Company(client, root));
        var project = await Json(await client.PostAsJsonAsync(root + "/projects", new { companyId = Id(company), name = "Standalone", status = "Active" }));
        var projectPath = root + "/projects/" + Id(project);
        Assert.Equal(0, (await Json(await client.GetAsync(projectPath)))["progress"]!.GetValue<int>());
        var task = await Json(await client.PostAsJsonAsync(projectPath + "/tasks", new { title = "Work", status = "Done", priority = "Urgent" }));
        var taskPath = root + "/tasks/" + Id(task);
        task = await Json(await client.PostAsJsonAsync(taskPath + "/archive", new { version = Version(task) }));
        Assert.Equal(0, (await Json(await client.GetAsync(projectPath)))["progress"]!.GetValue<int>());
        project = await Json(await client.PostAsJsonAsync(projectPath + "/archive", new { version = Version(project) }));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(projectPath + "/tasks", new { title = "Invalid", status = "ToDo", priority = "Low" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(taskPath + "/restore", new { version = Version(task) })).StatusCode);
        await Json(await client.PostAsJsonAsync(projectPath + "/restore", new { version = Version(project) }));
        await Json(await client.PostAsJsonAsync(taskPath + "/restore", new { version = Version(task) }));
        Assert.Equal(100, (await Json(await client.GetAsync(projectPath)))["progress"]!.GetValue<int>());
    }

    [Fact]
    public async Task Search_sort_and_pagination_return_a_consistent_page_contract()
    {
        var (client, root, _) = await Workspace();
        await Json(await Company(client, root, "Alpha"));
        await Json(await Company(client, root, "Beta"));
        var page = await Json(await client.GetAsync(root + "/companies?pageSize=1&sort=nameDesc"));
        Assert.Equal(2, page["total"]!.GetValue<int>());
        Assert.Equal(1, page["page"]!.GetValue<int>());
        Assert.Equal(1, page["pageSize"]!.GetValue<int>());
        Assert.Equal("Beta", page["items"]![0]!["name"]!.GetValue<string>());
        var search = await Json(await client.GetAsync(root + "/companies?search=ALP"));
        Assert.Equal(1, search["total"]!.GetValue<int>());
        Assert.Equal("Alpha", search["items"]![0]!["name"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    [InlineData("sort=unsafe")]
    public async Task Lists_validate_bounds(string query)
    {
        var (client, root, _) = await Workspace();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + "/companies?" + query)).StatusCode);
    }
}
