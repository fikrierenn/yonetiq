using System.Data;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Sunucu tipine göre uygun IDbConnection nesnesi üretir.
/// Dapper'ın IDbConnection arayüzüyle tam uyumludur — mevcut sorgular değişmez.
/// Desteklenen tipler: MSSQL, PostgreSQL, MySQL
/// </summary>
public static class ConnectionFactory
{
    /// <summary>
    /// Verilen sunucu tipi ve bağlantı dizisine göre yeni bir IDbConnection oluşturur.
    /// Bağlantı açılmaz; çağıran kod OpenAsync() çağırmalıdır.
    /// </summary>
    /// <param name="serverType">Sunucu tipi: "MSSQL", "PostgreSQL" veya "MySQL".</param>
    /// <param name="connectionString">Açık metin bağlantı dizisi.</param>
    /// <returns>Tipin gerektirdiği IDbConnection implementasyonu.</returns>
    public static IDbConnection Create(string serverType, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return serverType?.ToUpperInvariant() switch
        {
            "POSTGRESQL" or "POSTGRES" or "PG" => new NpgsqlConnection(connectionString),
            "MYSQL" or "MARIADB"               => new MySqlConnection(connectionString),
            _                                  => new SqlConnection(connectionString) // MSSQL varsayılan
        };
    }

    /// <summary>
    /// Verilen sunucu tipinin geçerli bir bağlantı kurabildiğini test eder.
    /// </summary>
    /// <param name="serverType">Sunucu tipi.</param>
    /// <param name="connectionString">Açık metin bağlantı dizisi.</param>
    /// <param name="timeoutSeconds">Bağlantı denemesi için maksimum bekleme süresi.</param>
    /// <returns>Bağlantı başarılıysa true, değilse false.</returns>
    public static async Task<(bool Success, string? Error)> TestConnectionAsync(
        string serverType,
        string connectionString,
        int timeoutSeconds = 10)
    {
        try
        {
            using var conn = Create(serverType, connectionString);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            if (conn is SqlConnection sqlConn)
                await sqlConn.OpenAsync(cts.Token);
            else if (conn is NpgsqlConnection pgConn)
                await pgConn.OpenAsync(cts.Token);
            else if (conn is MySqlConnection myConn)
                await myConn.OpenAsync(cts.Token);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
