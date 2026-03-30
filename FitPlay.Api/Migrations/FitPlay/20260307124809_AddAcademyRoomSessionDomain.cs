using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPlay.Api.Migrations.FitPlay
{
    /// <inheritdoc />
    public partial class AddAcademyRoomSessionDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Academies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CNPJ = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CancelFeeRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    StripeAccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Academies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GymLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymLocations_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainerAcademyLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerAcademyLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerAcademyLinks_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymLocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_GymLocations_GymLocationId",
                        column: x => x.GymLocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
             
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomBookings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomBookingId = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    PricePerStudent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSessions_RoomBookings_RoomBookingId",
                        column: x => x.RoomBookingId,
                        principalTable: "RoomBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassSessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassEnrollments_ClassSessions_ClassSessionId",
                        column: x => x.ClassSessionId,
                        principalTable: "ClassSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    AcademyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrainerAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StripeTransferId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSplits_ClassEnrollments_ClassEnrollmentId",
                        column: x => x.ClassEnrollmentId,
                        principalTable: "ClassEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomCheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    XpAwarded = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomCheckIns_ClassEnrollments_ClassEnrollmentId",
                        column: x => x.ClassEnrollmentId,
                        principalTable: "ClassEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Academies_CNPJ",
                table: "Academies",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassEnrollments_ClassSessionId_UserId",
                table: "ClassEnrollments",
                columns: new[] { "ClassSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_RoomBookingId",
                table: "ClassSessions",
                column: "RoomBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_TrainerId_StartTime",
                table: "ClassSessions",
                columns: new[] { "TrainerId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_GymLocations_AcademyId",
                table: "GymLocations",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSplits_ClassEnrollmentId",
                table: "PaymentSplits",
                column: "ClassEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomBookings_RoomId_EndTime",
                table: "RoomBookings",
                columns: new[] { "RoomId", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomBookings_RoomId_StartTime",
                table: "RoomBookings",
                columns: new[] { "RoomId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomBookings_TrainerId_StartTime",
                table: "RoomBookings",
                columns: new[] { "TrainerId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomCheckIns_ClassEnrollmentId_UserId",
                table: "RoomCheckIns",
                columns: new[] { "ClassEnrollmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_GymLocationId",
                table: "Rooms",
                column: "GymLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAcademyLinks_AcademyId",
                table: "TrainerAcademyLinks",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAcademyLinks_TrainerId_AcademyId",
                table: "TrainerAcademyLinks",
                columns: new[] { "TrainerId", "AcademyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSplits");

            migrationBuilder.DropTable(
                name: "RoomCheckIns");

            migrationBuilder.DropTable(
                name: "TrainerAcademyLinks");

            migrationBuilder.DropTable(
                name: "ClassEnrollments");

            migrationBuilder.DropTable(
                name: "ClassSessions");

            migrationBuilder.DropTable(
                name: "RoomBookings");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "GymLocations");

            migrationBuilder.DropTable(
                name: "Academies");
        }
    }
}
