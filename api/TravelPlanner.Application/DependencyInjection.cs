using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
namespace TravelPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IDestinationImportService, DestinationImportService>();
        services.AddScoped<IDestinationSearchService, DestinationSearchService>();
        return services;
    }
}
