using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class AddTrainerIdToClassSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainerId",
                table: "ClassSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSchedules_TrainerId_ScheduledAt",
                table: "ClassSchedules",
                columns: new[] { "TrainerId", "ScheduledAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules",
                column: "TrainerId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_Teachers_TrainerId",
                table: "ClassSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ClassSchedules_TrainerId_ScheduledAt",
                table: "ClassSchedules");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "ClassSchedules");
        }
    }
}
