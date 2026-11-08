using ClientFlow.Api;
using ClientFlow.Application;
using ClientFlow.Common;
using ClientFlow.Domain;
using ClientFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterCommon()
    .RegisterDomain()
    .RegisterApplication()
    .RegisterInfrastructure(builder.Configuration)
    .RegisterAPI(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ClientFlow.Api.Observability.RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.Run();

public partial class Program;
