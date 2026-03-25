using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;

namespace YonetIQ.Data;

/// <summary>
/// Başlangıç verilerinin (Initial Data) tohumlanmasını yöneten partial sınıf.
/// </summary>
public static partial class SeedData
{
    private static async Task SeedLookupsAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Lookups");
        if (count > 0) return;

        await conn.ExecuteAsync(@"
            INSERT INTO Lookups ([Group], Value, IsActive, CreatedAt) VALUES (@Group, @Value, 1, @CreatedAt)",
            new[]
            {
                new { Group = "Role", Value = "Genel Müdür", CreatedAt = DateTime.UtcNow },
                new { Group = "Role", Value = "Direktör", CreatedAt = DateTime.UtcNow },
                new { Group = "Role", Value = "Müdür", CreatedAt = DateTime.UtcNow },
                new { Group = "Role", Value = "Personel", CreatedAt = DateTime.UtcNow },

                new { Group = "Department", Value = "Yönetim", CreatedAt = DateTime.UtcNow },
                new { Group = "Department", Value = "Finans", CreatedAt = DateTime.UtcNow },
                new { Group = "Department", Value = "Satış", CreatedAt = DateTime.UtcNow },
                new { Group = "Department", Value = "IT", CreatedAt = DateTime.UtcNow },
                new { Group = "Department", Value = "Operasyon", CreatedAt = DateTime.UtcNow },
                new { Group = "Department", Value = "İnsan Kaynakları", CreatedAt = DateTime.UtcNow },

                new { Group = "TaskStatus", Value = "Bekliyor", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskStatus", Value = "Devam", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskStatus", Value = "Tamamlandi", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskStatus", Value = "Iptal", CreatedAt = DateTime.UtcNow },

                new { Group = "TaskPriority", Value = "Dusuk", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskPriority", Value = "Normal", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskPriority", Value = "Yuksek", CreatedAt = DateTime.UtcNow },
                new { Group = "TaskPriority", Value = "Kritik", CreatedAt = DateTime.UtcNow },

                new { Group = "DecisionStatus", Value = "Bekliyor", CreatedAt = DateTime.UtcNow },
                new { Group = "DecisionStatus", Value = "Devam", CreatedAt = DateTime.UtcNow },
                new { Group = "DecisionStatus", Value = "Tamamlandi", CreatedAt = DateTime.UtcNow },
                new { Group = "DecisionStatus", Value = "Iptal", CreatedAt = DateTime.UtcNow },
                
                new { Group = "SpecialDayType", Value = "Resmi Tatil", CreatedAt = DateTime.UtcNow },
                new { Group = "SpecialDayType", Value = "Ozel Gun", CreatedAt = DateTime.UtcNow },
                new { Group = "SpecialDayType", Value = "Sirket Izin Gunu", CreatedAt = DateTime.UtcNow },

                new { Group = "QueryUsage", Value = "Dashboard", CreatedAt = DateTime.UtcNow },
                new { Group = "QueryUsage", Value = "Report", CreatedAt = DateTime.UtcNow },
                new { Group = "QueryUsage", Value = "Both", CreatedAt = DateTime.UtcNow },

                // Görselleştirme tipleri (ResultType) — DB'den yüklenir, hardcoded enum yoktur
                new { Group = "ResultType", Value = "Table", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "PivotTable", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "KpiCard", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "MultiKpi", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "BarChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "LineChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "AreaChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "PieChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "DonutChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "HeatmapChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "ScatterChart", CreatedAt = DateTime.UtcNow },
                new { Group = "ResultType", Value = "RadarChart", CreatedAt = DateTime.UtcNow },

                // Set tipleri (SetType) — DB'den yüklenir, hardcoded enum yoktur
                new { Group = "SetType", Value = "Dashboard", CreatedAt = DateTime.UtcNow },
                new { Group = "SetType", Value = "KpiPanel", CreatedAt = DateTime.UtcNow },
                new { Group = "SetType", Value = "Report", CreatedAt = DateTime.UtcNow },
                new { Group = "SetType", Value = "Custom", CreatedAt = DateTime.UtcNow }
            });
    }

