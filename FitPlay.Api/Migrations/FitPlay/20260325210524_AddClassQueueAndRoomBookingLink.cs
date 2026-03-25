using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class AddClassQueueAndRoomBookingLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomBookingId",
                table: "ClassSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassQueueEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassScheduleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HasMembership = table.Column<bool>(type: "bit", nullable: false),
                    QueueCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassQueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassQueueEntries_ClassSchedules_ClassScheduleId",
                        column: x => x.ClassScheduleId,
                        principalTable: "ClassSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSchedules_RoomBookingId",
                table: "ClassSchedules",
                column: "RoomBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassQueueEntries_ClassScheduleId_UserId",
                table: "ClassQueueEntries",
                columns: new[] { "ClassScheduleId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_RoomBookings_RoomBookingId",
                table: "ClassSchedules",
                column: "RoomBookingId",
                principalTable: "RoomBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_RoomBookings_RoomBookingId",
                table: "ClassSchedules");

            migrationBuilder.DropTable(
                name: "ClassQueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_ClassSchedules_RoomBookingId",
                table: "ClassSchedules");

            migrationBuilder.DropColumn(
                name: "RoomBookingId",
                table: "ClassSchedules");
        }
    }
}
