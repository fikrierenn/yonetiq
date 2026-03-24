using YonetIQ.Data.Models;
using YonetIQ.Data.ViewModels;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Raporlama işlemlerini (listeleme, çalıştırma, favoriye ekleme ve paylaşma) yöneten servis.
/// ReportingHub benzeri bir akış sağlar.
/// </summary>
public class ReportingService(IConfiguration config, QueryService querySvc, LookupService lookupSvc, AuditService auditService) : BaseService(config, auditService)
{
    /// <summary>
    /// Rapor listeleme sayfası için gerekli tüm verileri içeren modeli hazırlar.
    /// </summary>
    public async Task<ServiceResult<ReportsIndexViewModel>> CreateListModelAsync(string? searchText, string? selectedCategory, bool onlyShared = false)
    {
        return await ExecuteServiceAsync<ReportsIndexViewModel>(async conn =>
        {
            var model = new ReportsIndexViewModel
            {
                SearchText = (searchText ?? string.Empty).Trim(),
                SelectedCategory = (selectedCategory ?? string.Empty).Trim(),
                OnlyShared = onlyShared
            };

            // Mevcut kullanıcı ID'sini al
            var userIdRes = await querySvc.GetDefaultUserIdAsync();
            model.UserId = userIdRes.Data;

            // 'QueryUsage' grubundan 'Report' ve 'Both' ID'lerini al
            var reportUsageRes = await lookupSvc.GetLookupIdAsync("QueryUsage", "Report");
            var bothUsageRes = await lookupSvc.GetLookupIdAsync("QueryUsage", "Both");

            var purposeIds = new List<int>();
            if (reportUsageRes.Data.HasValue) purposeIds.Add(reportUsageRes.Data.Value);
            if (bothUsageRes.Data.HasValue) purposeIds.Add(bothUsageRes.Data.Value);

            // Rapor amaçlı sorguları getir
            var reportsRes = await querySvc.GetQueriesByPurposeAsync(purposeIds.ToArray());
            if (!reportsRes.IsSuccess) return model;

            var reports = reportsRes.Data ?? [];

            // Rapor kategorilerini benzersiz olarak listele
            model.Categories = reports
                .Select(x => x.ResultType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (model.UserId.HasValue)
            {
                // Kullanıcının favori ve paylaşılan raporlarını getir
                var favsRes = await querySvc.GetFavoriteQueryIdsAsync(model.UserId.Value);
                var sharedRes = await querySvc.GetSharedQueryIdsAsync(model.UserId.Value);
                
                model.FavoriteReportIds = favsRes.Data ?? [];
                model.SharedReportIds = sharedRes.Data ?? [];
            }

            // Filtreleri uygulayarak son listeyi oluştur
            model.Reports = FilterAndSort(reports, model.SearchText, model.SelectedCategory, model.FavoriteReportIds, model.OnlyShared, model.SharedReportIds);

            return model;
        });
    }

    /// <summary>
    /// Belirli bir raporu çalıştırır ve sonuçlarla birlikte önizleme modelini döner.
    /// </summary>
    public async Task<ServiceResult<ReportRunViewModel>> CreateReportRunModelAsync(
        int reportId,
        string? viewMode = null,
        string? resultSearch = null,
        Dictionary<string, string>? parameters = null)
    {
        return await ExecuteServiceAsync<ReportRunViewModel>(async conn =>
        {
            var model = new ReportRunViewModel
            {
                ViewMode = NormalizeViewMode(viewMode),
                ResultSearch = (resultSearch ?? string.Empty).Trim()
            };

            // Rapor tanımını getir
            var queryRes = await querySvc.GetQueryAsync(reportId);
            if (!queryRes.IsSuccess || queryRes.Data is null)
            {
                model.Error = "Rapor bulunamadı.";
                return model;
            }

            model.SelectedReport = queryRes.Data;

            // Dashboard amaçlı sorguları engelle
            var dashboardUsageRes = await lookupSvc.GetLookupIdAsync("QueryUsage", "Dashboard");
            if (dashboardUsageRes.Data.HasValue && model.SelectedReport.UsagePurposeLookupId == dashboardUsageRes.Data.Value)
            {
                model.Error = "Bu sorgu dashboard için ayrılmış, rapor ekranında çalıştırılamaz.";
                return model;
            }

            // SQL'ı çalıştır ve süreyi ölç (raporun DataSourceId'si ile hedef sunucuyu seç)
            // AllowDml ve kullanıcı parametreleri rapor kaydından alınarak geçirilir.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await querySvc.ExecuteAsync(
                model.SelectedReport.SqlContent,
                dataSourceId: model.SelectedReport.DataSourceId,
                allowDml: model.SelectedReport.AllowDml,
                parameters: parameters);
            sw.Stop();

            model.ExecutionTimeMs = sw.ElapsedMilliseconds;

            // İşlemi denetim günlüğüne kaydet
            LogAction("Report", "Execute", new { ReportId = reportId, ReportName = model.SelectedReport.Name, ExecutionTimeMs = model.ExecutionTimeMs, IsSuccess = result.IsSuccess });

            if (!result.IsSuccess)
                model.Error = result.ErrorMessage ?? "Rapor çalıştırma hatası.";

            model.RawResult = result;
            model.RawRowCount = result.Rows.Count;

            // Sonuçlar içerisinde arama (Client-side benzeri filtreleme)
            if (result is not null)
            {
                model.DisplayResult = FilterResult(result, model.ResultSearch);
                model.DisplayedRowCount = model.DisplayResult.Rows.Count;
            }

            return model;
        });
    }

