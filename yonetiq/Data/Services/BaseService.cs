using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using Dapper;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Tüm servislerin türetileceği temel servis sınıfıdır.
/// Veritabanı bağlantısı ve ortak hata yönetimini merkezi hale getirir.
/// </summary>
public abstract class BaseService(IConfiguration config, AuditService? auditService = null)
{
    protected string ConnectionString => config.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string not found.");

    protected SqlConnection CreateConn() => new(ConnectionString);

    /// <summary>
    /// Servis işlemlerini standart bir try-catch bloğu içinde çalıştırır.
    /// </summary>
    protected async Task<ServiceResult<T>> ExecuteServiceAsync<T>(Func<SqlConnection, Task<T>> action)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            var data = await action(conn);
            return ServiceResult<T>.Success(data);
        }
        catch (SqlException ex)
        {
            return ServiceResult<T>.Failure($"Veritabani hatasi: {ex.Message}", ex.Number.ToString());
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure($"Sistem hatasi: {ex.Message}");
        }
    }

    /// <summary>
    /// Geriye değer dönmeyen servis işlemlerini çalıştırır.
    /// </summary>
    protected async Task<ServiceResult> ExecuteServiceAsync(Func<SqlConnection, Task> action)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            await action(conn);
            return ServiceResult.Success();
        }
        catch (SqlException ex)
        {
            return ServiceResult.Failure($"Veritabani hatasi: {ex.Message}", ex.Number.ToString());
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"Sistem hatasi: {ex.Message}");
        }
    }

    /// <summary>
    /// Audit log kaydeder.
    /// </summary>
    protected void LogAction(string module, string action, object? detail = null)
    {
        if (auditService != null)
        {
            _ = auditService.SaveLogAsync(module, action, detail);
        }
    }

    /// <summary>
    /// SQL Fallback mekanizması için kontrol sağlar.
    /// </summary>
    protected bool IsCompatibleWithFallback(SqlException ex) =>
        ex.Number is 2812 or 208 or 201 or 8144 or 207;
}
