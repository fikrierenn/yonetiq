using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueryRecordlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sql = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    SonucTipi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KullanimAmaci = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Renk = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PivotSatirKolon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PivotSutunKolon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PivotDegerKolon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuncellenmeTarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryRecordlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SorguSetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ikon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NavMenuGoster = table.Column<bool>(type: "bit", nullable: false),
                    NavMenuSira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SorguSetleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Toplantilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Yer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ToplantiLinki = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Katilimcilar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Gundem = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Notlar = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toplantilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gorevler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Atanan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AtananKullaniciId = table.Column<int>(type: "int", nullable: true),
                    AtanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Oncelik = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gorevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gorevler_Kullanicilar_AtananKullaniciId",
                        column: x => x.AtananKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RaporFavoriler",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    SorguId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaporFavoriler", x => new { x.KullaniciId, x.SorguId });
                    table.ForeignKey(
                        name: "FK_RaporFavoriler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaporFavoriler_QueryRecordlar_SorguId",
                        column: x => x.SorguId,
                        principalTable: "QueryRecordlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SetiSorgular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SetiId = table.Column<int>(type: "int", nullable: false),
                    SorguId = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    GenislikMd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetiSorgular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetiSorgular_QueryRecordlar_SorguId",
                        column: x => x.SorguId,
                        principalTable: "QueryRecordlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SetiSorgular_SorguSetleri_SetiId",
                        column: x => x.SetiId,
                        principalTable: "SorguSetleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kararlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToplantiId = table.Column<int>(type: "int", nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Sorumlu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SorumluKullaniciId = table.Column<int>(type: "int", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GoreveDonustuMu = table.Column<bool>(type: "bit", nullable: false),
                    BagliGorevId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kararlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kararlar_Gorevler_BagliGorevId",
                        column: x => x.BagliGorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kararlar_Kullanicilar_SorumluKullaniciId",
                        column: x => x.SorumluKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kararlar_Toplantilar_ToplantiId",
                        column: x => x.ToplantiId,
                        principalTable: "Toplantilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_AtananKullaniciId",
                table: "Gorevler",
                column: "AtananKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kararlar_BagliGorevId",
                table: "Kararlar",
                column: "BagliGorevId");

            migrationBuilder.CreateIndex(
                name: "IX_Kararlar_SorumluKullaniciId",
                table: "Kararlar",
                column: "SorumluKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kararlar_ToplantiId",
                table: "Kararlar",
                column: "ToplantiId");

            migrationBuilder.CreateIndex(
                name: "IX_RaporFavoriler_SorguId",
                table: "RaporFavoriler",
                column: "SorguId");

            migrationBuilder.CreateIndex(
                name: "IX_SetiSorgular_SetiId",
                table: "SetiSorgular",
                column: "SetiId");

            migrationBuilder.CreateIndex(
                name: "IX_SetiSorgular_SorguId",
                table: "SetiSorgular",
                column: "SorguId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kararlar");

            migrationBuilder.DropTable(
                name: "RaporFavoriler");

            migrationBuilder.DropTable(
                name: "SetiSorgular");

            migrationBuilder.DropTable(
                name: "Gorevler");

            migrationBuilder.DropTable(
                name: "Toplantilar");

            migrationBuilder.DropTable(
                name: "QueryRecordlar");

            migrationBuilder.DropTable(
                name: "SorguSetleri");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
