using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class ReplacePurposeWithModality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "RoomBookings");

            migrationBuilder.DropColumn(
                name: "PurposeDescription",
                table: "RoomBookings");

            migrationBuilder.AddColumn<string>(
                name: "Modality",
                table: "RoomBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules",
                column: "TrainerId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "RoomBookings");

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "RoomBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PurposeDescription",
                table: "RoomBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules",
                column: "TrainerId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }
    }
}
