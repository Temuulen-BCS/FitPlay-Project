using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class AddQueueSkippedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SkippedAt",
                table: "ClassQueueEntries",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkippedAt",
                table: "ClassQueueEntries");
        }
    }
}
