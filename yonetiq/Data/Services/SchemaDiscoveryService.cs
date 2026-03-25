using Dapper;
using System.Text.Json;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Bağlı DataSource'ların şemasını keşfeder.
/// Tablo listesi, kolon detayları, satır sayıları, örnek değerler.
/// Sonuçları schema_mapping.json'a yazar.
/// </summary>
public class SchemaDiscoveryService(
    IConfiguration config, AuditService auditSvc,
    DataSourceService dataSourceSvc,
    ILogger<SchemaDiscoveryService> logger)
    : BaseService(config, auditSvc)
{
    /// <summary>Verilen DataSource üzerinde tam şema keşfi yapar.</summary>
    public async Task<ServiceResult<SchemaDiscoveryResult>> DiscoverAsync(
        int dataSourceId, bool includeExamples = true)
    {
        try
        {
            var (connStr, serverType) = await dataSourceSvc.GetConnectionInfoAsync(dataSourceId);
            using var conn = ConnectionFactory.Create(serverType, connStr);
            conn.Open();

            var result = new SchemaDiscoveryResult
            {
                DataSourceId = dataSourceId,
                DiscoveredAt = DateTime.UtcNow,
                ServerName = conn.ConnectionString?.Split(';')
                    .FirstOrDefault(s => s.TrimStart().StartsWith("Server", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=').LastOrDefault()?.Trim() ?? "",
                DatabaseName = conn.Database ?? ""
            };

            // 1. Tablo listesi
            var tables = (await conn.QueryAsync<TableInfo>(@"
                SELECT
                    t.TABLE_SCHEMA AS SchemaName,
                    t.TABLE_NAME   AS TableName,
                    ISNULL(p.rows, 0) AS RowCount
                FROM INFORMATION_SCHEMA.TABLES t
                LEFT JOIN sys.partitions p
                    ON p.object_id = OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME)
                    AND p.index_id IN (0,1)
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                  AND t.TABLE_SCHEMA NOT IN ('sys','INFORMATION_SCHEMA')
                ORDER BY ISNULL(p.rows,0) DESC, t.TABLE_NAME")).ToList();

            result.Tables = tables;

            // 2. Her tablo için kolon detayları
            foreach (var table in tables)
            {
                var columns = (await conn.QueryAsync<ColumnInfo>(@"
                    SELECT
                        c.COLUMN_NAME    AS ColumnName,
                        c.DATA_TYPE      AS DataType,
                        c.IS_NULLABLE    AS IsNullable,
                        c.CHARACTER_MAXIMUM_LENGTH AS MaxLength
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    WHERE c.TABLE_NAME = @TableName
                    ORDER BY c.ORDINAL_POSITION",
                    new { table.TableName })).ToList();

                table.Columns = columns;

                // 3. Örnek değerler (string/date kolonlar, ilk 3 satır)
                if (includeExamples && table.RowCount > 0 && table.RowCount < 10_000_000)
                {
                    try
                    {
                        var sampleCols = columns
                            .Where(c => c.DataType is "nvarchar" or "varchar" or "date" or "datetime2" or "datetime")
                            .Take(5)
                            .Select(c => $"[{c.ColumnName}]");

                        if (sampleCols.Any())
                        {
                            var sampleSql = $"SELECT TOP 3 {string.Join(",", sampleCols)} FROM [{table.TableName}]";
                            var rows = (await conn.QueryAsync(sampleSql)).ToList();
                            table.SampleValues = rows.Select(r =>
                                ((IDictionary<string, object>)r)
                                .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")
                            ).ToList();
                        }
                    }
                    catch { /* Örnek alınamadıysa devam et */ }
                }
            }

            // 4. Aday tespitleri
            result.SalesCandidates = FindCandidates(tables,
                "sale", "satis", "invoice", "fatura", "order", "siparis", "receipt");
            result.ProductCandidates = FindCandidates(tables,
                "product", "urun", "item", "kitap", "book", "stock", "stok");
            result.StoreCandidates = FindCandidates(tables,
                "store", "magaza", "branch", "sube", "shop");

            logger.LogInformation(
                "Schema discovery: {DB}, {Tables} tablo, {Sales} satış adayı, {Products} ürün adayı",
                result.DatabaseName, tables.Count,
                result.SalesCandidates.Count, result.ProductCandidates.Count);

            return ServiceResult<SchemaDiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema discovery failed for DataSource {Id}", dataSourceId);
            return ServiceResult<SchemaDiscoveryResult>.Failure($"Şema keşfi başarısız: {ex.Message}");
        }
    }

    /// <summary>Keşif sonucunu soru listesine dönüştürür.</summary>
    public List<SchemaQuestion> GenerateQuestions(SchemaDiscoveryResult discovery)
    {
        var questions = new List<SchemaQuestion>();

        // Satış tablosu
        if (discovery.SalesCandidates.Count == 0)
        {
            questions.Add(new SchemaQuestion
            {
                Id = "sales_table", Priority = QuestionPriority.Critical,
                Question = "Satış/ciro verisi hangi tabloda? Hiç satış adayı bulunamadı.",
                Context = $"Tablolar: {string.Join(", ", discovery.Tables.Take(20).Select(t => t.TableName))}"
            });
        }
        else if (discovery.SalesCandidates.Count > 1)
        {
            questions.Add(new SchemaQuestion
            {
                Id = "sales_table", Priority = QuestionPriority.Critical,
                Question = $"Birden fazla satış adayı var. Hangisi ana satış tablosu?",
                Options = discovery.SalesCandidates
            });
        }

        // Tutar kolonu
        foreach (var salesTable in discovery.SalesCandidates)
        {
            var table = discovery.Tables.FirstOrDefault(t => t.TableName == salesTable);
            if (table is null) continue;

            var amountCols = table.Columns
                .Where(c => c.ColumnName.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                             c.ColumnName.Contains("tutar", StringComparison.OrdinalIgnoreCase) ||
                             c.ColumnName.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                             c.ColumnName.Contains("total", StringComparison.OrdinalIgnoreCase) ||
                             c.ColumnName.Contains("ciro", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ColumnName).ToList();

            if (amountCols.Count != 1)
            {
                questions.Add(new SchemaQuestion
                {
                    Id = $"amount_col_{salesTable}", Priority = QuestionPriority.Critical,
                    Question = $"{salesTable}: Net satış tutarı (ciro) hangi kolonda? KDV dahil mi, hariç mi?",
                    Options = amountCols.Count > 0 ? amountCols : null,
                    Context = $"Kolonlar: {string.Join(", ", table.Columns.Select(c => c.ColumnName))}"
                });
            }

            // Tarih kolonu
            var dateCols = table.Columns
                .Where(c => c.DataType is "date" or "datetime" or "datetime2")
                .Select(c => c.ColumnName).ToList();

            if (dateCols.Count > 1)
            {
                questions.Add(new SchemaQuestion
                {
                    Id = $"date_col_{salesTable}", Priority = QuestionPriority.Critical,
                    Question = $"{salesTable}: Rapor filtrelemede hangi tarih kolonu kullanılıyor?",
                    Options = dateCols
                });
            }
        }

        // İade ve kampanya soruları
        questions.Add(new SchemaQuestion
        {
            Id = "returns", Priority = QuestionPriority.Medium,
            Question = "İadeler nasıl kaydediliyor? (Negatif satır / ayrı tablo / net tutar iade düşülmüş)"
        });
        questions.Add(new SchemaQuestion
        {
            Id = "discount", Priority = QuestionPriority.Medium,
            Question = "Kampanya/indirim bilgisi var mı? (Ayrı kolon / sadece net fiyat / ayrı tablo)"
        });

        return questions.OrderByDescending(q => q.Priority).ToList();
    }

    /// <summary>Keşif sonucunu + cevapları schema_mapping.json'a kaydeder.</summary>
    public async Task SaveMappingAsync(
        SchemaDiscoveryResult discovery, List<SchemaAnswer> answers, string outputPath)
    {
        var mapping = new SchemaMapping
        {
            DataSourceId = discovery.DataSourceId,
            DatabaseName = discovery.DatabaseName,
            MappedAt = DateTime.UtcNow,
            Answers = answers,
            TableSummary = discovery.Tables.Select(t => new TableSummary
            {
                TableName = t.TableName,
                RowCount = t.RowCount,
                ColumnCount = t.Columns.Count
            }).ToList()
        };

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);
        logger.LogInformation("Schema mapping saved: {Path}", outputPath);
    }

    private static List<string> FindCandidates(List<TableInfo> tables, params string[] keywords)
    {
        return tables
            .Where(t => keywords.Any(k =>
                t.TableName.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.TableName)
            .ToList();
    }
}
