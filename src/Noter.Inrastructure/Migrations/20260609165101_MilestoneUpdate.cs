using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MilestoneUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyPlans_StudyPlans_StudyPlanId",
                table: "MonthlyPlans");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyPlans_StudyPlanId",
                table: "MonthlyPlans");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Milestones");

            migrationBuilder.AddColumn<Guid>(
                name: "StudyGoalId",
                table: "Milestones",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_StudyGoalId",
                table: "Milestones",
                column: "StudyGoalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_StudyGoals_StudyGoalId",
                table: "Milestones",
                column: "StudyGoalId",
                principalTable: "StudyGoals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_StudyGoals_StudyGoalId",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_StudyGoalId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "StudyGoalId",
                table: "Milestones");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Milestones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlans_StudyPlanId",
                table: "MonthlyPlans",
                column: "StudyPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyPlans_StudyPlans_StudyPlanId",
                table: "MonthlyPlans",
                column: "StudyPlanId",
                principalTable: "StudyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
