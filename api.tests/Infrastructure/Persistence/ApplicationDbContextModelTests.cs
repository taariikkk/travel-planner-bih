using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using TravelPlanner.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class ApplicationDbContextModelTests
{
    [Fact]
    public void Model_maps_users_to_the_User_table()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=travelplanner", options => options.UseNetTopologySuite())
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.Equal("User", context.Model.FindEntityType("TravelPlanner.Api.Domain.Entities.User")!.GetTableName());
    }

    [Fact]
    public void Model_maps_all_travel_entities_to_their_database_tables()
    {
        using var context = CreateContext();

        Assert.Equal("Destination", context.Model.FindEntityType(typeof(Destination))!.GetTableName());
        Assert.Equal("Place", context.Model.FindEntityType(typeof(Place))!.GetTableName());
        Assert.Equal("Trip", context.Model.FindEntityType(typeof(Trip))!.GetTableName());
        Assert.Equal("TripDay", context.Model.FindEntityType(typeof(TripDay))!.GetTableName());
        Assert.Equal("ItineraryItem", context.Model.FindEntityType(typeof(ItineraryItem))!.GetTableName());
        Assert.Equal("Expense", context.Model.FindEntityType(typeof(Expense))!.GetTableName());
        Assert.Equal("Accommodation", context.Model.FindEntityType(typeof(Accommodation))!.GetTableName());
        Assert.Equal("WeatherSnapshot", context.Model.FindEntityType(typeof(WeatherSnapshot))!.GetTableName());
    }

    [Fact]
    public void Model_maps_collections_currency_geography_and_ordering_constraints()
    {
        using var context = CreateContext();

        var user = context.Model.FindEntityType(typeof(User))!;
        var destination = context.Model.FindEntityType(typeof(Destination))!;
        var place = context.Model.FindEntityType(typeof(Place))!;
        var expense = context.Model.FindEntityType(typeof(Expense))!;
        var tripDay = context.Model.FindEntityType(typeof(TripDay))!;
        var itineraryItem = context.Model.FindEntityType(typeof(ItineraryItem))!;

        Assert.Equal("text[]", user.FindProperty(nameof(User.Preferences))!.GetColumnType());
        Assert.Equal("text[]", destination.FindProperty(nameof(Destination.Tags))!.GetColumnType());
        Assert.Equal("geometry (point,4326)", place.FindProperty(nameof(Place.Location))!.GetColumnType());
        Assert.Equal("numeric(18,2)", expense.FindProperty(nameof(Expense.AmountKM))!.GetColumnType());
        Assert.Contains(tripDay.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(TripDay.TripId), nameof(TripDay.DayNumber)]));
        Assert.Contains(itineraryItem.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ItineraryItem.TripDayId), nameof(ItineraryItem.Order)]));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=travelplanner", options => options.UseNetTopologySuite())
            .Options;

        return new ApplicationDbContext(options);
    }
}