    private static async Task SeedUsersAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        if (count > 0) return;

        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>("SELECT Id, [Group], Value FROM Lookups")).ToList();
        int GetLookupId(string group, string value) => lookups.FirstOrDefault(t => t.Group == group && t.Value == value).Id;

        await conn.ExecuteAsync(@"
            INSERT INTO Users (FullName, Email, RoleLookupId, IsActive, CreatedAt, DepartmentLookupId)
            VALUES (@FullName, @Email, @RoleLookupId, 1, @CreatedAt, @DepartmentLookupId)",
            new[]
            {
                new { FullName = "Genel Müdür", Email = "gm@YonetIQ.local", RoleLookupId = GetLookupId("Role", "Genel Müdür"), CreatedAt = DateTime.UtcNow, DepartmentLookupId = GetLookupId("Department", "Yonetim") },
                new { FullName = "Finans Direktörü", Email = "finans@YonetIQ.local", RoleLookupId = GetLookupId("Role", "Direktör"), CreatedAt = DateTime.UtcNow, DepartmentLookupId = GetLookupId("Department", "Finans") },
                new { FullName = "Satış Direktörü", Email = "satis@YonetIQ.local", RoleLookupId = GetLookupId("Role", "Direktör"), CreatedAt = DateTime.UtcNow, DepartmentLookupId = GetLookupId("Department", "Satış") },
                new { FullName = "IT Müdürü", Email = "it@YonetIQ.local", RoleLookupId = GetLookupId("Role", "Müdür"), CreatedAt = DateTime.UtcNow, DepartmentLookupId = GetLookupId("Department", "IT") },
                new { FullName = "Operasyon Müdürü", Email = "operasyon@YonetIQ.local", RoleLookupId = GetLookupId("Role", "Müdür"), CreatedAt = DateTime.UtcNow, DepartmentLookupId = GetLookupId("Department", "Operasyon") }
            });
    }

    /// <summary>
    /// PasswordHash'i NULL olan kullanıcılara varsayılan şifre atar (Admin1234!).
    /// AuthService.ComputeHash ile aynı HMACSHA256 mekanizması kullanılır.
    /// </summary>
    private static async Task EnsureDefaultPasswordsAsync(SqlConnection conn)
    {
        var nullPassUsers = await conn.QueryAsync<int>(
            "SELECT Id FROM Users WHERE PasswordHash IS NULL OR PasswordSalt IS NULL");

        if (!nullPassUsers.Any()) return;

        const string defaultPassword = "Admin1234!";
        var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var salt = Convert.ToBase64String(saltBytes);

        // AuthService.ComputeHash ile birebir aynı: HMACSHA256.HashData(key=UTF8(salt), data=UTF8(password))
        var key = System.Text.Encoding.UTF8.GetBytes(salt);
        var data = System.Text.Encoding.UTF8.GetBytes(defaultPassword);
        var hmacHash = System.Security.Cryptography.HMACSHA256.HashData(key, data);
        var hash = Convert.ToBase64String(hmacHash);

        foreach (var userId in nullPassUsers)
        {
            await conn.ExecuteAsync(
                "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = @Salt WHERE Id = @Id",
                new { Hash = hash, Salt = salt, Id = userId });
        }
    }

    private static async Task SeedMeetingsAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Meetings");
        if (count > 0) return;

        const string insertMeeting = @"
            INSERT INTO Meetings (Title, MeetingDate, Location, MeetingLink, Participants, Agenda, Notes, CreatedAt)
            OUTPUT INSERTED.Id VALUES (@Title, @MeetingDate, @Location, @MeetingLink, @Participants, @Agenda, @Notes, @CreatedAt)";

        var m1Id = await conn.ExecuteScalarAsync<int>(insertMeeting, new { Title = "Q1 2026 Strateji", MeetingDate = new DateTime(2026, 1, 15), Location = "Ofis", CreatedAt = DateTime.UtcNow });
        var m2Id = await conn.ExecuteScalarAsync<int>(insertMeeting, new { Title = "Operasyonel Verimlilik", MeetingDate = new DateTime(2026, 2, 10), Location = "Zoom", CreatedAt = DateTime.UtcNow });

        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>("SELECT Id, [Group], Value FROM Lookups")).ToList();
        int GetId(string g, string v) => lookups.FirstOrDefault(t => t.Group == g && t.Value == v).Id;

        await conn.ExecuteAsync(@"
            INSERT INTO Decisions (MeetingId, Content, AssigneeName, StatusLookupId, PriorityLookupId, CreatedAt)
            VALUES (@MeetingId, @Content, @AssigneeName, @StatusLookupId, @PriorityLookupId, @CreatedAt)",
            new[]
            {
                new { MeetingId = m1Id, Content = "IT altyapı yatırımı", AssigneeName = "IT Muduru", StatusLookupId = GetId("DecisionStatus", "Devam"), PriorityLookupId = GetId("TaskPriority", "Yuksek"), CreatedAt = DateTime.UtcNow },
                new { MeetingId = m2Id, Content = "Uretim hatasi orani", AssigneeName = "Operasyon Muduru", StatusLookupId = GetId("DecisionStatus", "Bekliyor"), PriorityLookupId = GetId("TaskPriority", "Normal"), CreatedAt = DateTime.UtcNow }
            });
    }

    private static async Task SeedTasksAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TaskItems");
        if (count > 0) return;

        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>("SELECT Id, [Group], Value FROM Lookups")).ToList();
        int GetId(string g, string v) => lookups.FirstOrDefault(t => t.Group == g && t.Value == v).Id;

        await conn.ExecuteAsync(@"
            INSERT INTO TaskItems (Title, Description, AssigneeName, AssignedAt, DueDate, PriorityLookupId, StatusLookupId, CreatedAt)
            VALUES (@Title, @Description, @AssigneeName, @AssignedAt, @DueDate, @PriorityLookupId, @StatusLookupId, @CreatedAt)",
            new[]
            {
                new { Title = "Yillik Butce Hazirligi", Description = "Finansal planlama", AssigneeName = "Finans Direktoru", AssignedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddMonths(1), PriorityLookupId = GetId("TaskPriority", "Yuksek"), StatusLookupId = GetId("TaskStatus", "Devam"), CreatedAt = DateTime.UtcNow },
                new { Title = "Satis Raporu", Description = "Haftalik takip", AssigneeName = "Satis Direktoru", AssignedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(7), PriorityLookupId = GetId("TaskPriority", "Normal"), StatusLookupId = GetId("TaskStatus", "Bekliyor"), CreatedAt = DateTime.UtcNow }
            });
    }

    private static async Task SeedQueriesAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM QueryRecords");
        if (count > 0) return;

        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>("SELECT Id, [Group], Value FROM Lookups")).ToList();
        int GetId(string g, string v) => lookups.FirstOrDefault(t => t.Group == g && t.Value == v).Id;

        await conn.ExecuteAsync(@"
            INSERT INTO QueryRecords (Name, Description, SqlContent, ResultType, UsagePurposeLookupId, CreatedAt)
            VALUES (@Name, @Description, @SqlContent, @ResultType, @UsagePurpose, @At)",
            new[]
            {
                new { Name = "Bekleyen Kararlar", Description = "KPI", SqlContent = "SELECT COUNT(*) FROM Decisions WHERE StatusLookupId = 8", ResultType = "KpiCard", UsagePurpose = GetId("QueryUsage", "Dashboard"), At = DateTime.UtcNow }
            });
    }

    private static async Task SeedQuerySetsAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM QuerySets");
        if (count > 0) return;

        await conn.ExecuteAsync(@"
            INSERT INTO QuerySets (Name, Description, Type, Icon, ShowInNavMenu, CreatedAt)
            VALUES (@Name, @Description, @Type, @Icon, 1, @At)",
            new { Name = "Ana Dashboard", Description = "Ozet", Type = "Dashboard", Icon = "Dashboard", At = DateTime.UtcNow });
    }

    private static async Task SeedCalendarSpecialDaysAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CalendarSpecialDays");
        if (count > 0) return;

        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>("SELECT Id, [Group], Value FROM Lookups")).ToList();
        int GetId(string g, string v) => lookups.FirstOrDefault(t => t.Group == g && t.Value == v).Id;

        await conn.ExecuteAsync(@"
            INSERT INTO CalendarSpecialDays (Date, Title, TypeLookupId, CreatedAt)
            VALUES (@Date, @Title, @Type, @At)",
            new { Date = new DateTime(DateTime.Today.Year, 1, 1), Title = "Yilbasi", Type = GetId("SpecialDayType", "Resmi Tatil"), At = DateTime.UtcNow });
    }

    private static async Task SeedMessagesAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CommunicationMessages");
        if (count > 0) return;

        await conn.ExecuteAsync(@"
            INSERT INTO CommunicationMessages (Body, Type, CreatedAt)
            VALUES (@Body, @Type, @At)",
            new { Body = "Sisteme hos geldiniz.", Type = 2, At = DateTime.UtcNow });
    }

    private static async Task SeedReportFavoritesAsync(SqlConnection conn)
    {
        // Opsiyonel: Boş bırakılabilir veya ekleme yapılabilir.
    }

    /// <summary>
    /// Tüm modüller için kapsamlı test verisi ekler.
    /// Guard: TaskItems tablosunda 10+ kayıt varsa tekrar çalışmaz (idempotent).
    /// </summary>
    private static async Task SeedComprehensiveTestDataAsync(SqlConnection conn)
    {
        var taskCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TaskItems");
        if (taskCount >= 10) return;

        var now = DateTime.UtcNow;

        // --- Lookup helper ---
        var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>(
            "SELECT Id, [Group], Value FROM Lookups")).ToList();
        int L(string g, string v) => lookups.FirstOrDefault(t => t.Group == g && t.Value == v).Id;

        // --- User ID helper ---
        var users = (await conn.QueryAsync<(int Id, string FullName)>(
            "SELECT Id, FullName FROM Users")).ToList();
        int U(string namePart) => users.FirstOrDefault(u => u.FullName.Contains(namePart)).Id;
        string UN(string namePart) => users.FirstOrDefault(u => u.FullName.Contains(namePart)).FullName;

        var gmId   = U("Genel");
        var finId  = U("Finans");
        var satId  = U("Satış");
        var itId   = U("IT");
        var opId   = U("Operasyon");

        // ==================== TASKS (15 adet) ====================
        var taskInsert = @"
            INSERT INTO TaskItems (Title, Description, AssigneeName, AssigneeId, AssignedAt, DueDate, PriorityLookupId, StatusLookupId, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Description, @AssigneeName, @AssigneeId, @AssignedAt, @DueDate, @PriorityLookupId, @StatusLookupId, @CreatedAt)";

        var taskIds = new List<int>();
        var tasks = new[]
        {
            new { Title = "ERP entegrasyonu analiz raporu", Description = "Mevcut ERP sistemiyle YonetIQ entegrasyonunun fizibilite analizi yapilacak", AssigneeName = UN("IT"), AssigneeId = (int?)itId, AssignedAt = now.AddDays(-14), DueDate = (DateTime?)now.AddDays(-3), PriorityLookupId = L("TaskPriority","Yuksek"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-14) },
            new { Title = "Q1 satis hedefleri degerlendirme", Description = "Ilk ceyrek satis performansinin hedeflerle karsilastirilmasi", AssigneeName = UN("Satış"), AssigneeId = (int?)satId, AssignedAt = now.AddDays(-10), DueDate = (DateTime?)now.AddDays(-1), PriorityLookupId = L("TaskPriority","Kritik"), StatusLookupId = L("TaskStatus","Bekliyor"), CreatedAt = now.AddDays(-10) },
            new { Title = "Yeni personel oryantasyon programi", Description = "Nisan ayi itibaryla baslayacak personel icin oryantasyon plani hazirligi", AssigneeName = UN("Operasyon"), AssigneeId = (int?)opId, AssignedAt = now.AddDays(-7), DueDate = (DateTime?)now.AddDays(5), PriorityLookupId = L("TaskPriority","Normal"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-7) },
            new { Title = "Sunucu yedekleme politikasi guncelleme", Description = "Haftalik yedekleme stratejisinin gozden gecirilmesi", AssigneeName = UN("IT"), AssigneeId = (int?)itId, AssignedAt = now.AddDays(-21), DueDate = (DateTime?)now.AddDays(-10), PriorityLookupId = L("TaskPriority","Yuksek"), StatusLookupId = L("TaskStatus","Tamamlandi"), CreatedAt = now.AddDays(-21) },
            new { Title = "2026 butce revizyonu", Description = "Ikinci ceyrek butce tahminlerinin guncellenmesi", AssigneeName = UN("Finans"), AssigneeId = (int?)finId, AssignedAt = now.AddDays(-5), DueDate = (DateTime?)now.AddDays(10), PriorityLookupId = L("TaskPriority","Kritik"), StatusLookupId = L("TaskStatus","Bekliyor"), CreatedAt = now.AddDays(-5) },
            new { Title = "Musteri memnuniyet anketi sonuclari", Description = "Anket verilerinin analiz edilip rapora donusturulmesi", AssigneeName = UN("Satış"), AssigneeId = (int?)satId, AssignedAt = now.AddDays(-12), DueDate = (DateTime?)now.AddDays(3), PriorityLookupId = L("TaskPriority","Normal"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-12) },
            new { Title = "Depo sayim raporu hazirlama", Description = "Mart sonu depo envanter sayimi ve raporlamasi", AssigneeName = UN("Operasyon"), AssigneeId = (int?)opId, AssignedAt = now.AddDays(-3), DueDate = (DateTime?)now, PriorityLookupId = L("TaskPriority","Yuksek"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-3) },
            new { Title = "Yazilim lisans yenilemesi", Description = "Microsoft 365 ve Adobe lisanslarinin yenileme sureci", AssigneeName = UN("IT"), AssigneeId = (int?)itId, AssignedAt = now.AddDays(-30), DueDate = (DateTime?)now.AddDays(-15), PriorityLookupId = L("TaskPriority","Normal"), StatusLookupId = L("TaskStatus","Tamamlandi"), CreatedAt = now.AddDays(-30) },
            new { Title = "Is sagligi ve guvenligi egitimi", Description = "Yillik zorunlu ISG egitiminin planlanmasi", AssigneeName = UN("Operasyon"), AssigneeId = (int?)opId, AssignedAt = now.AddDays(-2), DueDate = (DateTime?)now.AddDays(14), PriorityLookupId = L("TaskPriority","Dusuk"), StatusLookupId = L("TaskStatus","Bekliyor"), CreatedAt = now.AddDays(-2) },
            new { Title = "Nakit akisi projeksiyonu", Description = "Nisan-Haziran donemi nakit akis tahmini", AssigneeName = UN("Finans"), AssigneeId = (int?)finId, AssignedAt = now.AddDays(-8), DueDate = (DateTime?)now.AddDays(7), PriorityLookupId = L("TaskPriority","Yuksek"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-8) },
            new { Title = "CRM veri temizligi", Description = "Duplike ve eski musteri kayitlarinin temizlenmesi", AssigneeName = UN("Satış"), AssigneeId = (int?)satId, AssignedAt = now.AddDays(-20), DueDate = (DateTime?)now.AddDays(-5), PriorityLookupId = L("TaskPriority","Dusuk"), StatusLookupId = L("TaskStatus","Tamamlandi"), CreatedAt = now.AddDays(-20) },
            new { Title = "Yonetim kurulu sunumu hazirlama", Description = "Q1 ozet performans sunumu ve stateji onerileri", AssigneeName = UN("Genel"), AssigneeId = (int?)gmId, AssignedAt = now.AddDays(-4), DueDate = (DateTime?)now.AddDays(2), PriorityLookupId = L("TaskPriority","Kritik"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-4) },
            new { Title = "Tedarikci sozlesme yenileme", Description = "Ana tedarikci ile yillik sozlesme muzakeresi", AssigneeName = UN("Operasyon"), AssigneeId = (int?)opId, AssignedAt = now.AddDays(-15), DueDate = (DateTime?)now.AddDays(20), PriorityLookupId = L("TaskPriority","Normal"), StatusLookupId = L("TaskStatus","Bekliyor"), CreatedAt = now.AddDays(-15) },
            new { Title = "Ag guvenligi denetimi", Description = "Yillik penetrasyon testi ve guvenlik auditi", AssigneeName = UN("IT"), AssigneeId = (int?)itId, AssignedAt = now.AddDays(-1), DueDate = (DateTime?)now.AddDays(30), PriorityLookupId = L("TaskPriority","Yuksek"), StatusLookupId = L("TaskStatus","Bekliyor"), CreatedAt = now.AddDays(-1) },
            new { Title = "Satis ekibi performans degerlendirmesi", Description = "Bireysel hedef gerceklesme oranlari ve geri bildirim", AssigneeName = UN("Satış"), AssigneeId = (int?)satId, AssignedAt = now.AddDays(-6), DueDate = (DateTime?)now.AddDays(1), PriorityLookupId = L("TaskPriority","Normal"), StatusLookupId = L("TaskStatus","Devam"), CreatedAt = now.AddDays(-6) },
        };

        foreach (var t in tasks)
        {
            var id = await conn.ExecuteScalarAsync<int>(taskInsert, t);
            taskIds.Add(id);
        }

        // ==================== MEETINGS (6 adet) ====================
        var meetingInsert = @"
            INSERT INTO Meetings (Title, MeetingDate, Location, MeetingLink, Participants, Agenda, Notes, RecurrenceType, RecurrenceInterval, RecurrenceEndDate, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @MeetingDate, @Location, @MeetingLink, @Participants, @Agenda, @Notes, @RecurrenceType, @RecurrenceInterval, @RecurrenceEndDate, @CreatedAt)";

        var m1 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "Haftalik Yonetim Toplantisi", MeetingDate = now.AddDays(-7),
            Location = "Yonetim Kati Toplanti Salonu", MeetingLink = (string?)null,
            Participants = "Genel Mudur, Finans Direktoru, Satis Direktoru, IT Muduru, Operasyon Muduru",
            Agenda = "1. Haftalik performans ozeti\n2. Acil konular\n3. Gelecek hafta plani",
            Notes = "Satis rakamlari beklentilerin ustunde. IT ekibi ERP entegrasyon analizini tamamlamak uzere.",
            RecurrenceType = "Weekly", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)now.AddMonths(6),
            CreatedAt = now.AddDays(-7) });

        var m2 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "Butce Planlama Toplantisi", MeetingDate = now.AddDays(-14),
            Location = "Finans Departmani", MeetingLink = "https://teams.microsoft.com/l/meetup-join/budget2026",
            Participants = "Genel Mudur, Finans Direktoru",
            Agenda = "1. Q1 butce gerceklesme\n2. Q2 revizyon onerileri\n3. Yatirim butcesi",
            Notes = "Q1 butce %92 gerceklesti. Q2 icin %5 artis ongorulmekte.",
            RecurrenceType = "None", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)null,
            CreatedAt = now.AddDays(-14) });

        var m3 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "IT Altyapi Degerlendirmesi", MeetingDate = now.AddDays(-5),
            Location = "IT Ofisi", MeetingLink = "https://zoom.us/j/123456789",
            Participants = "IT Muduru, Operasyon Muduru, Genel Mudur",
            Agenda = "1. Sunucu durumu\n2. Guvenlik guncellemeleri\n3. Yeni yazilim ihtiyaclari",
            Notes = "Sunucu yedekleme politikasi guncellendi. Ag guvenligi denetimi planlanacak.",
            RecurrenceType = "None", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)null,
            CreatedAt = now.AddDays(-5) });

        var m4 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "Satis Stratejisi Toplantisi", MeetingDate = now.AddDays(2),
            Location = "Toplanti Odasi B", MeetingLink = (string?)null,
            Participants = "Satis Direktoru, Genel Mudur, Finans Direktoru",
            Agenda = "1. Q2 satis hedefleri\n2. Yeni pazar arastirmasi\n3. Kampanya planlamasi",
            Notes = "",
            RecurrenceType = "None", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)null,
            CreatedAt = now.AddDays(-1) });

        var m5 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "Aylik Operasyon Degerlendirmesi", MeetingDate = now.AddDays(7),
            Location = "Ana Toplanti Salonu", MeetingLink = "https://teams.microsoft.com/l/meetup-join/ops-monthly",
            Participants = "Operasyon Muduru, Genel Mudur, IT Muduru",
            Agenda = "1. Uretim verimliligi\n2. Tedarik zinciri performansi\n3. ISG guncellemeleri",
            Notes = "",
            RecurrenceType = "Monthly", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)now.AddMonths(12),
            CreatedAt = now.AddDays(-2) });

        var m6 = await conn.ExecuteScalarAsync<int>(meetingInsert, new {
            Title = "Yonetim Kurulu Sunumu Hazirligi", MeetingDate = now.AddDays(-2),
            Location = "Yonetim Kati", MeetingLink = (string?)null,
            Participants = "Genel Mudur, Finans Direktoru, Satis Direktoru",
            Agenda = "1. Sunum icerigi\n2. Finansal tablo kontrolu\n3. Stratejik hedefler",
            Notes = "Sunum taslagi hazirlandi. Finansal tablolar nihai hale getirilecek.",
            RecurrenceType = "None", RecurrenceInterval = 1, RecurrenceEndDate = (DateTime?)null,
            CreatedAt = now.AddDays(-4) });

        // ==================== DECISIONS (8 adet) ====================
        var decisionInsert = @"
            INSERT INTO Decisions (MeetingId, Content, AssigneeName, AssigneeId, DueDate, StatusLookupId, PriorityLookupId, IsConvertedToTask, LinkedTaskId, CreatedAt)
            VALUES (@MeetingId, @Content, @AssigneeName, @AssigneeId, @DueDate, @StatusLookupId, @PriorityLookupId, @IsConvertedToTask, @LinkedTaskId, @CreatedAt)";

        await conn.ExecuteAsync(decisionInsert, new[] {
            new { MeetingId = m1, Content = "ERP entegrasyon projesinin Nisan sonuna kadar tamamlanmasi", AssigneeName = UN("IT"), AssigneeId = (int?)itId, DueDate = (DateTime?)now.AddDays(30), StatusLookupId = L("DecisionStatus","Devam"), PriorityLookupId = (int?)L("TaskPriority","Yuksek"), IsConvertedToTask = true, LinkedTaskId = (int?)taskIds[0], CreatedAt = now.AddDays(-7) },
            new { MeetingId = m1, Content = "Haftalik satis raporlama formatinin yenilenmesi", AssigneeName = UN("Satış"), AssigneeId = (int?)satId, DueDate = (DateTime?)now.AddDays(14), StatusLookupId = L("DecisionStatus","Bekliyor"), PriorityLookupId = (int?)L("TaskPriority","Normal"), IsConvertedToTask = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-7) },
            new { MeetingId = m2, Content = "Q2 butce tavaninin %5 arttirilmasi", AssigneeName = UN("Finans"), AssigneeId = (int?)finId, DueDate = (DateTime?)now.AddDays(10), StatusLookupId = L("DecisionStatus","Devam"), PriorityLookupId = (int?)L("TaskPriority","Kritik"), IsConvertedToTask = true, LinkedTaskId = (int?)taskIds[4], CreatedAt = now.AddDays(-14) },
            new { MeetingId = m2, Content = "Yeni yatirim kalemleri icin fizibilite raporu hazirlanmasi", AssigneeName = UN("Finans"), AssigneeId = (int?)finId, DueDate = (DateTime?)now.AddDays(21), StatusLookupId = L("DecisionStatus","Bekliyor"), PriorityLookupId = (int?)L("TaskPriority","Normal"), IsConvertedToTask = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-14) },
            new { MeetingId = m3, Content = "Sunucu yedekleme sikliginin gunluge cikartilmasi", AssigneeName = UN("IT"), AssigneeId = (int?)itId, DueDate = (DateTime?)now.AddDays(-10), StatusLookupId = L("DecisionStatus","Tamamlandi"), PriorityLookupId = (int?)L("TaskPriority","Yuksek"), IsConvertedToTask = true, LinkedTaskId = (int?)taskIds[3], CreatedAt = now.AddDays(-5) },
            new { MeetingId = m3, Content = "Ag guvenligi denetimi icin dis firma ile gorusme", AssigneeName = UN("IT"), AssigneeId = (int?)itId, DueDate = (DateTime?)now.AddDays(30), StatusLookupId = L("DecisionStatus","Bekliyor"), PriorityLookupId = (int?)L("TaskPriority","Yuksek"), IsConvertedToTask = true, LinkedTaskId = (int?)taskIds[13], CreatedAt = now.AddDays(-5) },
            new { MeetingId = m6, Content = "Finansal tablolarin pazartesi gunu teslim edilmesi", AssigneeName = UN("Finans"), AssigneeId = (int?)finId, DueDate = (DateTime?)now.AddDays(1), StatusLookupId = L("DecisionStatus","Devam"), PriorityLookupId = (int?)L("TaskPriority","Kritik"), IsConvertedToTask = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-2) },
            new { MeetingId = m6, Content = "Stratejik hedefler bolumunun Genel Mudur tarafindan yazilmasi", AssigneeName = UN("Genel"), AssigneeId = (int?)gmId, DueDate = (DateTime?)now.AddDays(2), StatusLookupId = L("DecisionStatus","Devam"), PriorityLookupId = (int?)L("TaskPriority","Yuksek"), IsConvertedToTask = true, LinkedTaskId = (int?)taskIds[11], CreatedAt = now.AddDays(-2) },
        });

        // ==================== PERSONAL NOTES (10 adet) ====================
        await conn.ExecuteAsync(@"
            INSERT INTO PersonalNotes (UserId, Title, Content, Tags, ReminderAt, IsReminderDismissed, LinkedTaskId, CreatedAt)
            VALUES (@UserId, @Title, @Content, @Tags, @ReminderAt, @IsReminderDismissed, @LinkedTaskId, @CreatedAt)",
            new[] {
                new { UserId = gmId, Title = "Yonetim kurulu notlari", Content = "Stratejik hedefler: dijitallesme, pazar genislemesi, maliyet optimizasyonu", Tags = "strateji,yonetim", ReminderAt = (DateTime?)now.AddDays(2), IsReminderDismissed = false, LinkedTaskId = (int?)taskIds[11], CreatedAt = now.AddDays(-3) },
                new { UserId = gmId, Title = "Personel degerlendirme takvimi", Content = "Nisan sonu tum departman mudurleriyle bireysel gorusmeler yapilacak", Tags = "ik,degerlendirme", ReminderAt = (DateTime?)now.AddDays(10), IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-5) },
                new { UserId = finId, Title = "Vergi odeme hatirlatmasi", Content = "KDV beyannamesi 24 Mart son gun, kurumlar vergisi 30 Nisan", Tags = "vergi,finans,onemli", ReminderAt = (DateTime?)now.AddDays(1), IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-7) },
                new { UserId = finId, Title = "Bankacilik islemleri", Content = "Garanti ve Is Bankasi doviz hesaplari icin kur karsilastirmasi yapilacak", Tags = "banka,finans", ReminderAt = (DateTime?)null, IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-10) },
                new { UserId = satId, Title = "Yeni musteri adaylari", Content = "Fuar sonrasi toplanan 15 kartvizit icin CRM giris yapilacak", Tags = "musteri,satis,crm", ReminderAt = (DateTime?)now.AddDays(3), IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-4) },
                new { UserId = satId, Title = "Kampanya fikirleri", Content = "Yaz donemi icin erken alis kampanyasi ve sadakat programi onerileri", Tags = "kampanya,satis", ReminderAt = (DateTime?)null, IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-8) },
                new { UserId = itId, Title = "Sunucu bakim plani", Content = "Hafta sonu planlanan yedekleme sunucusu migration islemi. Downtime: 2 saat.", Tags = "sunucu,bakim,it", ReminderAt = (DateTime?)now.AddDays(4), IsReminderDismissed = false, LinkedTaskId = (int?)taskIds[3], CreatedAt = now.AddDays(-2) },
                new { UserId = itId, Title = "Lisans takibi", Content = "Adobe CC 12 kullanici, MS365 E3 50 lisans. Yenileme tarihi: Mayis 2026", Tags = "lisans,it", ReminderAt = (DateTime?)now.AddDays(45), IsReminderDismissed = false, LinkedTaskId = (int?)taskIds[7], CreatedAt = now.AddDays(-15) },
                new { UserId = opId, Title = "Depo duzenleme plani", Content = "A blogu raflarinin yeniden numaralandirilmasi ve barkod sistemi kurulumu", Tags = "depo,operasyon", ReminderAt = (DateTime?)null, IsReminderDismissed = false, LinkedTaskId = (int?)taskIds[6], CreatedAt = now.AddDays(-6) },
                new { UserId = opId, Title = "Tedarikci iletisim bilgileri", Content = "ABC Lojistik: 0212-555-1234, XYZ Malzeme: 0216-555-5678, Hizli Kargo: 0532-555-9012", Tags = "tedarikci,iletisim", ReminderAt = (DateTime?)null, IsReminderDismissed = false, LinkedTaskId = (int?)null, CreatedAt = now.AddDays(-20) },
            });

        // ==================== APPROVAL REQUESTS + STEPS (4 adet) ====================
        var approvalInsert = @"
            INSERT INTO ApprovalRequests (EntityType, EntityId, Title, Description, RequestedBy, RequestedByName, Status, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@EntityType, @EntityId, @Title, @Description, @RequestedBy, @RequestedByName, @Status, @CreatedAt)";

        var ar1 = await conn.ExecuteScalarAsync<int>(approvalInsert, new {
            EntityType = "Budget", EntityId = (int?)null, Title = "Q2 Butce Artis Talebi",
            Description = "Ikinci ceyrek operasyon butcesinin %5 arttirilmasi talebi",
            RequestedBy = finId, RequestedByName = UN("Finans"), Status = "Pending", CreatedAt = now.AddDays(-3) });

        var ar2 = await conn.ExecuteScalarAsync<int>(approvalInsert, new {
            EntityType = "Purchase", EntityId = (int?)null, Title = "Yeni Sunucu Alisim Onay",
            Description = "2 adet Dell PowerEdge sunucu alisimi icin 85.000 TL butce onayi",
            RequestedBy = itId, RequestedByName = UN("IT"), Status = "Approved", CreatedAt = now.AddDays(-10) });

        var ar3 = await conn.ExecuteScalarAsync<int>(approvalInsert, new {
            EntityType = "Travel", EntityId = (int?)null, Title = "Istanbul Fuar Katilim Giderleri",
            Description = "3 gunluk fuar katilimi icin ulasim ve konaklama masraflari",
            RequestedBy = satId, RequestedByName = UN("Satış"), Status = "Rejected", CreatedAt = now.AddDays(-8) });

        var ar4 = await conn.ExecuteScalarAsync<int>(approvalInsert, new {
            EntityType = "Contract", EntityId = (int?)null, Title = "Tedarikci Sozlesme Yenilemesi",
            Description = "ABC Lojistik ile yillik sozlesme yenileme ve fiyat guncelleme",
            RequestedBy = opId, RequestedByName = UN("Operasyon"), Status = "Pending", CreatedAt = now.AddDays(-1) });

        // Approval Steps
        var stepInsert = @"
            INSERT INTO ApprovalSteps (ApprovalRequestId, StepOrder, ApproverId, ApproverName, Status, Comment, ActionAt, CreatedAt)
            VALUES (@ApprovalRequestId, @StepOrder, @ApproverId, @ApproverName, @Status, @Comment, @ActionAt, @CreatedAt)";

        await conn.ExecuteAsync(stepInsert, new[] {
            // AR1: Pending — iki adimli
            new { ApprovalRequestId = ar1, StepOrder = 1, ApproverId = finId, ApproverName = UN("Finans"), Status = "Approved", Comment = (string?)"Butce analizi uygun", ActionAt = (DateTime?)now.AddDays(-2), CreatedAt = now.AddDays(-3) },
            new { ApprovalRequestId = ar1, StepOrder = 2, ApproverId = gmId, ApproverName = UN("Genel"), Status = "Pending", Comment = (string?)"", ActionAt = (DateTime?)null, CreatedAt = now.AddDays(-3) },
            // AR2: Approved — iki adimli
            new { ApprovalRequestId = ar2, StepOrder = 1, ApproverId = opId, ApproverName = UN("Operasyon"), Status = "Approved", Comment = (string?)"Teknik ihtiyac dogrulandi", ActionAt = (DateTime?)now.AddDays(-9), CreatedAt = now.AddDays(-10) },
            new { ApprovalRequestId = ar2, StepOrder = 2, ApproverId = gmId, ApproverName = UN("Genel"), Status = "Approved", Comment = (string?)"Onaylandi", ActionAt = (DateTime?)now.AddDays(-8), CreatedAt = now.AddDays(-10) },
            // AR3: Rejected — uc adimli
            new { ApprovalRequestId = ar3, StepOrder = 1, ApproverId = satId, ApproverName = UN("Satış"), Status = "Approved", Comment = (string?)"Katilim gerekli", ActionAt = (DateTime?)now.AddDays(-7), CreatedAt = now.AddDays(-8) },
            new { ApprovalRequestId = ar3, StepOrder = 2, ApproverId = finId, ApproverName = UN("Finans"), Status = "Rejected", Comment = (string?)"Butce asimi nedeniyle reddedildi", ActionAt = (DateTime?)now.AddDays(-6), CreatedAt = now.AddDays(-8) },
            new { ApprovalRequestId = ar3, StepOrder = 3, ApproverId = gmId, ApproverName = UN("Genel"), Status = "Pending", Comment = (string?)"", ActionAt = (DateTime?)null, CreatedAt = now.AddDays(-8) },
            // AR4: Pending — iki adimli
            new { ApprovalRequestId = ar4, StepOrder = 1, ApproverId = opId, ApproverName = UN("Operasyon"), Status = "Approved", Comment = (string?)"Tedarikci performansi iyi", ActionAt = (DateTime?)now.AddDays(0), CreatedAt = now.AddDays(-1) },
            new { ApprovalRequestId = ar4, StepOrder = 2, ApproverId = gmId, ApproverName = UN("Genel"), Status = "Pending", Comment = (string?)"", ActionAt = (DateTime?)null, CreatedAt = now.AddDays(-1) },
        });

        // ==================== OBJECTIVES + KEY RESULTS ====================
        var objInsert = @"
            INSERT INTO Objectives (Title, Description, OwnerId, OwnerName, Level, Period, Year, Status, Progress, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Description, @OwnerId, @OwnerName, @Level, @Period, @Year, @Status, @Progress, @CreatedAt)";

        var obj1 = await conn.ExecuteScalarAsync<int>(objInsert, new {
            Title = "Dijital donusum hizini artir", Description = "Tum departmanlarda dijital arac kullanimini yayginlastir",
            OwnerId = gmId, OwnerName = UN("Genel"), Level = "Company", Period = "Annual", Year = 2026,
            Status = "Active", Progress = 35.0m, CreatedAt = now.AddDays(-60) });

        var obj2 = await conn.ExecuteScalarAsync<int>(objInsert, new {
            Title = "Satis gelirini %20 artir", Description = "Q1-Q2 doneminde satis gelirlerinde buyume hedefi",
            OwnerId = satId, OwnerName = UN("Satış"), Level = "Department", Period = "Q1", Year = 2026,
            Status = "Active", Progress = 65.0m, CreatedAt = now.AddDays(-80) });

        var obj3 = await conn.ExecuteScalarAsync<int>(objInsert, new {
            Title = "Operasyonel verimliligi iyilestir", Description = "Uretim ve lojistik sureclerinde maliyet dususu",
            OwnerId = opId, OwnerName = UN("Operasyon"), Level = "Department", Period = "Q2", Year = 2026,
            Status = "Active", Progress = 20.0m, CreatedAt = now.AddDays(-30) });

        var obj4 = await conn.ExecuteScalarAsync<int>(objInsert, new {
            Title = "IT altyapisini modernize et", Description = "Sunucu, ag ve yazilim altyapisinin guncellenmesi",
            OwnerId = itId, OwnerName = UN("IT"), Level = "Department", Period = "Annual", Year = 2026,
            Status = "Active", Progress = 45.0m, CreatedAt = now.AddDays(-60) });

        // Key Results
        var krInsert = @"
            INSERT INTO KeyResults (ObjectiveId, Title, MetricType, StartValue, TargetValue, CurrentValue, Unit, OwnerId, OwnerName, LinkedTaskId, Progress, CreatedAt)
            VALUES (@ObjectiveId, @Title, @MetricType, @StartValue, @TargetValue, @CurrentValue, @Unit, @OwnerId, @OwnerName, @LinkedTaskId, @Progress, @CreatedAt)";

        await conn.ExecuteAsync(krInsert, new[] {
            new { ObjectiveId = obj1, Title = "ERP entegrasyonunu tamamla", MetricType = "Percentage", StartValue = 0m, TargetValue = 100m, CurrentValue = 40m, Unit = "%", OwnerId = (int?)itId, OwnerName = UN("IT"), LinkedTaskId = (int?)taskIds[0], Progress = 40.0m, CreatedAt = now.AddDays(-60) },
            new { ObjectiveId = obj1, Title = "Tum departmanlarda dijital raporlama", MetricType = "Number", StartValue = 1m, TargetValue = 6m, CurrentValue = 3m, Unit = "departman", OwnerId = (int?)gmId, OwnerName = UN("Genel"), LinkedTaskId = (int?)null, Progress = 50.0m, CreatedAt = now.AddDays(-60) },
            new { ObjectiveId = obj1, Title = "Kagit kullanimini %50 azalt", MetricType = "Percentage", StartValue = 0m, TargetValue = 50m, CurrentValue = 15m, Unit = "%", OwnerId = (int?)opId, OwnerName = UN("Operasyon"), LinkedTaskId = (int?)null, Progress = 30.0m, CreatedAt = now.AddDays(-60) },

            new { ObjectiveId = obj2, Title = "Yeni musteri adedi", MetricType = "Number", StartValue = 0m, TargetValue = 50m, CurrentValue = 32m, Unit = "musteri", OwnerId = (int?)satId, OwnerName = UN("Satış"), LinkedTaskId = (int?)null, Progress = 64.0m, CreatedAt = now.AddDays(-80) },
            new { ObjectiveId = obj2, Title = "Mevcut musteri buyume orani", MetricType = "Percentage", StartValue = 0m, TargetValue = 15m, CurrentValue = 10m, Unit = "%", OwnerId = (int?)satId, OwnerName = UN("Satış"), LinkedTaskId = (int?)null, Progress = 66.7m, CreatedAt = now.AddDays(-80) },

            new { ObjectiveId = obj3, Title = "Depo islem suresi azaltma", MetricType = "Number", StartValue = 45m, TargetValue = 30m, CurrentValue = 40m, Unit = "dakika", OwnerId = (int?)opId, OwnerName = UN("Operasyon"), LinkedTaskId = (int?)taskIds[6], Progress = 33.3m, CreatedAt = now.AddDays(-30) },
            new { ObjectiveId = obj3, Title = "Lojistik maliyet dususu", MetricType = "Percentage", StartValue = 0m, TargetValue = 10m, CurrentValue = 2m, Unit = "%", OwnerId = (int?)opId, OwnerName = UN("Operasyon"), LinkedTaskId = (int?)null, Progress = 20.0m, CreatedAt = now.AddDays(-30) },

            new { ObjectiveId = obj4, Title = "Sunucu uptime %99.9", MetricType = "Percentage", StartValue = 99.0m, TargetValue = 99.9m, CurrentValue = 99.5m, Unit = "%", OwnerId = (int?)itId, OwnerName = UN("IT"), LinkedTaskId = (int?)taskIds[3], Progress = 55.6m, CreatedAt = now.AddDays(-60) },
            new { ObjectiveId = obj4, Title = "Ag guvenlik skoru", MetricType = "Number", StartValue = 60m, TargetValue = 90m, CurrentValue = 72m, Unit = "puan", OwnerId = (int?)itId, OwnerName = UN("IT"), LinkedTaskId = (int?)taskIds[13], Progress = 40.0m, CreatedAt = now.AddDays(-60) },
        });

        // ==================== CALENDAR SPECIAL DAYS (6 adet) ====================
        var calInsert = @"
            INSERT INTO CalendarSpecialDays (Date, Title, TypeLookupId, Detail, IsAnnualRecurring, CreatedAt)
            VALUES (@Date, @Title, @TypeLookupId, @Detail, @IsAnnualRecurring, @CreatedAt)";

        await conn.ExecuteAsync(calInsert, new[] {
            new { Date = new DateTime(2026, 4, 23), Title = "23 Nisan Ulusal Egemenlik ve Cocuk Bayrami", TypeLookupId = L("SpecialDayType","Resmi Tatil"), Detail = "Resmi tatil", IsAnnualRecurring = true, CreatedAt = now },
            new { Date = new DateTime(2026, 5, 1), Title = "1 Mayis Emek ve Dayanisma Gunu", TypeLookupId = L("SpecialDayType","Resmi Tatil"), Detail = "Resmi tatil", IsAnnualRecurring = true, CreatedAt = now },
            new { Date = new DateTime(2026, 5, 19), Title = "19 Mayis Ataturk'u Anma ve Genclik Bayrami", TypeLookupId = L("SpecialDayType","Resmi Tatil"), Detail = "Resmi tatil", IsAnnualRecurring = true, CreatedAt = now },
            new { Date = new DateTime(2026, 8, 30), Title = "30 Agustos Zafer Bayrami", TypeLookupId = L("SpecialDayType","Resmi Tatil"), Detail = "Resmi tatil", IsAnnualRecurring = true, CreatedAt = now },
            new { Date = new DateTime(2026, 4, 10), Title = "Sirket Kurulis Yildonumu", TypeLookupId = L("SpecialDayType","Ozel Gun"), Detail = "YonetIQ kurulusunun yildonumu kutlamasi", IsAnnualRecurring = true, CreatedAt = now },
            new { Date = new DateTime(2026, 6, 26), Title = "Sirket Piknik Gunu", TypeLookupId = L("SpecialDayType","Sirket Izin Gunu"), Detail = "Yillik sirket piknigi - yarim gun izin", IsAnnualRecurring = false, CreatedAt = now },
        });

        // ==================== MESSAGES (10 adet) ====================
        await conn.ExecuteAsync(@"
            INSERT INTO CommunicationMessages (SenderUserId, ReceiverUserId, Body, Type, IsRead, CreatedAt)
            VALUES (@SenderUserId, @ReceiverUserId, @Body, @Type, @IsRead, @CreatedAt)",
            new[] {
                new { SenderUserId = (int?)gmId, ReceiverUserId = (int?)finId, Body = "Butce revizyonu toplantisi icin hazirlik yapildi mi?", Type = 1, IsRead = true, CreatedAt = now.AddDays(-5) },
                new { SenderUserId = (int?)finId, ReceiverUserId = (int?)gmId, Body = "Evet, Q1 gerceklesme raporu hazir. Toplantida sunarim.", Type = 1, IsRead = true, CreatedAt = now.AddDays(-5) },
                new { SenderUserId = (int?)satId, ReceiverUserId = (int?)gmId, Body = "Fuar sonuclari oldukca olumlu. Detayli raporu yarin gonderirim.", Type = 1, IsRead = true, CreatedAt = now.AddDays(-3) },
                new { SenderUserId = (int?)itId, ReceiverUserId = (int?)opId, Body = "Sunucu bakimi bu hafta sonu yapilacak. Sistem 2 saat erisilemez olabilir.", Type = 1, IsRead = false, CreatedAt = now.AddDays(-2) },
                new { SenderUserId = (int?)gmId, ReceiverUserId = (int?)satId, Body = "Yeni pazar arastirmasi sonuclarini bekliyorum. Toplantidan once gonderin.", Type = 1, IsRead = false, CreatedAt = now.AddDays(-1) },
                new { SenderUserId = (int?)opId, ReceiverUserId = (int?)gmId, Body = "Depo sayim raporu hazirligi devam ediyor, yarin tamamlanacak.", Type = 1, IsRead = true, CreatedAt = now.AddDays(-1) },
                new { SenderUserId = (int?)finId, ReceiverUserId = (int?)satId, Body = "Satis komisyon tablosu guncellendi. Lutfen kontrol edin.", Type = 1, IsRead = false, CreatedAt = now.AddHours(-12) },
                new { SenderUserId = (int?)itId, ReceiverUserId = (int?)gmId, Body = "Ag guvenligi denetimi icin 3 firmadan teklif aldik. Karsilastirma tablosunu gonderdim.", Type = 1, IsRead = false, CreatedAt = now.AddHours(-6) },
                new { SenderUserId = (int?)gmId, ReceiverUserId = (int?)itId, Body = "Teklif karsilastirmasini inceledim. Pazartesi goruselim.", Type = 1, IsRead = true, CreatedAt = now.AddHours(-3) },
                new { SenderUserId = (int?)satId, ReceiverUserId = (int?)finId, Body = "Bu ayin satis rakamlari ektedir. KDV dahil toplam 2.4M TL.", Type = 1, IsRead = false, CreatedAt = now.AddHours(-1) },
            });

        // ==================== NOTIFICATIONS (8 adet) ====================
        await conn.ExecuteAsync(@"
            INSERT INTO Notifications (UserId, Type, Title, Message, RelatedUrl, IsRead, CreatedAt)
            VALUES (@UserId, @Type, @Title, @Message, @RelatedUrl, @IsRead, @CreatedAt)",
            new[] {
                new { UserId = itId, Type = "TaskAssigned", Title = "Yeni gorev atandi", Message = "ERP entegrasyonu analiz raporu gorevi size atandi.", RelatedUrl = "/gorevler", IsRead = true, CreatedAt = now.AddDays(-14) },
                new { UserId = satId, Type = "TaskAssigned", Title = "Yeni gorev atandi", Message = "Q1 satis hedefleri degerlendirme gorevi size atandi.", RelatedUrl = "/gorevler", IsRead = false, CreatedAt = now.AddDays(-10) },
                new { UserId = gmId, Type = "ApprovalRequired", Title = "Onay bekliyor", Message = "Q2 Butce Artis Talebi onayinizi bekliyor.", RelatedUrl = "/onaylar", IsRead = false, CreatedAt = now.AddDays(-3) },
                new { UserId = gmId, Type = "MeetingReminder", Title = "Toplanti hatirlatmasi", Message = "Satis Stratejisi Toplantisi yarin saat 10:00'da.", RelatedUrl = "/toplantilar", IsRead = false, CreatedAt = now.AddDays(-1) },
                new { UserId = opId, Type = "TaskAssigned", Title = "Yeni gorev atandi", Message = "Depo sayim raporu hazirlama gorevi size atandi.", RelatedUrl = "/gorevler", IsRead = true, CreatedAt = now.AddDays(-3) },
                new { UserId = finId, Type = "ApprovalRequired", Title = "Onay sonucu", Message = "Yeni Sunucu Alisim Onayi talep onaylandi.", RelatedUrl = "/onaylar", IsRead = true, CreatedAt = now.AddDays(-8) },
                new { UserId = gmId, Type = "ApprovalRequired", Title = "Onay bekliyor", Message = "Tedarikci Sozlesme Yenilemesi onayinizi bekliyor.", RelatedUrl = "/onaylar", IsRead = false, CreatedAt = now.AddDays(-1) },
                new { UserId = satId, Type = "MeetingReminder", Title = "Toplanti hatirlatmasi", Message = "Aylik Operasyon Degerlendirmesi haftaya planlanmistir.", RelatedUrl = "/toplantilar", IsRead = false, CreatedAt = now.AddHours(-6) },
            });

        // ==================== QUERY RECORDS (5 adet) ====================
        var qInsert = @"
            INSERT INTO QueryRecords (Name, Description, SqlContent, ResultType, UsagePurposeLookupId, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @SqlContent, @ResultType, @UsagePurposeLookupId, @CreatedAt)";

        var q1 = await conn.ExecuteScalarAsync<int>(qInsert, new {
            Name = "Gorev Durum Dagilimi", Description = "Gorevlerin durumlara gore dagilimi",
            SqlContent = "SELECT l.Value AS Durum, COUNT(*) AS Adet FROM TaskItems t JOIN Lookups l ON t.StatusLookupId = l.Id GROUP BY l.Value",
            ResultType = "PieChart", UsagePurposeLookupId = L("QueryUsage","Dashboard"), CreatedAt = now.AddDays(-20) });

        var q2 = await conn.ExecuteScalarAsync<int>(qInsert, new {
            Name = "Departman Bazli Gorev Sayisi", Description = "Her departmandaki aktif gorev sayisi",
            SqlContent = "SELECT ld.Value AS Departman, COUNT(t.Id) AS GorevSayisi FROM TaskItems t JOIN Users u ON t.AssigneeId = u.Id JOIN Lookups ld ON u.DepartmentLookupId = ld.Id GROUP BY ld.Value",
            ResultType = "BarChart", UsagePurposeLookupId = L("QueryUsage","Both"), CreatedAt = now.AddDays(-15) });

        var q3 = await conn.ExecuteScalarAsync<int>(qInsert, new {
            Name = "Aylik Toplanti Sayisi", Description = "Son 6 aydaki toplanti adetleri",
            SqlContent = "SELECT FORMAT(MeetingDate, 'yyyy-MM') AS Ay, COUNT(*) AS ToplantiSayisi FROM Meetings WHERE MeetingDate >= DATEADD(MONTH, -6, GETUTCDATE()) GROUP BY FORMAT(MeetingDate, 'yyyy-MM') ORDER BY Ay",
            ResultType = "LineChart", UsagePurposeLookupId = L("QueryUsage","Report"), CreatedAt = now.AddDays(-10) });

        var q4 = await conn.ExecuteScalarAsync<int>(qInsert, new {
            Name = "Aktif Kullanici Listesi", Description = "Sistemdeki aktif kullanicilarin listesi",
            SqlContent = "SELECT u.FullName AS Ad, u.Email, lr.Value AS Rol, ld.Value AS Departman FROM Users u LEFT JOIN Lookups lr ON u.RoleLookupId = lr.Id LEFT JOIN Lookups ld ON u.DepartmentLookupId = ld.Id WHERE u.IsActive = 1",
            ResultType = "Table", UsagePurposeLookupId = L("QueryUsage","Report"), CreatedAt = now.AddDays(-5) });

        var q5 = await conn.ExecuteScalarAsync<int>(qInsert, new {
            Name = "Geciken Gorev Sayisi KPI", Description = "Teslim tarihi gecmis tamamlanmamis gorevler",
            SqlContent = "SELECT COUNT(*) AS GecikenGorev FROM TaskItems WHERE DueDate < GETUTCDATE() AND StatusLookupId NOT IN (SELECT Id FROM Lookups WHERE [Group]='TaskStatus' AND Value IN ('Tamamlandi','Iptal'))",
            ResultType = "KpiCard", UsagePurposeLookupId = L("QueryUsage","Dashboard"), CreatedAt = now.AddDays(-3) });

        // ==================== QUERY SETS + SET QUERIES ====================
        var qsInsert = @"
            INSERT INTO QuerySets (Name, Description, Type, Icon, ShowInNavMenu, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @Type, @Icon, @ShowInNavMenu, @CreatedAt)";

        var qs1 = await conn.ExecuteScalarAsync<int>(qsInsert, new {
            Name = "Gorev Takip Paneli", Description = "Gorevlerle ilgili tum KPI ve grafikler",
            Type = "Dashboard", Icon = "CheckCircle", ShowInNavMenu = true, CreatedAt = now.AddDays(-15) });

        var qs2 = await conn.ExecuteScalarAsync<int>(qsInsert, new {
            Name = "Yonetim Rapor Seti", Description = "Ust yonetim icin genel durum raporlari",
            Type = "Report", Icon = "FileText", ShowInNavMenu = true, CreatedAt = now.AddDays(-10) });

        var qs3 = await conn.ExecuteScalarAsync<int>(qsInsert, new {
            Name = "KPI Ozet Paneli", Description = "Temel performans gostergeleri",
            Type = "KpiPanel", Icon = "Activity", ShowInNavMenu = false, CreatedAt = now.AddDays(-5) });

        // SetQuery linkages
        await conn.ExecuteAsync(@"
            INSERT INTO SetQueries (SetId, QueryId, OrderIndex, WidthMd)
            VALUES (@SetId, @QueryId, @OrderIndex, @WidthMd)",
            new[] {
                new { SetId = qs1, QueryId = q1, OrderIndex = 0, WidthMd = 6 },
                new { SetId = qs1, QueryId = q2, OrderIndex = 1, WidthMd = 6 },
                new { SetId = qs1, QueryId = q5, OrderIndex = 2, WidthMd = 4 },
                new { SetId = qs2, QueryId = q3, OrderIndex = 0, WidthMd = 12 },
                new { SetId = qs2, QueryId = q4, OrderIndex = 1, WidthMd = 12 },
                new { SetId = qs3, QueryId = q5, OrderIndex = 0, WidthMd = 4 },
                new { SetId = qs3, QueryId = q1, OrderIndex = 1, WidthMd = 4 },
            });

        // ==================== KPI TARGETS (4 adet) ====================
        await conn.ExecuteAsync(@"
            INSERT INTO KpiTargets (MetricKey, Label, TargetValue, Operator, PeriodType, IsActive, CreatedAt)
            VALUES (@MetricKey, @Label, @TargetValue, @Operator, @PeriodType, 1, @CreatedAt)",
            new[] {
                new { MetricKey = "overdue_tasks", Label = "Geciken Gorev Sayisi", TargetValue = 5.0m, Operator = "<=", PeriodType = "AllTime", CreatedAt = now },
                new { MetricKey = "task_completion_rate", Label = "Gorev Tamamlanma Orani", TargetValue = 80.0m, Operator = ">=", PeriodType = "Monthly", CreatedAt = now },
                new { MetricKey = "meeting_decision_rate", Label = "Toplanti Basi Karar Sayisi", TargetValue = 2.0m, Operator = ">=", PeriodType = "Monthly", CreatedAt = now },
                new { MetricKey = "approval_avg_days", Label = "Ortalama Onay Suresi (gun)", TargetValue = 3.0m, Operator = "<=", PeriodType = "Monthly", CreatedAt = now },
            });
    }

    private static async Task SeedSemanticDefinitionsAsync(SqlConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SemanticDefinitions");
        if (count > 0) return;

        var definitions = new[]
        {
            // Tablo tanımları
            new { TermType = "table", TableName = (string?)"Tasks", ColumnName = (string?)null, BusinessName = "Görevler", Description = "Tüm görev kayıtları", SqlExpression = (string?)null, Aliases = "görev,task,iş" },
            new { TermType = "table", TableName = (string?)"Meetings", ColumnName = (string?)null, BusinessName = "Toplantılar", Description = "Tüm toplantı kayıtları", SqlExpression = (string?)null, Aliases = "toplantı,meeting" },
            new { TermType = "table", TableName = (string?)"Decisions", ColumnName = (string?)null, BusinessName = "Kararlar", Description = "Toplantı kararları", SqlExpression = (string?)null, Aliases = "karar,decision" },
            new { TermType = "table", TableName = (string?)"Users", ColumnName = (string?)null, BusinessName = "Kullanıcılar", Description = "Sistem kullanıcıları", SqlExpression = (string?)null, Aliases = "kullanıcı,personel,çalışan,user" },
            new { TermType = "table", TableName = (string?)"ApprovalRequests", ColumnName = (string?)null, BusinessName = "Onay Talepleri", Description = "Onay iş akışı talepleri", SqlExpression = (string?)null, Aliases = "onay,approval" },
            new { TermType = "table", TableName = (string?)"Objectives", ColumnName = (string?)null, BusinessName = "Hedefler (OKR)", Description = "Stratejik hedefler", SqlExpression = (string?)null, Aliases = "hedef,objective,okr" },
            new { TermType = "table", TableName = (string?)"PersonalNotes", ColumnName = (string?)null, BusinessName = "Kişisel Notlar", Description = "Kullanıcı notları", SqlExpression = (string?)null, Aliases = "not,note" },
            // Metrik tanımları
            new { TermType = "metric", TableName = (string?)null, ColumnName = (string?)null, BusinessName = "Aktif Görev Sayısı", Description = "Tamamlanmamış ve iptal edilmemiş görevler", SqlExpression = (string?)"SELECT COUNT(*) FROM Tasks WHERE StatusLookupId NOT IN (SELECT Id FROM Lookups WHERE [Group]='TaskStatus' AND Value IN ('Tamamlandı','İptal'))", Aliases = "aktif görev,bekleyen görev" },
            new { TermType = "metric", TableName = (string?)null, ColumnName = (string?)null, BusinessName = "Geciken Görev Sayısı", Description = "Son tarihi geçmiş tamamlanmamış görevler", SqlExpression = (string?)"SELECT COUNT(*) FROM Tasks WHERE DueDate < GETUTCDATE() AND StatusLookupId NOT IN (SELECT Id FROM Lookups WHERE [Group]='TaskStatus' AND Value IN ('Tamamlandı','İptal'))", Aliases = "geciken,overdue" },
            new { TermType = "metric", TableName = (string?)null, ColumnName = (string?)null, BusinessName = "Toplam Kullanıcı", Description = "Aktif sistem kullanıcıları", SqlExpression = (string?)"SELECT COUNT(*) FROM Users WHERE IsActive = 1", Aliases = "kullanıcı sayısı,personel sayısı" },
        };

        foreach (var d in definitions)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO SemanticDefinitions (TermType, TableName, ColumnName, BusinessName, Description, SqlExpression, Aliases)
                VALUES (@TermType, @TableName, @ColumnName, @BusinessName, @Description, @SqlExpression, @Aliases)", d);
        }
    }
}
