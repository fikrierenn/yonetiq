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

            // BKM Kitap şeması: fatAyr (fatura ayrıntı) + fat (fatura başlık) + UrunBilgi view + posMagaza
            // schema_mapping.json'daki değerler temel alınır, yoksa BKM default'ları kullanılır
            var salesTable = GetAnswer("sales_table") ?? "fatAyr";
            var headerTable = "fat";
            var amountCol = GetAnswer($"amount_col_{salesTable}") ?? "ehTutar";
            var dateCol = GetAnswer($"date_col_{salesTable}") ?? $"{headerTable}.eTarih";

            var defs = new List<(string Type, string? Table, string? Col, string Name, string Desc, string Sql, string Aliases)>
            {
                // ── Ölçüler ──
                ("measure", salesTable, amountCol, "ciro",
                    "Net satış tutarı (KDV hariç) — fatAyr.ehTutar",
                    $"SUM({amountCol})",
                    "ciro,satış,gelir,hasılat,cirolar,toplam satış,toplam ciro"),
                ("measure", salesTable, "ehTutarKDV", "kdv dahil ciro",
                    "KDV dahil satış tutarı",
                    "SUM(ehTutarKDV)",
                    "kdv dahil,brüt ciro,brüt satış,kdvli tutar"),
                ("measure", salesTable, "ehAdet", "satış adedi",
                    "Satılan ürün adedi — fatAyr.ehAdet",
                    "SUM(ehAdet)",
                    "adet,kaç sattı,satış sayısı,kaç adet,kaç tane,satılan adet"),
                ("measure", salesTable, "ehIndirim", "indirim tutarı",
                    "Uygulanan indirim toplamı",
                    "SUM(ehIndirim)",
                    "indirim,iskonto,kampanya tutarı,discount,indirim toplamı"),
                ("measure", salesTable, "ehMaliyet", "maliyet",
                    "Satılan ürünlerin maliyet toplamı",
                    "SUM(ehMaliyet)",
                    "maliyet,alış maliyeti,toplam maliyet,smm"),

                // ── Tarih aralıkları ── (fat.eTarih kullanılır)
                ("date_range", headerTable, "eTarih", "geçen ay",
                    "Bir önceki takvim ayı",
                    "fat.eTarih >= DATEADD(month,DATEDIFF(month,0,GETDATE())-1,0) AND fat.eTarih < DATEADD(month,DATEDIFF(month,0,GETDATE()),0)",
                    "geçen ay,önceki ay,son ay"),
                ("date_range", headerTable, "eTarih", "bu ay",
                    "Cari takvim ayı",
                    "fat.eTarih >= DATEADD(month,DATEDIFF(month,0,GETDATE()),0) AND fat.eTarih < GETDATE()",
                    "bu ay,mevcut ay,aylık"),
                ("date_range", headerTable, "eTarih", "bu yıl",
                    "Cari takvim yılı",
                    "YEAR(fat.eTarih) = YEAR(GETDATE())",
                    "bu yıl,cari yıl,yılbaşından beri"),
                ("date_range", headerTable, "eTarih", "geçen yıl",
                    "Bir önceki takvim yılı",
                    "YEAR(fat.eTarih) = YEAR(GETDATE())-1",
                    "geçen yıl,önceki yıl,geçen sene"),
                ("date_range", headerTable, "eTarih", "son 30 gün",
                    "Bugünden geriye 30 gün",
                    "fat.eTarih >= DATEADD(day,-30,GETDATE())",
                    "son 30 gün,son bir ay,30 günlük"),
                ("date_range", headerTable, "eTarih", "son 7 gün",
                    "Bugünden geriye 7 gün",
                    "fat.eTarih >= DATEADD(day,-7,GETDATE())",
                    "son hafta,son 7 gün,haftalık"),

                // ── Boyutlar ──
                ("dimension", "posMagaza", "mekanAd", "mağaza",
                    "Mağaza/şube adı — posMagaza.mekanAd (JOIN: fat.eMekan = posMagaza.mekanID)",
                    "posMagaza.mekanAd",
                    "mağaza,şube,lokasyon,mağazalar,şubeler,hangi mağaza,mekan"),
                ("dimension", "UrunBilgi", "KatAna", "kategori",
                    "Ürün ana kategorisi (Roman, Çocuk Kitapları vb.) — UrunBilgi view",
                    "UrunBilgi.KatAna",
                    "kategori,ürün grubu,bölüm,kategoriler,tür,kitap türü,reyon"),
                ("dimension", "UrunBilgi", "mrkAd", "yayınevi",
                    "Yayınevi/marka adı — UrunBilgi view",
                    "UrunBilgi.mrkAd",
                    "yayınevi,marka,yayıncı,publisher,hangi yayınevi"),
                ("dimension", "UrunBilgi", "Yazar", "yazar",
                    "Kitabın yazarı — UrunBilgi view",
                    "UrunBilgi.Yazar",
                    "yazar,kim yazmış,yazarlar,hangi yazar,author"),
                ("dimension", "UrunBilgi", "stkAd", "ürün adı",
                    "Kitap/ürün adı — UrunBilgi view",
                    "UrunBilgi.stkAd",
                    "ürün,kitap,ürün adı,kitap adı,hangi kitap"),

                // ── KPI ──
                ("kpi", salesTable, null, "büyüme oranı",
                    "Dönem büyümesi yüzdesi (bu dönem vs önceki dönem)",
                    "(SUM(bu_donem) - SUM(gecen_donem)) * 100.0 / NULLIF(SUM(gecen_donem),0)",
                    "büyüme,artış,değişim,yüzde değişim,büyüdü mü,trend"),
                ("kpi", salesTable, null, "ortalama sepet",
                    "Fatura başına ortalama tutar",
                    "SUM(ehTutar) / COUNT(DISTINCT ehID)",
                    "ortalama sepet,sepet tutarı,fatura ortalaması,average basket"),

                // ── Filtreler ──
                ("dimension", headerTable, "eTip", "satış filtresi",
                    "Sadece satış faturaları: eTip=1 (toptan), eTip=4 (mağaza perakende)",
                    "fat.eGC = 1 AND fat.eTip IN (1, 4)",
                    "satış,satış faturası,perakende,toptan satış"),
                ("dimension", headerTable, "eTip", "iade filtresi",
                    "İade faturaları: eTip=3 (toptan iade), eTip=5 (mağaza iade)",
                    "fat.eGC = 1 AND fat.eTip IN (3, 5)",
                    "iade,satış iade,iade faturası,return"),
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
