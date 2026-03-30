using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class AddGymOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Gyms",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Gyms");
        }
    }
}
