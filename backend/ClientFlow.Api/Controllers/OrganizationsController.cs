using ClientFlow.Api.Configuration;
using ClientFlow.Application.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class OrganizationsController(OrganizationService organizations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrganizationPage>> List(
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await organizations.ListAsync(page, pageSize, cancellationToken);

        return result.Failure is { } failure ? MapFailure(failure) : Ok(result.Value);
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationResponse>> Get(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await organizations.GetAsync(organizationId, cancellationToken);

        return result.Failure is { } failure ? MapFailure(failure) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(OrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await organizations.CreateAsync(request.Name, cancellationToken);

        return result.Failure is { } failure
            ? MapFailure(failure)
            : Created($"/api/organizations/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationResponse>> Rename(
        Guid organizationId, OrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await organizations.RenameAsync(organizationId, request.Name, cancellationToken, request.Version);

        return result.Failure is { } failure ? MapFailure(failure) : Ok(result.Value);
    }

    private ActionResult MapFailure(OrganizationFailure failure)
    {
        switch (failure)
        {
            case OrganizationFailure.NotFound:

                return NotFound();
            case OrganizationFailure.Forbidden:

                return Forbid();
            case OrganizationFailure.InvalidName:
                ModelState.AddModelError("name", "Enter an organization name of 1–160 characters.");

                return ValidationProblem(ModelState);
            case OrganizationFailure.InvalidPagination:
                ModelState.AddModelError("pagination", "page: 1–100000; pageSize: 1–100.");

                return ValidationProblem(ModelState);
            default:
                throw new ArgumentOutOfRangeException(nameof(failure));
        }
    }
}

public sealed record OrganizationRequest(string? Name, Guid? Version = null);
