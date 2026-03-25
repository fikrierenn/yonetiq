using Dapper;
using Microsoft.Data.SqlClient;

namespace YonetIQ.Data;

/// <summary>
/// Uygulama ilk ayağa kalktığında veritabanı şemasının doğrulanması ve başlangıç verilerinin (Seed) atılması işlemlerini yönetir.
/// Bu sınıf partial olarak InfrastructureSeed.cs ve DataSeed.cs dosyalarına bölünmüştür (300 satır sınırı).
/// </summary>
public static partial class SeedData
{
    /// <summary>
    /// Ana tohumlama (Seed) metodudur. Gerekli tüm hazırlık ve veri ekleme işlemlerini sırasıyla çağırır.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var connStr = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr)) return;

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // Temel tablo kontrolü
        if (!await TableExistsAsync(conn, "Users")) return;

        // Altyapı Hazırlıkları (InfrastructureSeed.cs)
        await PrepareUpdatedAtColumnsAsync(conn);
        await PrepareOkrInfrastructureAsync(conn);
        await PrepareUserPasswordColumnsAsync(conn);
        await PrepareLookupsTableAsync(conn);
        await PrepareUserLookupColumnsAsync(conn);
        await PrepareTaskAndDecisionLookupColumnsAsync(conn);
        await PrepareCalendarSpecialDayInfrastructureAsync(conn);
        await PrepareCalendarSpecialDayLookupColumnsAsync(conn);
        await PrepareReportFavoriteInfrastructureAsync(conn);
        await PrepareQueryUsagePurposeLookupColumnsAsync(conn);
        await PrepareSystemLogInfrastructureAsync(conn);
        await PrepareReportShareInfrastructureAsync(conn);
        await PrepareCommunicationMessageInfrastructureAsync(conn);
        await PrepareAttachmentInfrastructureAsync(conn);
        await PrepareMeetingRecurrenceColumnsAsync(conn);
        await PreparePersonalNotesInfrastructureAsync(conn);
        await PrepareNotificationsInfrastructureAsync(conn);
        await PrepareDataSourcesInfrastructureAsync(conn);
        await PrepareSystemSettingsInfrastructureAsync(conn);
        await PrepareTaskCommentInfrastructureAsync(conn);
        await PrepareScheduledReportsInfrastructureAsync(conn);
        await PrepareKpiTargetsInfrastructureAsync(conn);
        await PrepareTaskTemplatesInfrastructureAsync(conn);
        await PrepareTimeTrackingInfrastructureAsync(conn);
        await PrepareApprovalInfrastructureAsync(conn);
        await NormalizeRoleValuesAsync(conn);
        await PrepareAiInfrastructureAsync(conn);
        await PrepareSemanticDefinitionsAsync(conn);
        await PrepareAiPatternsAsync(conn);

        // WP1: Faz 2 — Yeni tablolar ve genişletmeler
        await PrepareAiSkillDefinitionsAsync(conn);
        await PrepareAiPatternSignalsAsync(conn);
        await PrepareAiPatternsExtensionsAsync(conn);
        await PrepareSemanticLearningCandidatesAsync(conn);
        await PrepareAiQueryLogAsync(conn);
        await PrepareAiConversationContextAsync(conn);
        await PrepareAiSkillTriggerLogAsync(conn);
        await PrepareUserDashboardPreferencesAsync(conn);
        await PrepareDecisionExtensionsAsync(conn);

        // Veri Tohumlama (DataSeed.cs)
        await SeedLookupsAsync(conn);
        await SeedUsersAsync(conn);
        await EnsureDefaultPasswordsAsync(conn);
        await SeedMeetingsAsync(conn);
        await SeedTasksAsync(conn);
        await SeedQueriesAsync(conn);
        await SeedQuerySetsAsync(conn);
        await SeedReportFavoritesAsync(conn);
        await SeedCalendarSpecialDaysAsync(conn);
        await SeedMessagesAsync(conn);
        await SeedSemanticDefinitionsAsync(conn);
        await SeedComprehensiveTestDataAsync(conn);
    }

    /// <summary>
    /// Belirtilen tablonun veritabanında mevcut olup olmadığını kontrol eder.
    /// </summary>
    private static async Task<bool> TableExistsAsync(SqlConnection conn, string table)
    {
        var count = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = @table", new { table });
        return count > 0;
    }
}
