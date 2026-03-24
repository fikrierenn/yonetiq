using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentMeetingId",
                table: "Meetings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "Meetings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "Meetings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceType",
                table: "Meetings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentMeetingId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RecurrenceType",
                table: "Meetings");
        }
    }
}
