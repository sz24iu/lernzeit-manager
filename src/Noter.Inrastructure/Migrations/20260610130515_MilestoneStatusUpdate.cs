using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MilestoneStatusUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Milestones");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Milestones",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Milestones");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Milestones",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