    /// <summary>
    /// Sorgu sonucundaki satırları verilen arama metnine göre filtreler.
    /// </summary>
    public QueryResult FilterResult(QueryResult result, string? search)
    {
        var filter = (search ?? string.Empty).Trim().ToLowerInvariant();

        var rows = string.IsNullOrWhiteSpace(filter)
            ? result.Rows
            : result.Rows
                .Where(s => s.Values.Any(v => (v?.ToString() ?? string.Empty).ToLowerInvariant().Contains(filter)))
                .ToList();

        return new QueryResult
        {
            ErrorMessage = result.ErrorMessage,
            Columns = result.Columns.ToList(),
            Rows = rows
        };
    }

    /// <summary>
    /// Bir raporun favori durumunu tersine çevirir.
    /// </summary>
    public async Task<ServiceResult<bool>> ToggleFavoriteAsync(
        int reportId,
        bool currentlyFavorite)
    {
        var userIdRes = await querySvc.GetDefaultUserIdAsync();
        if (!userIdRes.IsSuccess || !userIdRes.Data.HasValue)
            return ServiceResult<bool>.Failure("Favori işlemi için aktif kullanıcı bulunamadı.");

        var userId = userIdRes.Data.Value;

        if (currentlyFavorite)
        {
            var res = await querySvc.RemoveFavoriteAsync(userId, reportId);
            if (!res.IsSuccess) return ServiceResult<bool>.Failure(res.Message ?? "Favori kaldırılamadı.");
            LogAction("Report", "RemoveFavorite", new { ReportId = reportId });
            return ServiceResult<bool>.Success(false);
        }
        else
        {
            var res = await querySvc.AddFavoriteAsync(userId, reportId);
            if (!res.IsSuccess) return ServiceResult<bool>.Failure(res.Message ?? "Favori eklenemedi.");
            LogAction("Report", "AddFavorite", new { ReportId = reportId });
            return ServiceResult<bool>.Success(true);
        }
    }

    /// <summary>
    /// Raporu belirli bir kullanıcı veya departmanla paylaşır.
    /// </summary>
    public async Task<ServiceResult> ShareReportAsync(int queryId, int sharingUserId, int? targetUserId, string? targetDepartment, string? note = null)
    {
        var res = await querySvc.ShareReportAsync(queryId, sharingUserId, targetUserId, targetDepartment, note);
        if (res.IsSuccess)
        {
            LogAction("Report", "Share", new { ReportId = queryId, TargetUserId = targetUserId, TargetDepartment = targetDepartment });
        }
        return res;
    }

    /// <summary>
    /// Daha önce yapılmış bir paylaşımı iptal eder.
    /// </summary>
    public async Task<ServiceResult> UndoShareAsync(int shareId)
    {
        var res = await querySvc.UndoShareAsync(shareId);
        if (res.IsSuccess)
        {
            LogAction("Report", "UndoShare", new { ShareId = shareId });
        }
        return res;
    }

    /// <summary>
    /// Bir raporun kimlerle paylaşıldığı bilgisini getirir.
    /// </summary>
    public async Task<ServiceResult<List<ReportShare>>> ListSharedWithAsync(int queryId, int sharingUserId)
    {
        return await querySvc.ListSharedWithAsync(queryId, sharingUserId);
    }

    /// <summary>
    /// Rapor listesini filtreleme ve sıralama kurallarına göre düzenler.
    /// </summary>
    private static List<QueryRecord> FilterAndSort(
        IEnumerable<QueryRecord> source,
        string search,
        string category,
        HashSet<int> favorites,
        bool onlyShared,
        HashSet<int> shared)
    {
        var q = (search ?? "").Trim().ToLowerInvariant();
        var selected = (category ?? "").Trim();

        return source
            .Where(r =>
                (!onlyShared || shared.Contains(r.Id))
                 && (string.IsNullOrWhiteSpace(q)
                 || (r.Name ?? string.Empty).ToLowerInvariant().Contains(q)
                 || (r.Description ?? string.Empty).ToLowerInvariant().Contains(q))
                && (string.IsNullOrWhiteSpace(selected)
                    || string.Equals(r.ResultType, selected, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => favorites.Contains(r.Id))
            .ThenBy(r => r.Name)
            .ToList();
    }

    /// <summary>
    /// Görünüm modu parametresini standart formatlara normalize eder.
    /// </summary>
    private static string NormalizeViewMode(string? viewMode)
    {
        var mode = (viewMode ?? string.Empty).Trim().ToLowerInvariant();
        return mode switch
        {
            "wide" => "wide",
            "table" => "table",
            "full" => "full",
            _ => string.Empty
        };
    }

}


