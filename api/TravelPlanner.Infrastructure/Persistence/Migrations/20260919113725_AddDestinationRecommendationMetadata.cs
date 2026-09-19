using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationRecommendationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "BestSeasons",
                table: "Destination",
                type: "text[]",
                nullable: false,
                defaultValue: Array.Empty<string>());

            migrationBuilder.AddColumn<string>(
                name: "BudgetTier",
                table: "Destination",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SuggestedStayMaxDays",
                table: "Destination",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedStayMinDays",
                table: "Destination",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "standard", 4, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "standard", 3, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "standard", 3, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "summer" }, "standard", 5, 3 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "winter", "summer" }, "premium", 4, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "winter", "summer" }, "standard", 3, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "budget", 3, 2 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "budget", 2, 1 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "budget", 2, 1 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "budget", 2, 1 });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "BestSeasons", "BudgetTier", "SuggestedStayMaxDays", "SuggestedStayMinDays" },
                values: new object[] { new List<string> { "spring", "summer", "autumn" }, "standard", 3, 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestSeasons",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "BudgetTier",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "SuggestedStayMaxDays",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "SuggestedStayMinDays",
                table: "Destination");

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                column: "Tags",
                value: new List<string> { "historija", "kultura", "grad", "hrana" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                column: "Tags",
                value: new List<string> { "historija", "kultura", "rijeka", "hrana" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                column: "Tags",
                value: new List<string> { "vino", "kultura", "rijeka", "grad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                column: "Tags",
                value: new List<string> { "more", "porodica", "wellness" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                column: "Tags",
                value: new List<string> { "planina", "zima", "avantura" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                column: "Tags",
                value: new List<string> { "planina", "zima", "avantura", "priroda" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                column: "Tags",
                value: new List<string> { "grad", "kultura", "rijeka" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                column: "Tags",
                value: new List<string> { "historija", "kultura", "planina" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                column: "Tags",
                value: new List<string> { "historija", "kultura", "rijeka" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                column: "Tags",
                value: new List<string> { "historija", "kultura", "rijeka" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                column: "Tags",
                value: new List<string> { "historija", "priroda", "rijeka" });
        }
    }
}
