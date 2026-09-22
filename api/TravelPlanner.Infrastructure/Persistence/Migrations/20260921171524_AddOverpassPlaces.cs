using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOverpassPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Place_Destination_DestinationId",
                table: "Place");

            migrationBuilder.DropIndex(
                name: "IX_Place_DestinationId",
                table: "Place");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cuisine",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionBs",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionLanguage",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAuthor",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageLicense",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageSourceUrl",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAt",
                table: "Place",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastVerifiedAt",
                table: "Place",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualKey",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ManualOverrideFields",
                table: "Place",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceLevel",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Place",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DestinationPlace",
                columns: table => new
                {
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationPlace", x => new { x.DestinationId, x.PlaceId });
                    table.ForeignKey(
                        name: "FK_DestinationPlace_Destination_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destination",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestinationPlace_Place_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Place",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve IDs and the original ownership before removing the single-destination column.
            migrationBuilder.Sql("""
                INSERT INTO "DestinationPlace" ("DestinationId", "PlaceId", "IsManual")
                SELECT "DestinationId", "Id", "Source" = 'manual' FROM "Place";
                UPDATE "Place" SET "ManualOverrideFields" = ARRAY['Name', 'Category', 'Location']
                WHERE "Source" = 'manual';
                """);
            migrationBuilder.DropColumn(name: "DestinationId", table: "Place");

            migrationBuilder.CreateTable(
                name: "OverpassRequestState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextImportAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BudgetDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverpassRequestState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlacesImportState",
                columns: table => new
                {
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuerySignature = table.Column<string>(type: "text", nullable: true),
                    SucceededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacesImportState", x => x.DestinationId);
                    table.ForeignKey(
                        name: "FK_PlacesImportState_Destination_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destination",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Place_ExternalId",
                table: "Place",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Place_ManualKey",
                table: "Place",
                column: "ManualKey",
                unique: true,
                filter: "\"ManualKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DestinationPlace_PlaceId",
                table: "DestinationPlace",
                column: "PlaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "Place" p LEFT JOIN "DestinationPlace" l ON l."PlaceId" = p."Id"
                               GROUP BY p."Id" HAVING COUNT(l."DestinationId") <> 1) THEN
                        RAISE EXCEPTION 'Cannot downgrade shared or unlinked places without losing data.';
                    END IF;
                END $$;
                ALTER TABLE "Place" ADD COLUMN "DestinationId" uuid;
                UPDATE "Place" p SET "DestinationId" = l."DestinationId" FROM "DestinationPlace" l WHERE l."PlaceId" = p."Id";
                ALTER TABLE "Place" ALTER COLUMN "DestinationId" SET NOT NULL;
                """);
            migrationBuilder.DropTable(
                name: "DestinationPlace");

            migrationBuilder.DropTable(
                name: "OverpassRequestState");

            migrationBuilder.DropTable(
                name: "PlacesImportState");

            migrationBuilder.DropIndex(
                name: "IX_Place_ExternalId",
                table: "Place");

            migrationBuilder.DropIndex(
                name: "IX_Place_ManualKey",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Cuisine",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "DescriptionBs",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "DescriptionLanguage",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ImageAuthor",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ImageLicense",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ImageSourceUrl",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ManualKey",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "ManualOverrideFields",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "PriceLevel",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Place");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Place");

            migrationBuilder.CreateIndex(
                name: "IX_Place_DestinationId",
                table: "Place",
                column: "DestinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Place_Destination_DestinationId",
                table: "Place",
                column: "DestinationId",
                principalTable: "Destination",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
