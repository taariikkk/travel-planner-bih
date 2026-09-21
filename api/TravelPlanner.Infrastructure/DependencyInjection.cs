using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
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

        var wikimediaOptions = configuration.GetSection(WikimediaOptions.SectionName).Get<WikimediaOptions>()
            ?? throw new InvalidOperationException("Wikimedia configuration was not found.");
        if (string.IsNullOrWhiteSpace(wikimediaOptions.UserAgent) || wikimediaOptions.TimeoutSeconds <= 0 || wikimediaOptions.RequestsPerSecond <= 0)
            throw new InvalidOperationException("Wikimedia configuration is invalid.");
        var wikidataOptions = configuration.GetSection(WikidataOptions.SectionName).Get<WikidataOptions>()
            ?? throw new InvalidOperationException("Wikidata configuration was not found.");
        if (!Uri.TryCreate(wikidataOptions.BaseUrl, UriKind.Absolute, out var wikidataBaseUri))
            throw new InvalidOperationException("Wikidata configuration is invalid.");
        var wikipediaOptions = configuration.GetSection(WikipediaOptions.SectionName).Get<WikipediaOptions>()
            ?? throw new InvalidOperationException("Wikipedia configuration was not found.");
        var commonsOptions = configuration.GetSection(WikimediaCommonsOptions.SectionName).Get<WikimediaCommonsOptions>()
            ?? throw new InvalidOperationException("Wikimedia Commons configuration was not found.");
        var importOptions = configuration.GetSection(DestinationImportOptions.SectionName).Get<DestinationImportOptions>()
            ?? throw new InvalidOperationException("Destination import configuration was not found.");
        if (importOptions.TtlHours <= 0 || importOptions.DefaultSuggestedStayMinDays <= 0
            || importOptions.DefaultSuggestedStayMaxDays < importOptions.DefaultSuggestedStayMinDays)
            throw new InvalidOperationException("Destination import configuration is invalid.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(importOptions);
        services.Configure<WikimediaOptions>(configuration.GetSection(WikimediaOptions.SectionName));
        services.Configure<WikidataOptions>(configuration.GetSection(WikidataOptions.SectionName));
        services.Configure<WikipediaOptions>(configuration.GetSection(WikipediaOptions.SectionName));
        services.Configure<WikimediaCommonsOptions>(configuration.GetSection(WikimediaCommonsOptions.SectionName));
        services.AddSingleton<IWikimediaRequestGate, WikimediaRequestGate>();
        services.AddTransient<WikimediaRateLimitHandler>();
        services.AddHttpClient(WikidataProvider.ClientName, client =>
        {
            client.BaseAddress = wikidataBaseUri;
            ConfigureWikimediaClient(client, wikimediaOptions);
        }).AddHttpMessageHandler<WikimediaRateLimitHandler>();
        services.AddHttpClient(WikipediaSummaryProvider.ClientName, client => ConfigureWikimediaClient(client, wikimediaOptions))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler<WikimediaRateLimitHandler>();
        services.AddHttpClient(WikimediaCommonsImageProvider.ClientName, client => ConfigureWikimediaClient(client, wikimediaOptions))
            .AddHttpMessageHandler<WikimediaRateLimitHandler>();

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
        services.AddScoped<IWikipediaSummaryProvider, WikipediaSummaryProvider>();
        services.AddScoped<IImageProvider, WikimediaCommonsImageProvider>();
        services.AddScoped<IDestinationImportRepository, DestinationImportRepository>();
        services.AddScoped<SeedWikidataIdsSeeder>();
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

    private static void ConfigureWikimediaClient(HttpClient client, WikimediaOptions options)
    {
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    }
}
