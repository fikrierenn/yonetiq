using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

// SQL içinde kullanılan Lookup grup/değer sabitleri için Infrastructure.LookupConstants kullanılır.

/// <summary>
/// Kurumsal Zeka / Analitik sayfası için grafik verilerini sağlar.
/// Tüm SQL sorguları bu serviste tutulur; UI katmanında ham SQL yazılmaz.
/// </summary>
public class AnalyticsService(QueryService queryService)
{
    /// <summary>
    /// Görev verimlilik trendi: günlük oluşturulan ve tamamlanan görev sayıları.
    /// Tamamlanan sayı için Lookups tablosundaki TaskStatus = 'Tamamlandi' kullanılır.
    /// </summary>
    public async Task<ServiceResult<QueryResult>> GetTaskTrendResultAsync()
    {
        var sql = @"
            SELECT
                CAST(CreatedAt AS DATE) AS [Tarih],
                COUNT(*) AS [Toplam],
                SUM(CASE WHEN StatusLookupId = (SELECT Id FROM Lookups WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"' AND Value = '" + LookupConstants.ValueTamamlandi + @"') THEN 1 ELSE 0 END) AS [Tamamlanan]
            FROM TaskItems
            GROUP BY CAST(CreatedAt AS DATE)
            ORDER BY [Tarih]";
        return await RunAndWrapAsync(sql, "Görev Verimlilik Trendi");
    }

    /// <summary>
    /// Mesajlaşma aktivitesi: kullanıcı bazında gönderilen mesaj sayıları.
    /// </summary>
    public async Task<ServiceResult<QueryResult>> GetMessageActivityResultAsync()
    {
        const string sql = @"
            SELECT ISNULL(u.FullName, N'Sistem') AS [Kullanici], COUNT(*) AS [Mesaj]
            FROM CommunicationMessages m
            LEFT JOIN Users u ON m.SenderUserId = u.Id
            GROUP BY u.FullName";
        return await RunAndWrapAsync(sql, "Mesajlaşma Aktivitesi");
    }

    /// <summary>
    /// Görev durum dağılımı: her durum (Lookup) için görev adedi.
    /// </summary>
    public async Task<ServiceResult<QueryResult>> GetTaskStatusDistributionResultAsync()
    {
        const string sql = @"
            SELECT (SELECT Value FROM Lookups WHERE Id = StatusLookupId) AS [Durum], COUNT(*) AS [Adet]
            FROM TaskItems
            GROUP BY StatusLookupId";
        return await RunAndWrapAsync(sql, "Görev Durum Dağılımı");
    }

    /// <summary>
    /// QueryService.ExecuteAsync sonucunu ServiceResult ile sarmalayıp döndürür.
    /// Hata durumunda kullanıcı dostu mesaj ve QueryError kodu kullanılır.
    /// </summary>
    private async Task<ServiceResult<QueryResult>> RunAndWrapAsync(string sql, string displayName)
    {
        var result = await queryService.ExecuteAsync(sql);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            return ServiceResult<QueryResult>.Failure(result.ErrorMessage, "QueryError");
        result.Name = displayName;
        return ServiceResult<QueryResult>.Success(result);
    }
}
