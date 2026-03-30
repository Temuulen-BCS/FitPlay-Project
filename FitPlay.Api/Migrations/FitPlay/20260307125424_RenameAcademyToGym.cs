using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class RenameAcademyToGym : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymLocations_Academies_AcademyId",
                table: "GymLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainerAcademyLinks_Academies_AcademyId",
                table: "TrainerAcademyLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Academies",
                table: "Academies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainerAcademyLinks",
                table: "TrainerAcademyLinks");

            migrationBuilder.RenameTable(
                name: "Academies",
                newName: "Gyms");

            migrationBuilder.RenameTable(
                name: "TrainerAcademyLinks",
                newName: "TrainerGymLinks");

            migrationBuilder.RenameColumn(
                name: "AcademyId",
                table: "GymLocations",
                newName: "GymId");

            migrationBuilder.RenameColumn(
                name: "AcademyId",
                table: "TrainerGymLinks",
                newName: "GymId");

            migrationBuilder.RenameColumn(
                name: "AcademyAmount",
                table: "PaymentSplits",
                newName: "GymAmount");

            migrationBuilder.RenameIndex(
                name: "IX_Academies_CNPJ",
                table: "Gyms",
                newName: "IX_Gyms_CNPJ");

            migrationBuilder.RenameIndex(
                name: "IX_GymLocations_AcademyId",
                table: "GymLocations",
                newName: "IX_GymLocations_GymId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainerAcademyLinks_AcademyId",
                table: "TrainerGymLinks",
                newName: "IX_TrainerGymLinks_GymId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainerAcademyLinks_TrainerId_AcademyId",
                table: "TrainerGymLinks",
                newName: "IX_TrainerGymLinks_TrainerId_GymId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Gyms",
                table: "Gyms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainerGymLinks",
                table: "TrainerGymLinks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GymLocations_Gyms_GymId",
                table: "GymLocations",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerGymLinks_Gyms_GymId",
                table: "TrainerGymLinks",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymLocations_Gyms_GymId",
                table: "GymLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainerGymLinks_Gyms_GymId",
                table: "TrainerGymLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Gyms",
                table: "Gyms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainerGymLinks",
                table: "TrainerGymLinks");

            migrationBuilder.RenameTable(
                name: "Gyms",
                newName: "Academies");

            migrationBuilder.RenameTable(
                name: "TrainerGymLinks",
                newName: "TrainerAcademyLinks");

            migrationBuilder.RenameColumn(
                name: "GymId",
                table: "GymLocations",
                newName: "AcademyId");

            migrationBuilder.RenameColumn(
                name: "GymId",
                table: "TrainerAcademyLinks",
                newName: "AcademyId");

            migrationBuilder.RenameColumn(
                name: "GymAmount",
                table: "PaymentSplits",
                newName: "AcademyAmount");

            migrationBuilder.RenameIndex(
                name: "IX_Gyms_CNPJ",
                table: "Academies",
                newName: "IX_Academies_CNPJ");

            migrationBuilder.RenameIndex(
                name: "IX_GymLocations_GymId",
                table: "GymLocations",
                newName: "IX_GymLocations_AcademyId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainerGymLinks_GymId",
                table: "TrainerAcademyLinks",
                newName: "IX_TrainerAcademyLinks_AcademyId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainerGymLinks_TrainerId_GymId",
                table: "TrainerAcademyLinks",
                newName: "IX_TrainerAcademyLinks_TrainerId_AcademyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Academies",
                table: "Academies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainerAcademyLinks",
                table: "TrainerAcademyLinks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GymLocations_Academies_AcademyId",
                table: "GymLocations",
                column: "AcademyId",
                principalTable: "Academies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerAcademyLinks_Academies_AcademyId",
                table: "TrainerAcademyLinks",
                column: "AcademyId",
                principalTable: "Academies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
