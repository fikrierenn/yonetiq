// Temp script — DB listesi almak için
using Microsoft.Data.SqlClient;

var connStr = "Server=192.168.40.201;Database=master;User Id=sa;Password=H33451959*;TrustServerCertificate=true;Connect Timeout=10;";
using var conn = new SqlConnection(connStr);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT name, state_desc FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb') ORDER BY name";
using var reader = cmd.ExecuteReader();
while (reader.Read())
    Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)}");
