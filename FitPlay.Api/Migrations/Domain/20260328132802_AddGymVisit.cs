using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.Domain
{
    /// <inheritdoc />
    public partial class AddGymVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GymLocationId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInLatitude = table.Column<double>(type: "float", nullable: false),
                    CheckInLongitude = table.Column<double>(type: "float", nullable: false),
                    CheckOutLatitude = table.Column<double>(type: "float", nullable: true),
                    CheckOutLongitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymVisits_GymLocations_GymLocationId",
                        column: x => x.GymLocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymVisits_GymLocationId",
                table: "GymVisits",
                column: "GymLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GymVisits_UserId_CheckOutTime",
                table: "GymVisits",
                columns: new[] { "UserId", "CheckOutTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymVisits");
        }
    }
}
