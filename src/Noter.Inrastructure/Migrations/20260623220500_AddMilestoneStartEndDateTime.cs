using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noter.Inrastructure.Migrations
{
    public partial class AddMilestoneStartEndDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "Milestones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "Milestones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql("UPDATE \"Milestones\" SET \"StartDateTime\" = \"DueDateTime\", \"EndDateTime\" = \"DueDateTime\";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "Milestones");
        }
    }
}
