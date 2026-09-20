using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Api.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<DestinationTranslation> DestinationTranslations => Set<DestinationTranslation>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripDay> TripDays => Set<TripDay>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Accommodation> Accommodations => Set<Accommodation>();
    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.Property(user => user.Email).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.DisplayName).IsRequired();
            entity.Property(user => user.Preferences).HasColumnType("text[]");
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("Destination");
            entity.Property(destination => destination.Name).IsRequired();
            entity.Property(destination => destination.Type).IsRequired().HasMaxLength(16);
            entity.Property(destination => destination.Source).IsRequired().HasMaxLength(16);
            entity.Property(destination => destination.ExternalId).HasMaxLength(32);
            entity.Property(destination => destination.Region).IsRequired();
            entity.Property(destination => destination.Description).IsRequired();
            entity.Property(destination => destination.DescriptionLanguage).HasMaxLength(8);
            entity.Property(destination => destination.BestTimeToVisit).IsRequired();
            entity.Property(destination => destination.BudgetTier).IsRequired().HasMaxLength(16);
            entity.Property(destination => destination.SuggestedStayMinDays).IsRequired();
            entity.Property(destination => destination.SuggestedStayMaxDays).IsRequired();
            ConfigureStringList(entity.Property(destination => destination.BestSeasons));
            ConfigureStringList(entity.Property(destination => destination.Tags));
            ConfigureStringList(entity.Property(destination => destination.ManualOverrideFields));
            entity.HasIndex(destination => destination.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
            entity.HasIndex(destination => destination.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasData(DestinationSeedData.Destinations);
        });

        modelBuilder.Entity<DestinationTranslation>(entity =>
        {
            entity.ToTable("DestinationTranslation");
            entity.Property(translation => translation.LanguageCode).IsRequired().HasMaxLength(2);
            entity.Property(translation => translation.Description).IsRequired();
            entity.Property(translation => translation.BestTimeToVisit).IsRequired();
            entity.HasIndex(translation => new { translation.DestinationId, translation.LanguageCode }).IsUnique();
            entity.HasOne(translation => translation.Destination).WithMany(destination => destination.Translations)
                .HasForeignKey(translation => translation.DestinationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasData(DestinationSeedData.Translations);
        });

        modelBuilder.Entity<Place>(entity =>
        {
            entity.ToTable("Place");
            entity.Property(place => place.Name).IsRequired();
            entity.Property(place => place.Category).IsRequired();
            entity.Property(place => place.Location).HasColumnType("geometry (point,4326)");
            entity.Property(place => place.Source).IsRequired();
            entity.HasOne(place => place.Destination).WithMany(destination => destination.Places)
                .HasForeignKey(place => place.DestinationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("Trip");
            entity.Property(trip => trip.Title).IsRequired();
            entity.HasOne(trip => trip.User).WithMany(user => user.Trips)
                .HasForeignKey(trip => trip.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TripDay>(entity =>
        {
            entity.ToTable("TripDay");
            entity.HasIndex(day => new { day.TripId, day.DayNumber }).IsUnique();
            entity.HasOne(day => day.Trip).WithMany(trip => trip.Days)
                .HasForeignKey(day => day.TripId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItineraryItem>(entity =>
        {
            entity.ToTable("ItineraryItem");
            entity.HasIndex(item => new { item.TripDayId, item.Order }).IsUnique();
            entity.HasOne(item => item.TripDay).WithMany(day => day.ItineraryItems)
                .HasForeignKey(item => item.TripDayId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Place).WithMany(place => place.ItineraryItems)
                .HasForeignKey(item => item.PlaceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expense");
            entity.Property(expense => expense.Category).IsRequired();
            entity.Property(expense => expense.AmountKM).HasPrecision(18, 2);
            entity.Property(expense => expense.Description).IsRequired();
            entity.HasOne(expense => expense.Trip).WithMany(trip => trip.Expenses)
                .HasForeignKey(expense => expense.TripId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Accommodation>(entity =>
        {
            entity.ToTable("Accommodation");
            entity.Property(accommodation => accommodation.Name).IsRequired();
            entity.Property(accommodation => accommodation.Type).IsRequired();
            entity.Property(accommodation => accommodation.PricePerNightKM).HasPrecision(18, 2);
            entity.Property(accommodation => accommodation.ContactLink).IsRequired();
            entity.HasOne(accommodation => accommodation.Destination).WithMany(destination => destination.Accommodations)
                .HasForeignKey(accommodation => accommodation.DestinationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeatherSnapshot>(entity =>
        {
            entity.ToTable("WeatherSnapshot");
            entity.Property(snapshot => snapshot.RawData).IsRequired();
            entity.HasOne(snapshot => snapshot.Destination).WithMany(destination => destination.WeatherSnapshots)
                .HasForeignKey(snapshot => snapshot.DestinationId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureStringList(PropertyBuilder<List<string>> property)
    {
        property.HasColumnType("text[]");
        property.Metadata.SetValueComparer(new ValueComparer<List<string>>(
            (left, right) => left!.SequenceEqual(right!),
            values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode(StringComparison.Ordinal))),
            values => values.ToList()));
    }
}
