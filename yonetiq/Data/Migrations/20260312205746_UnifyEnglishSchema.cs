using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyEnglishSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "SorguSetleri");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QueryRecordlar",
                table: "QueryRecordlar");

            migrationBuilder.DropColumn(
                name: "KullanimAmaci",
                table: "QueryRecordlar");

            migrationBuilder.RenameTable(
                name: "QueryRecordlar",
                newName: "QueryRecords");

            migrationBuilder.RenameColumn(
                name: "Sql",
                table: "QueryRecords",
                newName: "SqlContent");

            migrationBuilder.RenameColumn(
                name: "SonucTipi",
                table: "QueryRecords",
                newName: "ResultType");

            migrationBuilder.RenameColumn(
                name: "Renk",
                table: "QueryRecords",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "PivotSutunKolon",
                table: "QueryRecords",
                newName: "PivotValueColumn");

            migrationBuilder.RenameColumn(
                name: "PivotSatirKolon",
                table: "QueryRecords",
                newName: "PivotRowColumn");

            migrationBuilder.RenameColumn(
                name: "PivotDegerKolon",
                table: "QueryRecords",
                newName: "PivotColumnColumn");

            migrationBuilder.RenameColumn(
                name: "GuncellenmeTarih",
                table: "QueryRecords",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Ad",
                table: "QueryRecords",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Aciklama",
                table: "QueryRecords",
                newName: "Description");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "QueryRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UsagePurposeLookupId",
                table: "QueryRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueryRecords",
                table: "QueryRecords",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CalendarSpecialDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeLookupId = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAnnualRecurring = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarSpecialDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    ReceiverUserId = table.Column<int>(type: "int", nullable: true),
                    ReceiverDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    AttachmentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Participants = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuerySets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShowInNavMenu = table.Column<bool>(type: "bit", nullable: false),
                    NavMenuOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuerySets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    SharedByUserId = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    TargetDepartmentLookupId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleLookupId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentLookupId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    WidthMd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetQueries_QueryRecords_QueryId",
                        column: x => x.QueryId,
                        principalTable: "QueryRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SetQueries_QuerySets_SetId",
                        column: x => x.SetId,
                        principalTable: "QuerySets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportFavorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFavorites", x => new { x.UserId, x.QueryId });
                    table.ForeignKey(
                        name: "FK_ReportFavorites_QueryRecords_QueryId",
                        column: x => x.QueryId,
                        principalTable: "QueryRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    AssigneeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssigneeId = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriorityLookupId = table.Column<int>(type: "int", nullable: false),
                    StatusLookupId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItems_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    AssigneeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssigneeId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusLookupId = table.Column<int>(type: "int", nullable: false),
                    IsConvertedToTask = table.Column<bool>(type: "bit", nullable: false),
                    LinkedTaskId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decisions_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Decisions_TaskItems_LinkedTaskId",
                        column: x => x.LinkedTaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Decisions_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_AssigneeId",
                table: "Decisions",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_LinkedTaskId",
                table: "Decisions",
                column: "LinkedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_MeetingId",
                table: "Decisions",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFavorites_QueryId",
                table: "ReportFavorites",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_SetQueries_QueryId",
                table: "SetQueries",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_SetQueries_SetId",
                table: "SetQueries",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_AssigneeId",
                table: "TaskItems",
                column: "AssigneeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarSpecialDays");

            migrationBuilder.DropTable(
                name: "CommunicationMessages");

            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "Lookups");

            migrationBuilder.DropTable(
                name: "ReportFavorites");

            migrationBuilder.DropTable(
                name: "ReportShares");

            migrationBuilder.DropTable(
                name: "SetQueries");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "QuerySets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QueryRecords",
                table: "QueryRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "QueryRecords");

            migrationBuilder.DropColumn(
                name: "UsagePurposeLookupId",
                table: "QueryRecords");

            migrationBuilder.RenameTable(
                name: "QueryRecords",
                newName: "QueryRecordlar");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "QueryRecordlar",
                newName: "GuncellenmeTarih");

            migrationBuilder.RenameColumn(
                name: "SqlContent",
                table: "QueryRecordlar",
                newName: "Sql");

            migrationBuilder.RenameColumn(
                name: "ResultType",
                table: "QueryRecordlar",
                newName: "SonucTipi");

            migrationBuilder.RenameColumn(
                name: "PivotValueColumn",
                table: "QueryRecordlar",
                newName: "PivotSutunKolon");

            migrationBuilder.RenameColumn(
                name: "PivotRowColumn",
                table: "QueryRecordlar",
                newName: "PivotSatirKolon");

            migrationBuilder.RenameColumn(
                name: "PivotColumnColumn",
                table: "QueryRecordlar",
                newName: "PivotDegerKolon");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "QueryRecordlar",
                newName: "Ad");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "QueryRecordlar",
                newName: "Aciklama");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "QueryRecordlar",
                newName: "Renk");

            migrationBuilder.AddColumn<string>(
                name: "KullanimAmaci",
                table: "QueryRecordlar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueryRecordlar",
                table: "QueryRecordlar",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SifreHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SifreTuzu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SorguSetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ikon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NavMenuGoster = table.Column<bool>(type: "bit", nullable: false),
                    NavMenuSira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                    Gundem = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Katilimcilar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notlar = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    OlusturmaTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToplantiLinki = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Yer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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
                    Aciklama = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Atanan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AtananKullaniciId = table.Column<int>(type: "int", nullable: true),
                    AtanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Oncelik = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                    GenislikMd = table.Column<int>(type: "int", nullable: false),
                    SetiId = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    SorguId = table.Column<int>(type: "int", nullable: false)
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
                    BagliGorevId = table.Column<int>(type: "int", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GoreveDonustuMu = table.Column<bool>(type: "bit", nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(MAX)", nullable: false),
                    Sorumlu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SorumluKullaniciId = table.Column<int>(type: "int", nullable: true),
                    ToplantiId = table.Column<int>(type: "int", nullable: false)
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
    }
}
