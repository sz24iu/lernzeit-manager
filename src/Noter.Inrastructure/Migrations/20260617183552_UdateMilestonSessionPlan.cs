using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UdateMilestonSessionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MilestoneId",
                table: "StudySessionPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionPlans_Milestones_Id",
                table: "StudySessionPlans",
                column: "Id",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionPlans_Milestones_Id",
                table: "StudySessionPlans");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "StudySessionPlans");
        }
    }
}
