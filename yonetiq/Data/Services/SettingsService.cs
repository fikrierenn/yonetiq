using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// SystemSettings tablosundaki uygulama geneli ayarları yöneten servis.
/// Hassas değerler (şifre, token) AES-256 ile şifreli olarak saklanır.
/// </summary>
#pragma warning disable CS9107
public class SettingsService(IConfiguration config, AuditService auditService)
    : BaseService(config, auditService)
{
    private string EncryptionKey => config["DataSourceEncryptionKey"]
        ?? throw new InvalidOperationException("DataSourceEncryptionKey yapılandırılmamış.");

    /// <summary>
    /// Verilen anahtara ait değeri döndürür. IsEncrypted=1 ise AES ile çözer.
    /// Anahtar bulunamazsa null döner.
    /// </summary>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            var row = await conn.QueryFirstOrDefaultAsync<(string? Value, bool IsEncrypted)>(
                "SELECT [Value], [IsEncrypted] FROM SystemSettings WHERE [Key] = @Key",
                new { Key = key });

            if (row.Value is null) return null;
            if (row.IsEncrypted)
            {
                try { return AesEncryption.Decrypt(row.Value, EncryptionKey); }
                catch { return null; } // Bozuk şifreli veri — null döner
            }
            return row.Value;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SettingsService.GetAsync] Hata: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Belirtilen gruba ait tüm anahtarları döndürür. Şifreli değerler çözülür.
    /// </summary>
    public async Task<Dictionary<string, string?>> GetGroupAsync(string group)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            var rows = await conn.QueryAsync<(string Key, string? Value, bool IsEncrypted)>(
                "SELECT [Key], [Value], [IsEncrypted] FROM SystemSettings WHERE [Group] = @Group",
                new { Group = group });

            return rows.ToDictionary(
                r => r.Key,
                r =>
                {
                    if (r.Value is null || !r.IsEncrypted) return r.Value;
                    try { return AesEncryption.Decrypt(r.Value, EncryptionKey); }
                    catch { return null; }
                });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SettingsService.GetGroupAsync] Hata: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Bir ayarı kaydeder. IsEncrypted=1 olan anahtarlar otomatik şifrelenir.
    /// </summary>
    public async Task<ServiceResult> SetAsync(string key, string? value)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            // Şifreleme gerekip gerekmediğini kontrol et
            var isEncrypted = await conn.ExecuteScalarAsync<bool>(
                "SELECT [IsEncrypted] FROM SystemSettings WHERE [Key] = @Key",
                new { Key = key });

            var storedValue = (isEncrypted && value is not null)
                ? AesEncryption.Encrypt(value, EncryptionKey)
                : value;

            await conn.ExecuteAsync(@"
                MERGE SystemSettings AS target
                USING (SELECT @Key AS [Key]) AS source ON target.[Key] = source.[Key]
                WHEN MATCHED    THEN UPDATE SET [Value] = @Value, UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN INSERT ([Key],[Value],UpdatedAt) VALUES (@Key,@Value,SYSUTCDATETIME());",
                new { Key = key, Value = storedValue });

            LogAction("Settings", "Set", new { Key = key });
        });
    }

    /// <summary>
    /// Bir gruptaki tüm ayarları toplu kaydeder.
    /// </summary>
    public async Task<ServiceResult> SetGroupAsync(Dictionary<string, string?> values)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            // Şifreleme bilgilerini önceden çek
            var keys = values.Keys.ToArray();
            var encryptedKeys = (await conn.QueryAsync<string>(
                "SELECT [Key] FROM SystemSettings WHERE [Key] IN @Keys AND [IsEncrypted] = 1",
                new { Keys = keys })).ToHashSet();

            foreach (var (key, value) in values)
            {
                var storedValue = (encryptedKeys.Contains(key) && value is not null)
                    ? AesEncryption.Encrypt(value, EncryptionKey)
                    : value;

                await conn.ExecuteAsync(@"
                    MERGE SystemSettings AS target
                    USING (SELECT @Key AS [Key]) AS source ON target.[Key] = source.[Key]
                    WHEN MATCHED     THEN UPDATE SET [Value] = @Value, UpdatedAt = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN INSERT ([Key],[Value],UpdatedAt) VALUES (@Key,@Value,SYSUTCDATETIME());",
                    new { Key = key, Value = storedValue });
            }

            LogAction("Settings", "SetGroup", new { Keys = string.Join(",", keys) });
        });
    }

    /// <summary>
    /// Tüm ayarları gruplanmış olarak döndürür (yönetim UI için, değerler şifresiz).
    /// </summary>
    public async Task<List<SettingItem>> GetAllAsync()
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            var rows = await conn.QueryAsync<SettingItem>(@"
                SELECT [Key], [Value], [IsEncrypted], [Group], [Description], UpdatedAt
                FROM SystemSettings
                ORDER BY [Group], [Key]");

            var result = rows.ToList();
            // Şifreli değerleri çöz (UI için)
            foreach (var item in result.Where(r => r.IsEncrypted && r.Value is not null))
            {
                try { item.Value = AesEncryption.Decrypt(item.Value!, EncryptionKey); }
                catch { item.Value = null; }
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SettingsService.GetAllAsync] Hata: {ex.Message}");
            return [];
        }
    }
}

/// <summary>
/// SystemSettings tablosunun tek bir satırını temsil eder.
/// </summary>
public class SettingItem
{
    public string  Key         { get; set; } = string.Empty;
    public string? Value       { get; set; }
    public bool    IsEncrypted { get; set; }
    public string? Group       { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt  { get; set; }
}
