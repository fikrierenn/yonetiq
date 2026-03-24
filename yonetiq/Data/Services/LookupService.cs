using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Sistem tanımlarını (Lookups) yöneten servis sınıfıdır. 
/// Kategorik verilerin (Departman, Rol, Durum vb.) veritabanından getirilmesi ve yönetilmesini sağlar.
/// </summary>
public class LookupService(IConfiguration config) : BaseService(config)
{
    /// <summary>
    /// Belirli bir gruba (örn: "TaskStatus") ait aktif tanımları listeler.
    /// </summary>
    public async Task<List<Lookup>> ListByGroupAsync(string group)
    {
        var result = await ExecuteServiceAsync<List<Lookup>>(async conn =>
        {
            return (await conn.QueryAsync<Lookup>(
                "sp_Lookup_ListByGroup",
                new { Group = group },
                commandType: System.Data.CommandType.StoredProcedure)).ToList();
        });

        return result.Data ?? [];
    }

    /// <summary>
    /// Belirli bir grup ve değere göre Lookup ID'sini döner.
    /// </summary>
    public async Task<ServiceResult<int?>> GetLookupIdAsync(string group, string value)
    {
        return await ExecuteServiceAsync<int?>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int?>(@"
                SELECT Id FROM Lookups 
                WHERE [Group] = @Group AND Value = @Value AND IsActive = 1", 
                new { Group = group, Value = value });
        });
    }

    /// <summary>
    /// Sistemdeki tüm tanımları (gruplarıyla birlikte) listeler. (Yönetim Paneli için)
    /// </summary>
    public async Task<ServiceResult<List<Lookup>>> GetAllAsync()
    {
        return await ExecuteServiceAsync<List<Lookup>>(async conn =>
        {
            return (await conn.QueryAsync<Lookup>(@"
                SELECT Id, [Group], Value, IsActive
                FROM Lookups
                ORDER BY [Group], Value")).ToList();
        });
    }

    /// <summary>
    /// Yeni bir tanım ekler veya mevcut tanımı günceller.
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(Lookup lookup)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (lookup.Id == 0)
            {
                var sql = @"
                    INSERT INTO Lookups ([Group], Value, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@Group, @Value, @IsActive)";

                return await conn.ExecuteScalarAsync<int>(sql, lookup);
            }
            else
            {
                var sql = @"
                    UPDATE Lookups
                    SET [Group] = @Group,
                        Value = @Value,
                        IsActive = @IsActive
                    WHERE Id = @Id";

                await conn.ExecuteAsync(sql, lookup);
                return lookup.Id;
            }
        });
    }

    /// <summary>
    /// Belirtilen tanımı veritabanından tamamen siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM Lookups WHERE Id = @Id", new { Id = id });
        });
    }
}
