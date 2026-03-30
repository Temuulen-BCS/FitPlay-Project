using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.Domain
{
    /// <inheritdoc />
    public partial class AddTrainerNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainerNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SenderGymAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GymLocationId = table.Column<int>(type: "int", nullable: false),
                    SubjectUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerNotifications_GymLocations_GymLocationId",
                        column: x => x.GymLocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerNotifications_GymLocationId",
                table: "TrainerNotifications",
                column: "GymLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerNotifications_TrainerId_IsRead",
                table: "TrainerNotifications",
                columns: new[] { "TrainerId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerNotifications");
        }
    }
}
