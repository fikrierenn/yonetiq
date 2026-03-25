using System.Text.Json;
using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// schema_mapping.json'daki cevaplara bakarak SemanticDefinitions tablosunu otomatik bootstrap eder.
/// Onboarding'in son adımı.
/// </summary>
public class SemanticBootstrapService(
    IConfiguration config, AuditService auditSvc,
    ILogger<SemanticBootstrapService> logger)
    : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<int>> BootstrapFromMappingAsync(string mappingJsonPath)
    {
        if (!File.Exists(mappingJsonPath))
            return ServiceResult<int>.Failure("schema_mapping.json bulunamadı");

        SchemaMapping? mapping;
        try
        {
            var json = await File.ReadAllTextAsync(mappingJsonPath);
            mapping = JsonSerializer.Deserialize<SchemaMapping>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return ServiceResult<int>.Failure("schema_mapping.json geçersiz"); }

        if (mapping is null) return ServiceResult<int>.Failure("Mapping boş");

        return await ExecuteServiceAsync<int>(async conn =>
        {
            var existing = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SemanticDefinitions WHERE IsActive=1");
            if (existing > 10)
            {
                logger.LogInformation("SemanticDefinitions zaten dolu ({C} kayıt), skip", existing);
                return existing;
            }

            // Cevaplardan tablo/kolon adlarını çıkar
            string? GetAnswer(string qId) =>
                mapping.Answers.FirstOrDefault(a => a.QuestionId == qId)?.Answer;

            var salesTable = GetAnswer("sales_table") ?? "SalesLines";
            var amountCol = GetAnswer($"amount_col_{salesTable}") ?? "NetAmount";
            var dateCol = GetAnswer($"date_col_{salesTable}") ?? "SaleDate";

            var defs = new List<(string Type, string? Table, string? Col, string Name, string Desc, string Sql, string Aliases)>
            {
                ("measure", salesTable, amountCol, "ciro", "Net satış tutarı", $"SUM({amountCol})", "ciro,satış,gelir,hasılat,toplam satış"),
                ("measure", salesTable, "Quantity", "satış adedi", "Satılan adet", "SUM(Quantity)", "adet,kaç sattı,satış sayısı"),
                ("date_range", salesTable, dateCol, "geçen ay", "Bir önceki takvim ayı",
                    $"{dateCol} >= DATEADD(month,DATEDIFF(month,0,GETDATE())-1,0) AND {dateCol} < DATEADD(month,DATEDIFF(month,0,GETDATE()),0)",
                    "geçen ay,önceki ay,son ay"),
                ("date_range", salesTable, dateCol, "bu ay", "Cari takvim ayı",
                    $"{dateCol} >= DATEADD(month,DATEDIFF(month,0,GETDATE()),0) AND {dateCol} < GETDATE()",
                    "bu ay,mevcut ay,aylık"),
                ("date_range", salesTable, dateCol, "bu yıl", "Cari takvim yılı",
                    $"YEAR({dateCol}) = YEAR(GETDATE())", "bu yıl,cari yıl,yılbaşından beri"),
                ("date_range", salesTable, dateCol, "geçen yıl", "Bir önceki takvim yılı",
                    $"YEAR({dateCol}) = YEAR(GETDATE())-1", "geçen yıl,önceki yıl,geçen sene"),
                ("date_range", salesTable, dateCol, "son 30 gün", "Bugünden geriye 30 gün",
                    $"{dateCol} >= DATEADD(day,-30,GETDATE())", "son 30 gün,son bir ay"),
            };

            var count = 0;
            foreach (var d in defs)
            {
                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM SemanticDefinitions WHERE BusinessName=@Name AND IsActive=1)
                    INSERT INTO SemanticDefinitions
                        (TermType,TableName,ColumnName,BusinessName,Description,SqlExpression,Aliases,IsActive,CreatedAt)
                    VALUES (@Type,@Table,@Col,@Name,@Desc,@Sql,@Aliases,1,SYSUTCDATETIME())",
                    new { d.Type, d.Table, d.Col, d.Name, d.Desc, d.Sql, d.Aliases });
                count++;
            }

            logger.LogInformation("SemanticBootstrap: {Count} terim eklendi", count);
            return count;
        });
    }
}
