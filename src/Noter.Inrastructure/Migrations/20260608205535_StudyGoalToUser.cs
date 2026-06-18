using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudyGoalToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyGoals_Users_UserId1",
                table: "StudyGoals");

            migrationBuilder.DropIndex(
                name: "IX_StudyGoals_UserId1",
                table: "StudyGoals");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "StudyGoals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "StudyGoals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StudyGoals_UserId1",
                table: "StudyGoals",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyGoals_Users_UserId1",
                table: "StudyGoals",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
