using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataSourceId",
                table: "QueryRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EncryptedConnectionString = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryRecords_DataSourceId",
                table: "QueryRecords",
                column: "DataSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_QueryRecords_DataSources_DataSourceId",
                table: "QueryRecords",
                column: "DataSourceId",
                principalTable: "DataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueryRecords_DataSources_DataSourceId",
                table: "QueryRecords");

            migrationBuilder.DropTable(
                name: "DataSources");

            migrationBuilder.DropIndex(
                name: "IX_QueryRecords_DataSourceId",
                table: "QueryRecords");

            migrationBuilder.DropColumn(
                name: "DataSourceId",
                table: "QueryRecords");
        }
    }
}
