using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeletedMonatPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MonthlyPlans_MonthlyPlanId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionPlans_MonthlyPlans_Id",
                table: "StudySessionPlans");

            migrationBuilder.DropTable(
                name: "MonthlyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_MonthlyPlanId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "MonthlyPlanId",
                table: "Milestones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MonthlyPlanId",
                table: "Milestones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MonthlyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    StudyPlanId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_MonthlyPlanId",
                table: "Milestones",
                column: "MonthlyPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_MonthlyPlans_MonthlyPlanId",
                table: "Milestones",
                column: "MonthlyPlanId",
                principalTable: "MonthlyPlans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionPlans_MonthlyPlans_Id",
                table: "StudySessionPlans",
                column: "Id",
                principalTable: "MonthlyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
