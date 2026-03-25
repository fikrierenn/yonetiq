using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Çoklu sunucu bağlantılarını (DataSources) yönetir.
/// Bağlantı stringleri AES-256 ile şifreli olarak saklanır.
/// Kayıt yönetimi ana uygulama veritabanı üzerinden yapılır.
/// </summary>
public class DataSourceService : BaseService
{
    private readonly IConfiguration _cfg;

    public DataSourceService(IConfiguration config, AuditService auditService)
        : base(config, auditService) => _cfg = config;

    private string EncryptionKey => _cfg["DataSourceEncryptionKey"]
        ?? throw new InvalidOperationException("[DataSourceService] 'DataSourceEncryptionKey' yapılandırılmamış.");

    // ─── Listeleme ve Okuma ───────────────────────────────────────────────────

    /// <summary>
    /// Tüm aktif veri kaynaklarını listeler. Bağlantı stringleri şifreli döner (güvenlik).
    /// </summary>
    public async Task<ServiceResult<List<DataSource>>> GetAllAsync(bool onlyActive = false)
    {
        return await ExecuteServiceAsync<List<DataSource>>(async conn =>
        {
            var sql = onlyActive
                ? "SELECT Id, Name, Description, ServerType, IsDefault, IsActive, CreatedByUserId, CreatedAt, UpdatedAt FROM DataSources WHERE IsActive = 1 ORDER BY IsDefault DESC, Name"
                : "SELECT Id, Name, Description, ServerType, IsDefault, IsActive, CreatedByUserId, CreatedAt, UpdatedAt FROM DataSources ORDER BY IsDefault DESC, Name";

            return (await conn.QueryAsync<DataSource>(sql)).ToList();
        });
    }

    /// <summary>
    /// ID'si verilen veri kaynağını getirir. Bağlantı dizisi çözülmüş halde döner.
    /// </summary>
    public async Task<ServiceResult<DataSource?>> GetByIdAsync(int id)
    {
        return await ExecuteServiceAsync<DataSource?>(async conn =>
        {
            var ds = await conn.QueryFirstOrDefaultAsync<DataSource>(@"
                SELECT Id, Name, Description, ServerType, EncryptedConnectionString, IsDefault, IsActive, CreatedByUserId, CreatedAt, UpdatedAt
                FROM DataSources WHERE Id = @Id", new { Id = id });

            if (ds is null) return null;

            // Formda göstermek üzere bağlantı dizisini çöz
            try { ds.PlainConnectionString = AesEncryption.Decrypt(ds.EncryptedConnectionString, EncryptionKey); }
            catch (Exception decEx)
            {
                // Şifre çözme hatası sessizce yutulmamalı — form boş gösterir, arka planda loglayın
                Console.Error.WriteLine($"[DataSourceService] AES deşifreleme hatası (DataSource Id={id}): {decEx.Message}");
                ds.PlainConnectionString = string.Empty;
            }

            return ds;
        });
    }

    /// <summary>
    /// Verilen DataSourceId için bağlantı dizisi ve sunucu tipini çözer.
    /// dataSourceId null ise uygulamanın varsayılan bağlantısı döner.
    /// </summary>
    public async Task<(string ConnectionString, string ServerType)> GetConnectionInfoAsync(int? dataSourceId)
    {
        if (dataSourceId is null)
            return (ConnectionString, "MSSQL");

        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();

            var row = await conn.QueryFirstOrDefaultAsync<(string Enc, string Type)>(@"
                SELECT EncryptedConnectionString, ServerType
                FROM DataSources WHERE Id = @Id AND IsActive = 1", new { Id = dataSourceId });

            if (row == default)
                return (ConnectionString, "MSSQL"); // Kayıt yoksa fallback

            var plain = AesEncryption.Decrypt(row.Enc, EncryptionKey);
            return (plain, row.Type);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DataSourceService] GetConnectionInfoAsync hata: {ex.Message}");
            return (ConnectionString, "MSSQL");
        }
    }

    // ─── Kayıt ve Silme ───────────────────────────────────────────────────────

    /// <summary>
    /// Veri kaynağını ekler veya günceller.
    /// PlainConnectionString şifrelerek EncryptedConnectionString'e yazılır.
    /// IsDefault = true ise diğer tüm kayıtlar false yapılır (tekil varsayılan kuralı).
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(DataSource ds)
    {
        if (string.IsNullOrWhiteSpace(ds.PlainConnectionString))
            return ServiceResult<int>.Failure("Bağlantı dizisi boş olamaz.");

        // Bağlantı dizisini şifrele
        try { ds.EncryptedConnectionString = AesEncryption.Encrypt(ds.PlainConnectionString, EncryptionKey); }
        catch (Exception ex) { return ServiceResult<int>.Failure($"Şifreleme hatası: {ex.Message}"); }

        ds.UpdatedAt = DateTime.UtcNow;

        return await ExecuteServiceAsync<int>(async conn =>
        {
            // Tekil varsayılan kuralı: bu kayıt varsayılan yapılıyorsa diğerlerini sıfırla
            if (ds.IsDefault)
                await conn.ExecuteAsync("UPDATE DataSources SET IsDefault = 0, UpdatedAt = GETUTCDATE()");

            int resultId;
            if (ds.Id == 0)
            {
                resultId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO DataSources
                        (Name, Description, ServerType, EncryptedConnectionString, IsDefault, IsActive, CreatedByUserId, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Description, @ServerType, @EncryptedConnectionString, @IsDefault, @IsActive, @CreatedByUserId, @CreatedAt, @UpdatedAt)",
                    new
                    {
                        ds.Name, ds.Description, ds.ServerType, ds.EncryptedConnectionString,
                        ds.IsDefault, ds.IsActive, ds.CreatedByUserId,
                        CreatedAt = DateTime.UtcNow, ds.UpdatedAt
                    });
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE DataSources SET
                        Name = @Name, Description = @Description, ServerType = @ServerType,
                        EncryptedConnectionString = @EncryptedConnectionString,
                        IsDefault = @IsDefault, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id",
                    new
                    {
                        ds.Id, ds.Name, ds.Description, ds.ServerType, ds.EncryptedConnectionString,
                        ds.IsDefault, ds.IsActive, ds.UpdatedAt
                    });
                resultId = ds.Id;
            }

            LogAction("DataSource", ds.Id == 0 ? "Add" : "Update",
                new { DataSourceId = resultId, Name = ds.Name, ServerType = ds.ServerType });
            return resultId;
        });
    }

    /// <summary>
    /// Veri kaynağını siler. Varsayılan kayıt silinemez.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var isDefault = await conn.ExecuteScalarAsync<bool>(
                "SELECT IsDefault FROM DataSources WHERE Id = @Id", new { Id = id });

            if (isDefault)
                throw new InvalidOperationException("Varsayılan veri kaynağı silinemez. Önce başka bir kaynağı varsayılan yapın.");

            await conn.ExecuteAsync("DELETE FROM DataSources WHERE Id = @Id", new { Id = id });
            LogAction("DataSource", "Delete", new { DataSourceId = id });
        });
    }

    // ─── Bağlantı Testi ───────────────────────────────────────────────────────

    /// <summary>
    /// Verilen açık metin bağlantı dizisiyle gerçek bir bağlantı kurmayı dener.
    /// </summary>
    public static Task<(bool Success, string? Error)> TestConnectionAsync(string serverType, string plainConnStr)
        => ConnectionFactory.TestConnectionAsync(serverType, plainConnStr);
}
