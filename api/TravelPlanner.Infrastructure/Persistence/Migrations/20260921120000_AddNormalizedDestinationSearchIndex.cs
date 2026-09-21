using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelPlanner.Api.Infrastructure.Persistence;

#nullable disable

namespace TravelPlanner.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921120000_AddNormalizedDestinationSearchIndex")]
public partial class AddNormalizedDestinationSearchIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION immutable_unaccent(text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            PARALLEL SAFE
            STRICT
            AS $function$
                SELECT public.unaccent('public.unaccent', $1)
            $function$;

            CREATE INDEX "IX_Destination_NormalizedName_Trgm"
            ON "Destination"
            USING gin (immutable_unaccent(lower("Name")) gin_trgm_ops);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_Destination_NormalizedName_Trgm";
            DROP FUNCTION IF EXISTS immutable_unaccent(text);
            """);
    }
}
