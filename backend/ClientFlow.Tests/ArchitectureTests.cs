using System.Reflection;
using ClientFlow.Application.Organizations;
using ClientFlow.Domain.Organizations;
using ClientFlow.Common.Constants;

namespace ClientFlow.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Inner_layers_do_not_reference_outer_layers()
    {
        AssertNoReferences(typeof(Organization).Assembly,
            "ClientFlow.Application", "ClientFlow.Infrastructure", "ClientFlow.Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
        AssertNoReferences(typeof(OrganizationService).Assembly,
            "ClientFlow.Infrastructure", "ClientFlow.Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
        AssertNoReferences(typeof(DatabaseIndexNames).Assembly,
            "ClientFlow.Domain", "ClientFlow.Application", "ClientFlow.Infrastructure", "ClientFlow.Api");
    }

    private static void AssertNoReferences(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var references = assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => forbiddenPrefixes.Any(prefix =>
            reference.Name!.StartsWith(prefix, StringComparison.Ordinal)));
    }
}
