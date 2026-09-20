using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedPlace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPlace", x => x.Id);
                    table.CheckConstraint("CK_SavedPlace_ExactlyOneTarget", "(\"DestinationId\" IS NOT NULL AND \"PlaceId\" IS NULL) OR (\"DestinationId\" IS NULL AND \"PlaceId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SavedPlace_Destination_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destination",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedPlace_Place_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Place",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedPlace_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlace_DestinationId",
                table: "SavedPlace",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlace_PlaceId",
                table: "SavedPlace",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlace_UserId_DestinationId",
                table: "SavedPlace",
                columns: new[] { "UserId", "DestinationId" },
                unique: true,
                filter: "\"DestinationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlace_UserId_PlaceId",
                table: "SavedPlace",
                columns: new[] { "UserId", "PlaceId" },
                unique: true,
                filter: "\"PlaceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedPlace");

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
