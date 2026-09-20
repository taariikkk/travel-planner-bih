using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Infrastructure.Persistence.Migrations;

public partial class CorrectBjelasnicaRegion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.UpdateData("Destination", "Id",
            new Guid("1a0a0001-0000-4000-8000-000000000006"), "Region", "Sarajevski kanton");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.UpdateData("Destination", "Id",
            new Guid("1a0a0001-0000-4000-8000-000000000006"), "Region", "Centralna Bosna");
}
