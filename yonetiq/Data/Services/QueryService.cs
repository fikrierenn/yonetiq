using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Dinamik SQL sorgularını çalıştırma, rapor favorileri ve paylaşım işlemlerini yöneten servis sınıfıdır.
/// Çoklu sunucu desteği: her sorgu kendi DataSourceId'sine göre doğru bağlantıyı açar.
/// </summary>
public class QueryService(IConfiguration config, AuditService auditService, DataSourceService dataSourceSvc) : BaseService(config, auditService)
{
    /// <summary>
    /// QueryRecords tablosunda 'AllowDml' kolonunun olup olmadığını kontrol eder.
    /// </summary>
    private async Task<bool> HasAllowDmlColumnAsync(SqlConnection conn)
    {
        var result = await conn.ExecuteScalarAsync<int>(@"
            SELECT CASE
                WHEN COL_LENGTH('dbo.QueryRecords', 'AllowDml') IS NULL THEN 0
                ELSE 1
            END");
        return result == 1;
    }

    /// <summary>
    /// Sorgu kayıtları tablosunda 'UsagePurposeLookupId' kolonunun olup olmadığını kontrol eder.
    /// </summary>
    private async Task<bool> HasUsagePurposeColumnAsync(SqlConnection conn)
    {
        var result = await conn.ExecuteScalarAsync<int>(@"
            SELECT CASE
                WHEN COL_LENGTH('dbo.QueryRecords', 'UsagePurposeLookupId') IS NULL THEN 0
                ELSE 1
            END");
        return result == 1;
    }

    // DDL komutları — AllowDml=true olsa bile her zaman engellenir
    // (geçici tablo CREATE/DROP kalıpları kontrol öncesinde sıyrılır — bkz. StripTempTablePatterns)
    private static readonly string[] _ddlKeywords =
        ["DROP ", "TRUNCATE ", "ALTER ", "CREATE ", "EXEC ", "EXECUTE ", "GRANT ", "REVOKE ", "xp_"];

    // DML komutları — AllowDml=false ise engellenir, true ise geçişe izin verilir
    private static readonly string[] _dmlKeywords =
        ["INSERT ", "UPDATE ", "DELETE ", "MERGE "];

    // Geçici tablo kalıpları (session-scoped): CREATE TABLE #/## ve DROP TABLE (IF EXISTS) #/##
    // Bu kalıplar DDL kontrolünden önce kaldırılır çünkü şema değişikliği yaratmaz.
    private static readonly Regex _rexCreateTemp =
        new(@"CREATE\s+TABLE\s+##?\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _rexDropTemp =
        new(@"DROP\s+TABLE\s+(IF\s+EXISTS\s+)?##?\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// DDL güvenlik kontrolü yapılmadan önce SQL'deki geçici tablo (CREATE TABLE #, DROP TABLE #)
    /// ifadelerini boşlukla değiştirir. Bu sayede geçici tablo kullanan meşru sorgular
    /// yanlışlıkla bloklanmaz.
    /// </summary>
    private static string StripTempTablePatterns(string sql)
    {
        sql = _rexCreateTemp.Replace(sql, " ");
        sql = _rexDropTemp.Replace(sql,   " ");
        return sql;
    }

    /// <summary>
    /// SQL sorgusundaki @ParamAdi formatındaki kullanıcı parametrelerini tespit eder.
    /// @@ROWCOUNT gibi sistem değişkenlerini hariç tutar.
    /// </summary>
    public static List<string> DetectParameters(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return [];

        // @@... sistem değişkenlerini atlayarak @ParamAdi kalıbını bul
        var matches = Regex.Matches(sql, @"(?<!@)@([A-Za-z_][A-Za-z0-9_]*)");

        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    /// <summary>
    /// SQL sorgusunu analiz ederek her parametre için tip ve seçenek bilgilerini çıkarır.
    ///
    /// Desteklenen kaynak formatlar:
    ///   DECLARE @Param NVARCHAR(50) = 'default'  → tip + varsayılan değer
    ///   -- @Param: options=Seçenek1,Seçenek2      → dropdown seçenekleri
    ///   -- @Param: options=A,B default=A           → dropdown + başlangıç değeri
    /// </summary>
    public static List<QueryParameterInfo> ParseParameterInfo(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return [];

        // 1. Tüm @Param adlarını bul
        var names = DetectParameters(sql);
        if (names.Count == 0) return [];

        var result = new Dictionary<string, QueryParameterInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            result[name] = new QueryParameterInfo { Name = name };

        // 2. DECLARE satırlarından tip + varsayılan değer parse et
        //    DECLARE @Ad TIP[(boyut)] [= 'default']
        var rexDeclare = new Regex(
            @"DECLARE\s+@(?<n>[A-Za-z_]\w*)\s+(?<t>[A-Za-z]+)(?:\s*\([^)]*\))?(?:\s*=\s*(?<d>[^\n,;]+))?",
            RegexOptions.IgnoreCase);

        foreach (Match m in rexDeclare.Matches(sql))
        {
            var name    = m.Groups["n"].Value;
            var sqlType = m.Groups["t"].Value.ToUpperInvariant();
            var defVal  = m.Groups["d"].Success
                ? m.Groups["d"].Value.Trim().Trim('\'', '"', ' ')
                : string.Empty;

            if (!result.TryGetValue(name, out var info)) continue;

            info.SqlType      = sqlType;
            info.DefaultValue = defVal;
            info.Value        = defVal;
            info.InputType    = SqlTypeToInputType(sqlType);
        }

        // 3. Yorum satırlarından seçenek listesi parse et
        //    -- @Param: options=A,B,C  veya -- @Param: options=A,B default=A
        var rexComment = new Regex(
            @"--\s*@(?<n>[A-Za-z_]\w*)\s*:\s*options=(?<opts>[^\n]+)",
            RegexOptions.IgnoreCase);

        foreach (Match m in rexComment.Matches(sql))
        {
            var name    = m.Groups["n"].Value;
            var optRaw  = m.Groups["opts"].Value;

            if (!result.TryGetValue(name, out var info)) continue;

            // default=X anahtar kelimesini ayır
            var defaultMatch = Regex.Match(optRaw, @"\bdefault=(\S+)", RegexOptions.IgnoreCase);
            if (defaultMatch.Success)
            {
                info.DefaultValue = defaultMatch.Groups[1].Value.Trim('\'', '"');
                info.Value        = info.DefaultValue;
                optRaw = optRaw.Replace(defaultMatch.Value, "").Trim();
            }

            var opts = optRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => !o.StartsWith("default=", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (opts.Count > 0)
            {
                info.InputType = "select";
                info.Options   = opts;
                if (string.IsNullOrEmpty(info.Value) && opts.Count > 0)
                    info.Value = opts[0];
            }
        }

        // SQL'deki görünüş sırasını koru
        return names
            .Select(n => result.TryGetValue(n, out var v) ? v : null)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();
    }

    /// <summary>SQL veri tipini UI input tipine dönüştürür.</summary>
    private static string SqlTypeToInputType(string sqlType) => sqlType.ToUpperInvariant() switch
    {
        "DATE"          => "date",
        "DATETIME"      => "datetime",
        "DATETIME2"     => "datetime",
        "SMALLDATETIME" => "datetime",
        "INT"           => "number",
        "BIGINT"        => "number",
        "SMALLINT"      => "number",
        "TINYINT"       => "number",
        "DECIMAL"       => "number-decimal",
        "NUMERIC"       => "number-decimal",
        "FLOAT"         => "number-decimal",
        "REAL"          => "number-decimal",
        "MONEY"         => "number-decimal",
        "SMALLMONEY"    => "number-decimal",
        "BIT"           => "yesno",
        _               => "text"
    };

    /// <summary>
    /// Verilen DataSource bilgisine göre doğru IDbConnection oluşturur.
    /// dataSourceId null ise uygulamanın varsayılan bağlantısı kullanılır.
    /// </summary>
    private async Task<IDbConnection> CreateTargetConnAsync(int? dataSourceId)
    {
        var (connStr, serverType) = await dataSourceSvc.GetConnectionInfoAsync(dataSourceId);
        return ConnectionFactory.Create(serverType, connStr);
    }

    /// <summary>
    /// Verilen SQL sorgusunu asenkron olarak çalıştırır ve sonuçları QueryResult nesnesi olarak döner.
    /// DDL komutları her zaman engellenir. DML komutları (INSERT/UPDATE/DELETE/MERGE) yalnızca
    /// <paramref name="allowDml"/> = true olduğunda geçişe izin verilir.
    /// </summary>
    /// <param name="sql">Çalıştırılacak SQL sorgusu.</param>
    /// <param name="timeoutSeconds">Maksimum çalışma süresi (saniye).</param>
    /// <param name="parameters">Sorgudaki @ParamAdi değişkenleri için kullanıcı değerleri (null ise parametresiz çalışır).</param>
    /// <param name="dataSourceId">Kullanılacak veri kaynağı ID'si. Null ise varsayılan bağlantı kullanılır.</param>
    /// <param name="allowDml">True ise INSERT/UPDATE/DELETE/MERGE komutlarına izin verilir (DML/Uzman Modu).</param>
    public async Task<QueryResult> ExecuteAsync(
        string sql,
        int timeoutSeconds = 30,
        Dictionary<string, string>? parameters = null,
        int? dataSourceId = null,
        bool allowDml = false)
    {
        var result = new QueryResult();
        if (string.IsNullOrWhiteSpace(sql))
        {
            result.ErrorMessage = "SQL sorgusu boş olamaz.";
            return result;
        }

        // DDL anahtar kelime kontrolü — her zaman engellenir.
        // İstisna: CREATE TABLE #temp / DROP TABLE #temp (session-scoped geçici tablo)
        // Kontrol öncesinde geçici tablo kalıpları SQL'den sıyrılır.
        var upperSql = sql.ToUpperInvariant();
        var sqlForDdlCheck = StripTempTablePatterns(upperSql);
        var dangerousDdl = _ddlKeywords.FirstOrDefault(k => sqlForDdlCheck.Contains(k.ToUpperInvariant()));
        if (dangerousDdl != null)
        {
            result.ErrorMessage = $"Güvenlik kısıtlaması: '{dangerousDdl.Trim()}' komutu dinamik sorgu ekranında kullanılamaz.";
            return result;
        }

        // DML anahtar kelime kontrolü — AllowDml=false ise engellenir
        if (!allowDml)
        {
            var dangerousDml = _dmlKeywords.FirstOrDefault(k => upperSql.Contains(k.ToUpperInvariant()));
            if (dangerousDml != null)
            {
                result.ErrorMessage = $"Güvenlik kısıtlaması: '{dangerousDml.Trim()}' komutu izin verilmeyen komutlar arasında. DML/Uzman Modunu etkinleştirin.";
                return result;
            }
        }

        try
        {
            using var conn = await CreateTargetConnAsync(dataSourceId);
            conn.Open();

            // Kullanıcı tarafından girilen parametreleri Dapper DynamicParameters'a ekle.
            // SQL içinde zaten DECLARE @Param yapılan değişkenler hariç tutulur;
            // aksi hâlde "değişken adı zaten bildirilmiş" hatası oluşur.
            DynamicParameters? dp = null;
            if (parameters is { Count: > 0 })
            {
                // SQL'de DECLARE edilen parametre adlarını tespit et (büyük/küçük harf duyarsız)
                var declaredInSql = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match m in Regex.Matches(sql, @"DECLARE\s+@(\w+)", RegexOptions.IgnoreCase))
                    declaredInSql.Add(m.Groups[1].Value);

                // Yalnızca DECLARE ile bildirilmemiş parametreleri Dapper'a ekle
                var externalParams = parameters
                    .Where(kv => !declaredInSql.Contains(kv.Key))
                    .ToList();

                if (externalParams.Count > 0)
                {
                    dp = new DynamicParameters();
                    foreach (var (key, val) in externalParams)
                        dp.Add(key, val);
                }
            }

            var rows = await conn.QueryAsync(sql, dp, commandTimeout: timeoutSeconds);
            var list = rows
                .Select(row => ((IDictionary<string, object>)row)
                    .ToDictionary(kv => kv.Key, kv => (object?)kv.Value))
                .ToList();

            result.Rows = list;
            result.Columns = list.Count > 0 ? list[0].Keys.ToList() : [];
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Veritabanındaki tablo şemalarını (Tablo Adı - Kolon Listesi) döner.
    /// dataSourceId ile hedef sunucu seçilebilir; null ise varsayılan bağlantı kullanılır.
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, List<string>>>> GetTableSchemaAsync(int? dataSourceId = null)
    {
        try
        {
            var (connStr, serverType) = await dataSourceSvc.GetConnectionInfoAsync(dataSourceId);
            using var conn = ConnectionFactory.Create(serverType, connStr);
            conn.Open();

            // INFORMATION_SCHEMA.COLUMNS üç DB tipinde de (MSSQL/PG/MySQL) çalışır
            // PG ve MySQL iç sistem şemalarını filtrele
            var schemaSql = serverType.ToUpperInvariant() switch
            {
                "POSTGRESQL" or "POSTGRES" or "PG" => @"
                    SELECT table_name AS TABLE_NAME, column_name AS COLUMN_NAME
                    FROM information_schema.columns
                    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                    ORDER BY table_name, ordinal_position",
                "MYSQL" or "MARIADB" => @"
                    SELECT TABLE_NAME, COLUMN_NAME
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    ORDER BY TABLE_NAME, ORDINAL_POSITION",
                _ => @"
                    SELECT TABLE_NAME, COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME NOT LIKE '__EF%'
                    ORDER BY TABLE_NAME, ORDINAL_POSITION"
            };

            var rows = await conn.QueryAsync<(string TABLE_NAME, string COLUMN_NAME)>(schemaSql);
            var schema = rows
                .GroupBy(x => x.TABLE_NAME)
                .ToDictionary(g => g.Key, g => g.Select(x => x.COLUMN_NAME).ToList());

            return ServiceResult<Dictionary<string, List<string>>>.Success(schema);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[QueryService] GetTableSchemaAsync hata: {ex.Message}");
            return ServiceResult<Dictionary<string, List<string>>>.Failure($"Şema yüklenemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// Veritabanındaki tüm kayıtlı SQL sorgularını listeler.
    /// </summary>
    public async Task<ServiceResult<List<QueryRecord>>> GetAllQueriesAsync()
    {
        return await ExecuteServiceAsync<List<QueryRecord>>(async conn =>
        {
            var hasUsagePurpose = await HasUsagePurposeColumnAsync(conn);
            var hasAllowDml    = await HasAllowDmlColumnAsync(conn);
            var sql = @"
                SELECT Id, Name, Description, SqlContent, ResultType,
                        " + (hasUsagePurpose ? "UsagePurposeLookupId" : "NULL AS UsagePurposeLookupId") + @",
                        Color, PivotRowColumn, PivotColumnColumn, PivotValueColumn, DataSourceId, UpdatedAt,
                        " + (hasAllowDml ? "AllowDml" : "CAST(0 AS BIT) AS AllowDml") + @"
                FROM QueryRecords
                ORDER BY Name";

            return (await conn.QueryAsync<QueryRecord>(sql)).ToList();
        });
    }

    /// <summary>
    /// Belirli bir kullanım amacına (Dashboard, Rapor vb.) göre filtrelenmiş sorguları döner.
    /// </summary>
    public async Task<ServiceResult<List<QueryRecord>>> GetQueriesByPurposeAsync(params int[] purposeLookupIds)
    {
        var result = await GetAllQueriesAsync();
        if (!result.IsSuccess || result.Data == null) return result;

        if (purposeLookupIds == null || purposeLookupIds.Length == 0)
        {
            return result;
        }

        var filterSet = purposeLookupIds.ToHashSet();
        var list = result.Data.Where(q => q.UsagePurposeLookupId.HasValue && filterSet.Contains(q.UsagePurposeLookupId.Value)).ToList();
        return ServiceResult<List<QueryRecord>>.Success(list);
    }

    /// <summary>
    /// ID'si verilen tek bir sorgu kaydını getirir.
    /// </summary>
    public async Task<ServiceResult<QueryRecord?>> GetQueryAsync(int id)
    {
        return await ExecuteServiceAsync<QueryRecord?>(async conn =>
        {
            var hasUsagePurpose = await HasUsagePurposeColumnAsync(conn);
            var hasAllowDml    = await HasAllowDmlColumnAsync(conn);
            var sql = @"
                SELECT Id, Name, Description, SqlContent, ResultType,
                        " + (hasUsagePurpose ? "UsagePurposeLookupId" : "NULL AS UsagePurposeLookupId") + @",
                        Color, PivotRowColumn, PivotColumnColumn, PivotValueColumn, DataSourceId, UpdatedAt,
                        " + (hasAllowDml ? "AllowDml" : "CAST(0 AS BIT) AS AllowDml") + @"
                FROM QueryRecords
                WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<QueryRecord>(sql, new { Id = id });
        });
    }

    /// <summary>
    /// Mevcut bir sorguyu günceller veya yeni bir tane oluşturur.
    /// </summary>
    public async Task<ServiceResult<int>> SaveQueryAsync(QueryRecord q)
    {
        q.UpdatedAt = DateTime.UtcNow;

        return await ExecuteServiceAsync<int>(async conn =>
        {
            int resultId;
            var hasUsagePurpose = await HasUsagePurposeColumnAsync(conn);
            var hasAllowDml     = await HasAllowDmlColumnAsync(conn);
            if (q.Id == 0)
            {
                var allowDmlCol = hasAllowDml ? ", AllowDml" : "";
                var allowDmlVal = hasAllowDml ? ", @AllowDml" : "";
                var sql = $@"
                    INSERT INTO QueryRecords
                        (Name, Description, SqlContent, ResultType, UsagePurposeLookupId, Color, PivotRowColumn, PivotColumnColumn, PivotValueColumn, DataSourceId, UpdatedAt{allowDmlCol})
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Description, @SqlContent, @ResultType, @UsagePurpose, @Color, @PivotRowColumn, @PivotColumnColumn, @PivotValueColumn, @DataSourceId, @UpdatedAt{allowDmlVal})";

                resultId = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    q.Name, q.Description, q.SqlContent,
                    ResultType = q.ResultType,
                    UsagePurpose = hasUsagePurpose ? q.UsagePurposeLookupId : null,
                    q.Color, q.PivotRowColumn, q.PivotColumnColumn, q.PivotValueColumn,
                    q.DataSourceId, q.UpdatedAt,
                    AllowDml = hasAllowDml ? q.AllowDml : (object?)null
                });
            }
            else
            {
                var sql = @"
                    UPDATE QueryRecords
                    SET Name = @Name,
                        Description = @Description,
                        SqlContent = @SqlContent,
                        ResultType = @ResultType,
                        " + (hasUsagePurpose ? "UsagePurposeLookupId = @UsagePurpose," : "") +
                        (hasAllowDml ? "AllowDml = @AllowDml," : "") + @"
                        Color = @Color,
                        PivotRowColumn = @PivotRowColumn,
                        PivotColumnColumn = @PivotColumnColumn,
                        PivotValueColumn = @PivotValueColumn,
                        DataSourceId = @DataSourceId,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                await conn.ExecuteAsync(sql, new
                {
                    q.Id, q.Name, q.Description, q.SqlContent,
                    ResultType = q.ResultType,
                    UsagePurpose = q.UsagePurposeLookupId,
                    q.Color, q.PivotRowColumn, q.PivotColumnColumn, q.PivotValueColumn,
                    q.DataSourceId, q.UpdatedAt,
                    AllowDml = q.AllowDml
                });
                resultId = q.Id;
            }

            var actionType = q.Id == 0 ? "Add" : "Update";
            LogAction("Query", actionType, new { QueryId = resultId, Name = q.Name, Type = q.ResultType });
            return resultId;
        });
    }

    /// <summary>
    /// Sorguyu sistemden siler.
    /// </summary>
    public async Task<ServiceResult> DeleteQueryAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM QueryRecords WHERE Id = @Id", new { Id = id });
            LogAction("Query", "Delete", new { QueryId = id });
        });
    }

    /// <summary>
    /// Aktif olan varsayılan kullanıcı ID'sini getirir.
    /// </summary>
    public async Task<ServiceResult<int?>> GetDefaultUserIdAsync()
    {
        return await ExecuteServiceAsync<int?>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 Id FROM Users WHERE IsActive = 1 ORDER BY Id");
        });
    }

    /// <summary>
    /// Kullanıcının favori rapor/sorgu ID'lerini döner.
    /// </summary>
    public async Task<ServiceResult<HashSet<int>>> GetFavoriteQueryIdsAsync(int userId)
    {
        return await ExecuteServiceAsync<HashSet<int>>(async conn =>
        {
            var ids = await conn.QueryAsync<int>(
                "SELECT QueryId FROM ReportFavorites WHERE UserId = @UserId",
                new { UserId = userId });
            return ids.ToHashSet();
        });
    }

    /// <summary>
    /// Bir raporu kullanıcının favorilerine ekler.
    /// </summary>
    public async Task<ServiceResult> AddFavoriteAsync(int userId, int queryId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM ReportFavorites WHERE UserId = @UserId AND QueryId = @QueryId)
                BEGIN
                    INSERT INTO ReportFavorites (UserId, QueryId) VALUES (@UserId, @QueryId)
                END", new { UserId = userId, QueryId = queryId });
        });
    }

    /// <summary>
    /// Bir raporu kullanıcının favorilerinden çıkarır.
    /// </summary>
    public async Task<ServiceResult> RemoveFavoriteAsync(int userId, int queryId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "DELETE FROM ReportFavorites WHERE UserId = @UserId AND QueryId = @QueryId",
                new { UserId = userId, QueryId = queryId });
        });
    }

    /// <summary>
    /// Kullanıcıya veya departmana özel paylaşılan rapor ID'lerini döner.
    /// </summary>
    public async Task<ServiceResult<HashSet<int>>> GetSharedQueryIdsAsync(int userId, string? departmentName = null)
    {
        return await ExecuteServiceAsync<HashSet<int>>(async conn =>
        {
            // Hem direkt kullanıcıya hem de departmana paylaşılanları getir
            var ids = await conn.QueryAsync<int>(@"
                SELECT DISTINCT QueryId
                FROM ReportShares
                WHERE TargetUserId = @UserId
                    OR TargetDepartmentLookupId IN (SELECT Id FROM Lookups WHERE Value = @DeptName AND [Group] = '" + LookupConstants.GroupDepartment + "')",
                new { UserId = userId, DeptName = departmentName });
            return ids.ToHashSet();
        });
    }

    /// <summary>
    /// Bir raporu kullanıcı veya departmanla paylaşır.
    /// </summary>
    public async Task<ServiceResult> ShareReportAsync(int queryId, int sharingUserId, int? targetUserId, string? targetDepartmentName, string? note = null)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            int? deptId = null;
            if (!string.IsNullOrWhiteSpace(targetDepartmentName))
            {
                deptId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT Id FROM Lookups WHERE Name = @Name", new { Name = targetDepartmentName });
            }

            await conn.ExecuteAsync(@"
                INSERT INTO ReportShares (QueryId, SharedByUserId, TargetUserId, TargetDepartmentLookupId, Note, CreatedAt)
                VALUES (@QueryId, @SharedByUserId, @TargetUserId, @TargetDepartmentLookupId, @Note, @CreatedAt)",
                new 
                { 
                    QueryId = queryId, 
                    SharedByUserId = sharingUserId, 
                    TargetUserId = targetUserId, 
                    TargetDepartmentLookupId = deptId, 
                    Note = note, 
                    CreatedAt = DateTime.UtcNow 
                });
        });
    }

    /// <summary>
    /// Bir paylaşımı iptal eder.
    /// </summary>
    public async Task<ServiceResult> UndoShareAsync(int shareId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM ReportShares WHERE Id = @Id", new { Id = shareId });
        });
    }

    /// <summary>
    /// Bir raporun kimlerle paylaşıldığını listeler.
    /// </summary>
    public async Task<ServiceResult<List<ReportShare>>> ListSharedWithAsync(int queryId, int sharingUserId)
    {
        return await ExecuteServiceAsync<List<ReportShare>>(async conn =>
        {
            return (await conn.QueryAsync<ReportShare>(@"
                SELECT Id, QueryId, SharedByUserId, TargetUserId, TargetDepartmentLookupId, Note, CreatedAt
                FROM ReportShares
                WHERE QueryId = @QueryId AND SharedByUserId = @SharedByUserId",
                new { QueryId = queryId, SharedByUserId = sharingUserId })).ToList();
        });
    }

    /// <summary>
    /// Bir QueryResult veri kümesini, verilen kolonlara göre pivot tablo formatına (X-Y-Z) dönüştürür.
    /// </summary>
    public static QueryResult Pivot(QueryResult source, string rowColumn, string colColumn, string valueColumn)
    {
        var result = new QueryResult();
        if (source.Rows.Count == 0) return result;

        var rows = source.Rows.Select(r => r.ContainsKey(rowColumn) ? r[rowColumn]?.ToString() ?? "N/A" : "N/A").Distinct().OrderBy(x => x).ToList();
        var cols = source.Rows.Select(r => r.ContainsKey(colColumn) ? r[colColumn]?.ToString() ?? "N/A" : "N/A").Distinct().OrderBy(x => x).ToList();

        result.Columns.Add(rowColumn);
        result.Columns.AddRange(cols);

        foreach (var rowVal in rows)
        {
            var newRow = new Dictionary<string, object?>();
            newRow[rowColumn] = rowVal;

            foreach (var colVal in cols)
            {
                var match = source.Rows.FirstOrDefault(r =>
                    (r.ContainsKey(rowColumn) ? r[rowColumn]?.ToString() : null) == rowVal &&
                    (r.ContainsKey(colColumn) ? r[colColumn]?.ToString() : null) == colVal);

                newRow[colVal] = match != null && match.ContainsKey(valueColumn) ? match[valueColumn] : 0;
            }
            result.Rows.Add(newRow);
        }

        return result;
    }

    /// <summary>
    /// ID ile kayıtlı bir sorguyu getirir ve çalıştırır. Worker servisi tarafından kullanılır.
    /// </summary>
    public async Task<ServiceResult<QueryResult>> RunQueryAsync(int queryRecordId, Dictionary<string, string>? parameters)
    {
        var queryResult = await GetQueryAsync(queryRecordId);
        if (!queryResult.IsSuccess || queryResult.Data is null)
            return ServiceResult<QueryResult>.Failure(queryResult.Message ?? "Sorgu bulunamadı.");

        var record = queryResult.Data;
        var result = await ExecuteAsync(record.SqlContent, 60, parameters, record.DataSourceId, record.AllowDml);
        if (result.ErrorMessage is not null)
            return ServiceResult<QueryResult>.Failure(result.ErrorMessage);

        return ServiceResult<QueryResult>.Success(result);
    }
}
