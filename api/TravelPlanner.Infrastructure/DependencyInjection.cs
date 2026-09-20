using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using TravelPlanner.Infrastructure.Security;
using TravelPlanner.Infrastructure.Providers;

namespace TravelPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

        var wikidataOptions = configuration.GetSection(WikidataOptions.SectionName).Get<WikidataOptions>()
            ?? throw new InvalidOperationException("Wikidata configuration was not found.");
        if (string.IsNullOrWhiteSpace(wikidataOptions.UserAgent) || wikidataOptions.TimeoutSeconds <= 0
            || !Uri.TryCreate(wikidataOptions.BaseUrl, UriKind.Absolute, out var wikidataBaseUri))
            throw new InvalidOperationException("Wikidata configuration is invalid.");
        services.Configure<WikidataOptions>(configuration.GetSection(WikidataOptions.SectionName));
        services.AddHttpClient(WikidataProvider.ClientName, client =>
        {
            client.BaseAddress = wikidataBaseUri;
            client.Timeout = TimeSpan.FromSeconds(wikidataOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(wikidataOptions.UserAgent);
        });

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration was not found.");
        if (string.IsNullOrWhiteSpace(jwtOptions.Key) || string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("JWT key, issuer, and audience must be configured.");

        services.AddSingleton(jwtOptions);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<IDestinationDetailsRepository, DestinationDetailsRepository>();
        services.AddScoped<ISavedPlaceRepository, SavedPlaceRepository>();
        services.AddScoped<IDestinationDataProvider, WikidataProvider>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true, ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            });

        return services;
    }
}
