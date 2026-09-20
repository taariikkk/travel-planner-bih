using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationDescriptionLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEnLicense",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionLicense",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "DescriptionEnLicense", "DescriptionLicense" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEnLicense",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "DescriptionLicense",
                table: "Destination");

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "kultura", "grad", "hrana" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "kultura", "rijeka", "hrana" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "vino", "kultura", "rijeka", "grad" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "summer" }, new List<string>(), new List<string> { "more", "porodica", "wellness" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "winter", "summer" }, new List<string>(), new List<string> { "planina", "zima", "avantura" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "winter", "summer" }, new List<string>(), new List<string> { "planina", "zima", "avantura", "priroda" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "grad", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "kultura", "planina" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "kultura", "rijeka" } });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "BestSeasons", "ManualOverrideFields", "Tags" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, new List<string>(), new List<string> { "historija", "priroda", "rijeka" } });
        }
    }
}
