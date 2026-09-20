using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEnSourceUrl",
                table: "Destination",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommendationEligible",
                table: "Destination",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Destination",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "sarajevo" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "mostar" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "trebinje" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "neum" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "jahorina" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "bjelasnica" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "banja-luka" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "travnik" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "pocitelj" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "visegrad" });

            migrationBuilder.UpdateData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"),
                columns: new[] { "DescriptionEn", "DescriptionEnSourceUrl", "IsRecommendationEligible", "Slug" },
                values: new object[] { null, null, true, "jajce" });

            migrationBuilder.CreateIndex(
                name: "IX_Destination_Slug",
                table: "Destination",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Destination_Slug",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "DescriptionEnSourceUrl",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "IsRecommendationEligible",
                table: "Destination");

            migrationBuilder.DropColumn(
                name: "Slug",
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
