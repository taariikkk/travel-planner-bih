# EF Core Foundation Design

## Goal

Introduce the initial PostgreSQL/PostGIS persistence model for the Travel Planner BiH API. The model must support users, curated destinations and places, trips and itineraries, expenses, accommodations, and cached weather snapshots.

## Scope

The API project gains EF Core with the Npgsql PostgreSQL provider and NetTopologySuite support. `ApplicationDbContext` is registered through dependency injection and reads `ConnectionStrings:DefaultConnection`; local development supplies that value in `appsettings.Development.json` for the Docker Compose database.

This change does not add HTTP endpoints, authentication logic, seed data, repositories with business queries, external providers, Redis, SignalR, or Hangfire. Future controllers must access persistence through repositories rather than injecting `ApplicationDbContext` directly.

## Entity Model

All entities use generated `Guid` primary keys. Required textual fields use non-null CLR properties and are configured as required. `DateOnly` represents calendar-only trip dates, and `DateTimeOffset` represents instants in UTC.

- `User`: email, password hash, display name, creation time, and a PostgreSQL `text[]` preferences list.
- `Destination`: name, region, description, best time to visit, and a PostgreSQL `text[]` tags list.
- `Place`: destination foreign key, name, category, PostGIS `Point` location, and source. Points use WGS 84 (SRID 4326).
- `Trip`: user foreign key, title, start/end dates, and traveler count.
- `TripDay`: trip foreign key, one-based day number, and calendar date. `(TripId, DayNumber)` is unique.
- `ItineraryItem`: trip-day and place foreign keys, ordering number, and optional notes. `(TripDayId, Order)` is unique.
- `Expense`: trip foreign key, category, BAM amount using `numeric(18,2)`, and description.
- `Accommodation`: destination foreign key, name, type, BAM nightly price using `numeric(18,2)`, and contact link.
- `WeatherSnapshot`: destination foreign key, UTC fetch timestamp, and raw provider payload.

Relationships are required from dependent entities to their parent records. Deleting a `Trip` cascades to its `TripDay`, `ItineraryItem`, and `Expense` records. Deleting a `Destination` cascades to its `Place`, `Accommodation`, and `WeatherSnapshot` records. `ItineraryItem -> Place` uses restrictive deletion so a referenced place cannot be silently removed. User deletion is restrictive while trips exist.

## Database and Migration

The initial migration first enables PostGIS using `CREATE EXTENSION IF NOT EXISTS postgis`, then creates all entity tables, foreign keys, indexes, unique constraints, array columns, and the spatial point column. Its down migration drops the tables before removing the PostGIS extension.

The migration is generated using the EF CLI and applied to the local Docker database configured by `DefaultConnection`.

## Testing and Verification

An xUnit test project will exercise the EF model metadata without needing a live database: table names, PostgreSQL array mappings, decimal precision, PostGIS SRID/type, required foreign keys, and unique indexes. The implementation is accepted when unit tests pass, the solution builds, `dotnet ef migrations list` sees the initial migration, and `dotnet ef database update` applies it successfully to the local PostGIS instance.

## Constraints

- PostgreSQL and PostGIS only; no paid provider or service is introduced.
- No existing EF migration is edited; this is the first migration.
- Secrets remain out of source control. The development connection string uses only the existing local Docker credentials.
- No controller receives `ApplicationDbContext` directly; repositories will be the API boundary when endpoint work begins.
