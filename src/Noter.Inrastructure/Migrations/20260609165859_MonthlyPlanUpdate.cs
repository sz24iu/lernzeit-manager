using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MonthlyPlanUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MonthlyPlans_Id",
                table: "Milestones");

            migrationBuilder.AddColumn<Guid>(
                name: "MonthlyPlanId",
                table: "Milestones",
                type: "uuid",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MonthlyPlans_MonthlyPlanId",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_MonthlyPlanId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "MonthlyPlanId",
                table: "Milestones");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_MonthlyPlans_Id",
                table: "Milestones",
                column: "Id",
                principalTable: "MonthlyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
