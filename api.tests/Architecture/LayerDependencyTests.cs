using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.Services;
using Xunit;

namespace TravelPlanner.Api.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_reference_outer_layers_or_technical_frameworks()
    {
        var references = typeof(User).Assembly.GetReferencedAssemblies().Select(reference => reference.Name!);
        Assert.DoesNotContain(references, name =>
            name.StartsWith("TravelPlanner.") || name.StartsWith("Microsoft.EntityFrameworkCore") ||
            name.StartsWith("Microsoft.AspNetCore") || name.StartsWith("Microsoft.IdentityModel") ||
            name.StartsWith("Npgsql"));
    }

    [Fact]
    public void Application_references_Domain_but_not_Infrastructure_or_WebAPI()
    {
        var references = typeof(AuthService).Assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToArray();
        Assert.Contains("TravelPlanner.Domain", references);
        Assert.DoesNotContain(references, name =>
            name == "TravelPlanner.Infrastructure" || name == "TravelPlanner.WebAPI" ||
            name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.AspNetCore") ||
            name.StartsWith("Microsoft.IdentityModel") || name.StartsWith("Konscious") || name.StartsWith("Npgsql"));
    }
}
