using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemSourceTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceEntityId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceEntityType",
                table: "TaskItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceEntityId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "SourceEntityType",
                table: "TaskItems");
        }
    }
}
