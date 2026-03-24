using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// .sql dosyalarından stored procedure yükleyip veritabanında oluşturur.
/// Data/StoredProcedures/ klasöründeki dosyaları okur, GO/DROP bloklarını temizler
/// ve CREATE OR ALTER PROCEDURE olarak çalıştırır.
/// </summary>
public static partial class StoredProcedureLoader
{
    private static readonly string SpFolder = Path.Combine(
        AppContext.BaseDirectory, "Data", "StoredProcedures");

    /// <summary>
    /// Belirtilen SP'yi .sql dosyasından okuyup veritabanında oluşturur/günceller.
    /// </summary>
    public static async Task LoadAndExecuteAsync(SqlConnection conn, string spName)
    {
        var filePath = Path.Combine(SpFolder, $"{spName}.sql");
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[SP Loader] UYARI: {spName}.sql bulunamadı, atlanıyor.");
            return;
        }

        var rawSql = await File.ReadAllTextAsync(filePath);
        var createSql = ExtractCreateBlock(rawSql);

        if (string.IsNullOrWhiteSpace(createSql))
        {
            Console.WriteLine($"[SP Loader] UYARI: {spName}.sql içinde CREATE PROCEDURE bloğu bulunamadı.");
            return;
        }

        // CREATE PROCEDURE → CREATE OR ALTER PROCEDURE dönüşümü
        createSql = CreateToCreateOrAlter().Replace(createSql, "CREATE OR ALTER PROCEDURE");

        await conn.ExecuteAsync(createSql);
    }

    /// <summary>
    /// StoredProcedures klasöründeki tüm .sql dosyalarını yükler.
    /// </summary>
    public static async Task LoadAllAsync(SqlConnection conn)
    {
        if (!Directory.Exists(SpFolder))
        {
            Console.WriteLine($"[SP Loader] UYARI: {SpFolder} klasörü bulunamadı.");
            return;
        }

        var files = Directory.GetFiles(SpFolder, "*.sql");
        foreach (var file in files.OrderBy(f => f))
        {
            var spName = Path.GetFileNameWithoutExtension(file);
            await LoadAndExecuteAsync(conn, spName);
        }

        Console.WriteLine($"[SP Loader] {files.Length} SP yüklendi.");
    }

    /// <summary>
    /// SQL dosyasından CREATE PROCEDURE ... END bloğunu çıkarır.
    /// Yorum satırları, DROP PROCEDURE ve GO ifadelerini atlar.
    /// İç içe BEGIN/END bloklarını doğru şekilde işler.
    /// </summary>
    private static string ExtractCreateBlock(string rawSql)
    {
        var lines = rawSql.Split('\n');
        var inCreate = false;
        var depth = 0;
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            // GO satırlarını atla
            if (trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("GO\r", StringComparison.OrdinalIgnoreCase))
                continue;

            // CREATE PROCEDURE başlangıcı
            if (!inCreate && trimmed.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                inCreate = true;
                depth = 0;
                result.Add(line);
                continue;
            }

            if (inCreate)
            {
                result.Add(line);

                // BEGIN derinliğini takip et
                if (trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase) &&
                    (trimmed.Length == 5 || !char.IsLetterOrDigit(trimmed[5])))
                    depth++;

                // END derinliğini azalt
                if (trimmed.Equals("END", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("END\r", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("END;", StringComparison.OrdinalIgnoreCase))
                {
                    depth--;
                    if (depth <= 0)
                        break;
                }
            }
        }

        return string.Join('\n', result);
    }

    [GeneratedRegex(@"CREATE\s+PROCEDURE", RegexOptions.IgnoreCase)]
    private static partial Regex CreateToCreateOrAlter();
}
