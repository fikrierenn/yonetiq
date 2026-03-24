using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;
using System.Text.Json;

namespace YonetIQ.Data.Services;

/// <summary>
/// Sistem üzerindeki kritik işlemleri (Ekleme, Silme, Güncelleme vb.) veritabanına loglayan servistir.
/// Denetim izi (Audit Trail) takibi için kullanılır.
/// </summary>
public class AuditService(IConfiguration config, SessionService sessionService) : BaseService(config)
{
    private readonly SessionService _sessionService = sessionService;

    /// <summary>
    /// Yeni bir sistem günlüğü (audit log) kaydı oluşturur.
    /// </summary>
    /// <param name="module">İşlemin yapıldığı modül adı (örn: User, Task).</param>
    /// <param name="action">Yapılan eylem (örn: Add, Update, Delete).</param>
    /// <param name="detailsObj">İşlemle ilgili ek detaylar (JSON olarak saklanır).</param>
    public async Task<ServiceResult> SaveLogAsync(string module, string action, object? detailsObj = null)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var user = _sessionService.ActiveUser;
            var details = detailsObj is not null ? JsonSerializer.Serialize(detailsObj) : null;

            await conn.ExecuteAsync(
                "sp_SystemLog_Add",
                new
                {
                    UserId = user?.UserId,
                    UserName = user?.FullName ?? "System",
                    Module = module,
                    Action = action,
                    Details = details
                },
                commandType: System.Data.CommandType.StoredProcedure);
        });
    }

    /// <summary>
    /// Sistem günlüklerini sondan başa doğru listeler.
    /// </summary>
    /// <param name="limit">Dönecek maksimum kayıt sayısı.</param>
    public async Task<ServiceResult<List<SystemLog>>> ListLogsAsync(int limit = 100)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var logs = await conn.QueryAsync<SystemLog>(
                "sp_SystemLog_List",
                new { Limit = limit },
                commandType: System.Data.CommandType.StoredProcedure);
            
            return logs.ToList();
        });
    }
}
