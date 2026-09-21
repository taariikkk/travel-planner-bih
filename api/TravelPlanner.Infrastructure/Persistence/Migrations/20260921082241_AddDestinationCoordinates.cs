using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Destination",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Destination",
                type: "double precision",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.859000000000002, 18.428999999999998 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.337299999999999, 17.815000000000001 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 42.710999999999999, 18.344000000000001 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 42.923000000000002, 17.616 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.734999999999999, 18.568999999999999 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.715000000000003, 18.288 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 44.771999999999998, 17.190999999999999 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 44.226999999999997, 17.664999999999999 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.134, 17.731999999999999 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 43.781999999999996, 19.292999999999999 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 44.338000000000001, 17.27 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "Longitude",
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
