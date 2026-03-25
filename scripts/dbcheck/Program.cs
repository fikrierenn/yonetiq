using Microsoft.Data.SqlClient;
var connStr = "Server=192.168.40.201;Database=master;User Id=sa;Password=H33451959*;TrustServerCertificate=true;Connect Timeout=10;";
try {
    using var conn = new SqlConnection(connStr);
    conn.Open();
    Console.WriteLine("BAGLANTI BASARILI");
    Console.WriteLine("=================");
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT name, state_desc, CAST(SUM(size)*8/1024 AS INT) AS SizeMB FROM sys.databases d LEFT JOIN sys.master_files f ON d.database_id=f.database_id WHERE d.name NOT IN ('master','tempdb','model','msdb') GROUP BY d.name, d.state_desc ORDER BY SizeMB DESC";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"DB ADI",-30} {"DURUM",-12} {"BOYUT(MB)"}");
    Console.WriteLine(new string('-', 55));
    while (reader.Read())
        Console.WriteLine($"{reader.GetString(0),-30} {reader.GetString(1),-12} {reader.GetInt32(2)} MB");
} catch (Exception ex) {
    Console.WriteLine("HATA: " + ex.Message);
}
