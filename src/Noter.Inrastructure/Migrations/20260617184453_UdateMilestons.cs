using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UdateMilestons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionPlans_Milestones_Id",
                table: "StudySessionPlans");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessionPlans_MilestoneId",
                table: "StudySessionPlans",
                column: "MilestoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionPlans_Milestones_MilestoneId",
                table: "StudySessionPlans",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionPlans_Milestones_MilestoneId",
                table: "StudySessionPlans");

            migrationBuilder.DropIndex(
                name: "IX_StudySessionPlans_MilestoneId",
                table: "StudySessionPlans");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionPlans_Milestones_Id",
                table: "StudySessionPlans",
                column: "Id",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
