using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Dashboard ve Sorgu Setlerini yöneten servistir.
/// Panoların oluşturulması, sorguların panolara eklenmesi ve panoların çalıştırılmasını sağlar.
/// </summary>
public class QuerySetService(IConfiguration config, AuditService auditService, QueryService querySvc) : BaseService(config, auditService)
{
    /// <summary>
    /// Servis üzerindeki veriler değiştiğinde (ekleme, silme, güncelleme) tetiklenen olay.
    /// Genellikle NavMenu gibi bileşenlerin kendini yenilemesi için kullanılır.
    /// </summary>
    public event Func<System.Threading.Tasks.Task>? OnChange;



    /// <summary>
    /// Dashboad'lardaki veri değişikliklerini dinleyen bileşenlere bildirim gönderir.
    /// </summary>
    private async Task NotifyChangeAsync()
    {
        if (OnChange is null) return;

        var handlers = OnChange.GetInvocationList()
            .Cast<Func<System.Threading.Tasks.Task>>()
            .Select(async h =>
            {
                try { await h(); }
                catch { /* Bağımsız hata yönetimi: Menü yenileme ana akışı bozmamalı */ }
            });

        await System.Threading.Tasks.Task.WhenAll(handlers);
    }

    /// <summary>
    /// Sistemde kayıtlı olan tüm sorgu setlerini (panoları) getirir.
    /// </summary>
    public async Task<ServiceResult<List<QuerySet>>> GetAllAsync()
    {
        return await ExecuteServiceAsync<List<QuerySet>>(async conn =>
        {
            return (await conn.QueryAsync<QuerySet>(@"
                SELECT Id, Name, Description, Type, Icon, ShowInNavMenu, NavMenuOrder, CreatedAt
                FROM QuerySets
                ORDER BY NavMenuOrder, Name")).ToList();
        });
    }

    /// <summary>
    /// Belirli bir ID'ye sahip sorgu setinin detaylarını getirir.
    /// </summary>
    public async Task<ServiceResult<QuerySet?>> GetByIdAsync(int id)
    {
        return await ExecuteServiceAsync<QuerySet?>(async conn =>
        {
            return await conn.QueryFirstOrDefaultAsync<QuerySet>(@"
                SELECT Id, Name, Description, Type, Icon, ShowInNavMenu, NavMenuOrder, CreatedAt
                FROM QuerySets
                WHERE Id = @Id", new { Id = id });
        });
    }

    /// <summary>
    /// Yan menüde (NavMenu) gösterilmesi işaretlenmiş setleri getirir.
    /// </summary>
    public async Task<ServiceResult<List<QuerySet>>> GetNavSetsAsync()
    {
        return await ExecuteServiceAsync<List<QuerySet>>(async conn =>
        {
            return (await conn.QueryAsync<QuerySet>(@"
                SELECT Id, Name, Description, Type, Icon, ShowInNavMenu, NavMenuOrder, CreatedAt
                FROM QuerySets
                WHERE ShowInNavMenu = 1
                ORDER BY NavMenuOrder, Name")).ToList();
        });
    }

    /// <summary>
    /// Bir sorgu setini (Dashboard) kaydeder veya mevcut olanı günceller.
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(QuerySet s)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (s.Id == 0) s.CreatedAt = DateTime.UtcNow;
            int id;
            if (s.Id == 0)
            {
                id = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO QuerySets (Name, Description, Type, Icon, ShowInNavMenu, NavMenuOrder, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Description, @Type, @Icon, @ShowInNavMenu, @NavMenuOrder, @CreatedAt)",
                    new
                    {
                        s.Name,
                        s.Description,
                        Type = s.Type,
                        s.Icon,
                        s.ShowInNavMenu,
                        s.NavMenuOrder,
                        s.CreatedAt
                    });
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE QuerySets
                    SET Name = @Name,
                        Description = @Description,
                        Type = @Type,
                        Icon = @Icon,
                        ShowInNavMenu = @ShowInNavMenu,
                        NavMenuOrder = @NavMenuOrder
                    WHERE Id = @Id", new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    Type = s.Type.ToString(),
                    s.Icon,
                    s.ShowInNavMenu,
                    s.NavMenuOrder
                });
                id = s.Id;
            }
            LogAction("QuerySet", s.Id > 0 ? "Update" : "Add", new { Id = id, Name = s.Name });
            await NotifyChangeAsync();
            return id;
        });
    }

    /// <summary>
    /// Bir sorgu setini ve içindeki tüm sorgu eşleşmelerini siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM SetQueries WHERE SetId = @Id", new { Id = id });
            await conn.ExecuteAsync("DELETE FROM QuerySets WHERE Id = @Id", new { Id = id });
            LogAction("QuerySet", "Delete", new { Id = id });
            await NotifyChangeAsync();
        });
    }

    /// <summary>
    /// Belirli bir set içerisindeki sorguları ve bu sorguların görüntüleme metadatalarını getirir.
    /// </summary>
    public async Task<ServiceResult<List<(SetQuery Meta, QueryRecord Query)>>> GetSetQueriesAsync(int setId)
    {
        return await ExecuteServiceAsync<List<(SetQuery Meta, QueryRecord Query)>>(async conn =>
        {
            var rows = await conn.QueryAsync<SetQueryRow>(@"
                SELECT
                    ss.Id AS SetQueryId,
                    ss.SetId,
                    ss.QueryId AS LinkedQueryId,
                    ss.OrderIndex,
                    ss.WidthMd,
                    sk.Id AS QueryRecordId,
                    sk.Name AS Name,
                    sk.Description AS Description,
                    sk.SqlContent AS SqlContent,
                    sk.ResultType AS ResultType,
                    sk.Color AS Color,
                    sk.PivotRowColumn AS PivotRowColumn,
                    sk.PivotColumnColumn AS PivotColumnColumn,
                    sk.PivotValueColumn AS PivotValueColumn,
                    sk.UpdatedAt AS UpdatedAt
                FROM SetQueries ss
                INNER JOIN QueryRecords sk ON sk.Id = ss.QueryId
                WHERE ss.SetId = @SetId
                ORDER BY ss.OrderIndex", new { SetId = setId });

            return rows.Select(r =>
                (
                    new SetQuery
                    {
                        Id = r.SetQueryId,
                        SetId = r.SetId,
                        QueryId = r.LinkedQueryId,
                        OrderIndex = r.OrderIndex,
                        WidthMd = r.WidthMd
                    },
                    new QueryRecord
                    {
                        Id = r.QueryRecordId,
                        Name = r.Name,
                        Description = r.Description,
                        SqlContent = r.SqlContent,
                        ResultType = r.ResultType,
                        Color = r.Color,
                        PivotRowColumn = r.PivotRowColumn,
                        PivotColumnColumn = r.PivotColumnColumn,
                        PivotValueColumn = r.PivotValueColumn,
                        UpdatedAt = r.UpdatedAt
                    }
                ))
                .ToList();
        });
    }

    /// <summary>
    /// Bir sorguyu belirli bir sete (Dashboard) dahil eder.
    /// </summary>
    public async Task<ServiceResult> AddQueryToSetAsync(int setId, int queryId, int widthMd = 6, int? order = null)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var targetOrder = await conn.ExecuteScalarAsync<int>(
                "SELECT ISNULL(MAX(OrderIndex), -1) + 1 FROM SetQueries WHERE SetId = @SetId",
                new { SetId = setId });

            await conn.ExecuteAsync(@"
                INSERT INTO SetQueries (SetId, QueryId, OrderIndex, WidthMd)
                VALUES (@SetId, @QueryId, @OrderIndex, @WidthMd)",
                new { SetId = setId, QueryId = queryId, OrderIndex = targetOrder, WidthMd = widthMd });
            
            LogAction("QuerySet", "AddQuery", new { SetId = setId, QueryId = queryId });
        });
    }

    /// <summary>
    /// Bir sorguyu set içerisinden çıkartır (Eşleşmeyi siler).
    /// </summary>
    public async Task<ServiceResult> RemoveQueryFromSetAsync(int setQueryId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "DELETE FROM SetQueries WHERE Id = @Id",
                new { Id = setQueryId });
            LogAction("QuerySet", "RemoveQuery", new { SetQueryId = setQueryId });
        });
    }

    /// <summary>
    /// Dashboard içerisindeki bir kartın genişliğini günceller.
    /// </summary>
    public async Task<ServiceResult> UpdateWidthAsync(int setQueryId, int widthMd)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE SetQueries SET WidthMd = @WidthMd WHERE Id = @Id",
                new { Id = setQueryId, WidthMd = widthMd });
        });
    }

    /// <summary>
    /// Dashboard içerisindeki kartın görüntüleme sırasını günceller.
    /// </summary>
    public async Task<ServiceResult> UpdateOrderAsync(int setQueryId, int newOrder)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE SetQueries SET OrderIndex = @OrderIndex WHERE Id = @Id",
                new { Id = setQueryId, OrderIndex = newOrder });
        });
    }

    /// <summary>
    /// Bir set (Dashboard) içerisindeki tüm sorguları asenkron paralel olarak çalıştırır ve sonuçları döner.
    /// </summary>
    public async Task<ServiceResult<List<(SetQuery Meta, QueryRecord Query, QueryResult Result)>>> RunSetAsync(int setId)
    {
        var queriesResult = await GetSetQueriesAsync(setId);
        if (!queriesResult.IsSuccess) return ServiceResult<List<(SetQuery Meta, QueryRecord Query, QueryResult Result)>>.Failure(queriesResult.Message!);

        var queries = queriesResult.Data!;
        var tasks = queries.Select(async item =>
        {
            // SQL sorgusunu modernize edilmiş servis üzerinden çalıştır
            var resultObj = await querySvc.ExecuteAsync(item.Query.SqlContent);
            var result = resultObj;
            if (!result.IsSuccess) result.ErrorMessage = resultObj.ErrorMessage;

            // Eğer pivot ayarları varsa sonucu pivotla
            if (result.IsSuccess && item.Query.ResultType == "PivotTable"
                && item.Query.PivotRowColumn is not null
                && item.Query.PivotColumnColumn is not null
                && item.Query.PivotValueColumn is not null)
            {
                result = QueryService.Pivot(
                    result,
                    item.Query.PivotRowColumn,
                    item.Query.PivotColumnColumn,
                    item.Query.PivotValueColumn);
            }

            return (item.Meta, item.Query, result);
        });

        var results = (await System.Threading.Tasks.Task.WhenAll(tasks)).ToList();
        return ServiceResult<List<(SetQuery Meta, QueryRecord Query, QueryResult Result)>>.Success(results);
    }

    private sealed class SetQueryRow
    {
        public int SetQueryId { get; set; }
        public int SetId { get; set; }
        public int LinkedQueryId { get; set; }
        public int OrderIndex { get; set; }
        public int WidthMd { get; set; }

        public int QueryRecordId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SqlContent { get; set; } = string.Empty;
        public string ResultType { get; set; } = "Table";
        public string Color { get; set; } = "#1976d2";
        public string? PivotRowColumn { get; set; }
        public string? PivotColumnColumn { get; set; }
        public string? PivotValueColumn { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}


