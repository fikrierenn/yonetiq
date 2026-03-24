using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKullaniciSifre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SifreHash",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SifreTuzu",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SifreHash",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SifreTuzu",
                table: "Kullanicilar");
        }
    }
}
