using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDestinationDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionLanguage",
                table: "Destination",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSourceUrl",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElevationM",
                table: "Destination",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Destination",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAuthor",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageLicense",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageSourceUrl",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAt",
                table: "Destination",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ManualOverrideFields",
                table: "Destination",
                type: "text[]",
                nullable: false,
                defaultValue: Array.Empty<string>());

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Destination",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Destination",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Destination",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "ostalo" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "planina" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "planina" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "DescriptionLanguage", "DescriptionSourceUrl", "ElevationM", "ExternalId", "ImageAuthor", "ImageLicense", "ImageSourceUrl", "ImageUrl", "ImportedAt", "ManualOverrideFields", "Population", "Source", "Type" },
                values: new object[] { "bs", null, null, null, null, null, null, null, null, new List<string>(), null, "manual", "grad" });

            migrationBuilder.CreateIndex(
                name: "IX_Destination_ExternalId",
                table: "Destination",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Destination_Name",
                table: "Destination",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Destination_ExternalId",
                table: "Destination");

            migrationBuilder.DropIndex(
                name: "IX_Destination_Name",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "DescriptionLanguage",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "DescriptionSourceUrl",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ElevationM",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ImageAuthor",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ImageLicense",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ImageSourceUrl",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "ManualOverrideFields",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Destination");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "kultura", "grad", "hrana" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "kultura", "rijeka", "hrana" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "vino", "kultura", "rijeka", "grad" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "summer" }, new List<string> { "more", "porodica", "wellness" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "winter", "summer" }, new List<string> { "planina", "zima", "avantura" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "winter", "summer" }, new List<string> { "planina", "zima", "avantura", "priroda" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "grad", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "kultura", "planina" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "BestSeasons", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string> { "historija", "priroda", "rijeka" } });
        }
    }
}
