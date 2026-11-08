using ClientFlow.Application.Workspace;
using ClientFlow.Domain.Shared;
using ClientFlow.Domain.Organizations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace ClientFlow.Api.Errors;

internal sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            ResourceNotFoundException => 404,
            AccessForbiddenException or OrganizationAccessDeniedException => 403,
            StaleRecordException => 409,
            BusinessRuleException or InvalidOrganizationNameException => 400,
            _ => 500
        };
        context.Response.StatusCode = status;
        if (status == 500)
        {
            logger.LogError(exception, "Request failed with trace {TraceId}", context.TraceIdentifier);
        }

        var detail = new ProblemDetails
        {
            Status = status,
            Title = status switch { 404 => "Not found", 403 => "Access denied", 409 => "Record changed", 400 => "Validation failed", _ => "Something went wrong" },
            Detail = status == 500 ? "The request could not be completed. Try again or quote the trace ID." : exception.Message
        };
        detail.Extensions["traceId"] = context.TraceIdentifier;

        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = detail });
    }
}
