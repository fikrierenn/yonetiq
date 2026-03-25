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

            // BKM Kitap şeması: irsHrk = TÜM hareketlerin merkez tablosu (57M satır)
            // POS satış, irsaliye, fatura — hepsi irsHrk'ya düşer
            // ehAdetN negatif = çıkış (satış), pozitif = giriş
            var mvt = "irsHrk"; // movements table

            var defs = new List<(string Type, string? Table, string? Col, string Name, string Desc, string Sql, string Aliases)>
            {
                // ── Ölçüler (irsHrk merkezli) ──
                ("measure", mvt, "ehTutarN", "ciro",
                    "Net satış tutarı — irsHrk.ehTutarN (satış satırlarında negatif, ABS ile pozitife çevir)",
                    "SUM(ABS(irsHrk.ehTutarN))",
                    "ciro,satış,gelir,hasılat,cirolar,toplam satış,toplam ciro,net ciro"),
                ("measure", mvt, "ehAdetN", "satış adedi",
                    "Satılan ürün adedi — irsHrk.ehAdetN (satışta negatif, ABS ile)",
                    "SUM(ABS(irsHrk.ehAdetN))",
                    "adet,kaç sattı,satış sayısı,kaç adet,kaç tane,satılan adet"),
                ("measure", mvt, "ehMlyt", "maliyet",
                    "Satılan ürünlerin maliyet toplamı — irsHrk.ehMlyt",
                    "SUM(ABS(irsHrk.ehMlyt))",
                    "maliyet,alış maliyeti,toplam maliyet,smm,cost"),
                ("measure", mvt, "ehTutarOzl", "özel tutar",
                    "Özel fiyat/kampanya tutarı — irsHrk.ehTutarOzl",
                    "SUM(ABS(irsHrk.ehTutarOzl))",
                    "özel fiyat,kampanya,özel tutar"),

                // ── İade ölçüleri ──
                ("measure", mvt, "ehTutarN", "iade tutarı",
                    "İade edilen ürünlerin tutarı — ehTip IN (3,5,101)",
                    "SUM(ABS(irsHrk.ehTutarN)) -- WHERE ehTip IN (3,5,101)",
                    "iade,iade tutarı,iade toplamı,return,iade cirosu"),
                ("measure", mvt, "ehAdetN", "iade adedi",
                    "İade edilen ürün adedi",
                    "SUM(ABS(irsHrk.ehAdetN)) -- WHERE ehTip IN (3,5,101)",
                    "iade adet,kaç iade,iade sayısı,iade edilen"),
                ("measure", mvt, null, "net ciro",
                    "Satış - İade = Net Ciro (tek sorguda hesaplanır)",
                    "SUM(CASE WHEN ehTip IN (1,4,100) THEN ABS(ehTutarN) WHEN ehTip IN (3,5,101) THEN -ABS(ehTutarN) ELSE 0 END)",
                    "net ciro,net satış,iade düşülmüş ciro,gerçek ciro"),

                // ── Tarih aralıkları (irsHrk.ehTrhS) ──
                ("date_range", mvt, "ehTrhS", "geçen ay",
                    "Bir önceki takvim ayı",
                    "irsHrk.ehTrhS >= DATEADD(month,DATEDIFF(month,0,GETDATE())-1,0) AND irsHrk.ehTrhS < DATEADD(month,DATEDIFF(month,0,GETDATE()),0)",
                    "geçen ay,önceki ay,son ay"),
                ("date_range", mvt, "ehTrhS", "bu ay",
                    "Cari takvim ayı",
                    "irsHrk.ehTrhS >= DATEADD(month,DATEDIFF(month,0,GETDATE()),0) AND irsHrk.ehTrhS < GETDATE()",
                    "bu ay,mevcut ay,aylık"),
                ("date_range", mvt, "ehTrhS", "bu yıl",
                    "Cari takvim yılı",
                    "YEAR(irsHrk.ehTrhS) = YEAR(GETDATE())",
                    "bu yıl,cari yıl,yılbaşından beri"),
                ("date_range", mvt, "ehTrhS", "geçen yıl",
                    "Bir önceki takvim yılı",
                    "YEAR(irsHrk.ehTrhS) = YEAR(GETDATE())-1",
                    "geçen yıl,önceki yıl,geçen sene"),
                ("date_range", mvt, "ehTrhS", "son 30 gün",
                    "Bugünden geriye 30 gün",
                    "irsHrk.ehTrhS >= DATEADD(day,-30,GETDATE())",
                    "son 30 gün,son bir ay,30 günlük"),
                ("date_range", mvt, "ehTrhS", "son 7 gün",
                    "Bugünden geriye 7 gün",
                    "irsHrk.ehTrhS >= DATEADD(day,-7,GETDATE())",
                    "son hafta,son 7 gün,haftalık"),

                // ── Boyutlar ──
                ("dimension", "posMagaza", "mekanAd", "mağaza",
                    "Mağaza/şube adı (JOIN: irsHrk.ehMekan = posMagaza.mekanID)",
                    "posMagaza.mekanAd",
                    "mağaza,şube,lokasyon,mağazalar,şubeler,hangi mağaza,mekan"),
                ("dimension", "UrunBilgi", "KatAna", "kategori",
                    "Ürün ana kategorisi (Roman, Çocuk Kitapları vb.)",
                    "UrunBilgi.KatAna",
                    "kategori,ürün grubu,bölüm,kategoriler,tür,kitap türü,reyon"),
                ("dimension", "UrunBilgi", "mrkAd", "yayınevi",
                    "Yayınevi/marka adı",
                    "UrunBilgi.mrkAd",
                    "yayınevi,marka,yayıncı,publisher,hangi yayınevi"),
                ("dimension", "UrunBilgi", "Yazar", "yazar",
                    "Kitabın yazarı",
                    "UrunBilgi.Yazar",
                    "yazar,kim yazmış,yazarlar,hangi yazar,author"),
                ("dimension", "UrunBilgi", "stkAd", "ürün adı",
                    "Kitap/ürün adı",
                    "UrunBilgi.stkAd",
                    "ürün,kitap,ürün adı,kitap adı,hangi kitap"),

                // ── KPI ──
                ("kpi", mvt, null, "büyüme oranı",
                    "Dönem büyümesi yüzdesi",
                    "(SUM(bu_donem) - SUM(gecen_donem)) * 100.0 / NULLIF(SUM(gecen_donem),0)",
                    "büyüme,artış,değişim,yüzde değişim,büyüdü mü,trend"),
                ("kpi", mvt, null, "iade oranı",
                    "Satışa karşı iade yüzdesi",
                    "SUM(CASE WHEN ehTip IN (3,5,101) THEN ABS(ehTutarN) ELSE 0 END) * 100.0 / NULLIF(SUM(CASE WHEN ehTip IN (1,4,100) THEN ABS(ehTutarN) ELSE 0 END),0)",
                    "iade oranı,iade yüzdesi,return rate,iade/satış"),

                // ── Hareket tipi filtreleri ──
                ("dimension", mvt, "ehTip", "POS satış",
                    "Kasa satışları — ehTip=100 (en yüksek hacim)",
                    "irsHrk.ehTip = 100",
                    "pos,kasa satışı,pos satış,mağaza kasa,perakende"),
                ("dimension", mvt, "ehTip", "toptan satış",
                    "Toptan/kurumsal satış — ehTip=1",
                    "irsHrk.ehTip = 1",
                    "toptan,kurumsal satış,toptan satış,b2b"),
                ("dimension", mvt, "ehTip", "tüm satışlar",
                    "POS + toptan + mağaza satış (iade hariç)",
                    "irsHrk.ehTip IN (1, 4, 100)",
                    "tüm satışlar,satışlar,all sales"),
                ("dimension", mvt, "ehTip", "tüm iadeler",
                    "POS iade + toptan iade + mağaza iade",
                    "irsHrk.ehTip IN (3, 5, 101)",
                    "tüm iadeler,iadeler,all returns"),
                ("dimension", mvt, "ehTip", "transfer",
                    "Mağaza/depo arası transferler",
                    "irsHrk.ehTip IN (8, 9, 11, 13)",
                    "transfer,sevk,depo transfer,mağaza transfer"),

                // ── Stok / Envanter (urnOzt tablosu — mağaza×ürün bazında) ──
                ("measure", "urnOzt", "stok", "stok",
                    "Anlık stok miktarı (mağaza×ürün bazında) — urnOzt.stok",
                    "SUM(urnOzt.stok)",
                    "stok,stok miktarı,envanter,kaç adet var,mevcut stok,eldeki stok"),
                ("measure", "urnOzt", "oztSon7", "son 7 gün satış",
                    "Son 7 günde satılan adet (urnOzt.oztSon7)",
                    "SUM(urnOzt.oztSon7)",
                    "haftalık satış,son 7 gün satış,bu hafta satış"),
                ("measure", "urnOzt", "oztSon30", "son 30 gün satış",
                    "Son 30 günde satılan adet (urnOzt.oztSon30)",
                    "SUM(urnOzt.oztSon30)",
                    "aylık satış,son 30 gün satış,bu ay satış adet"),
                ("measure", "urnOzt", "oztSon365", "yıllık satış",
                    "Son 365 günde satılan adet (urnOzt.oztSon365)",
                    "SUM(urnOzt.oztSon365)",
                    "yıllık satış,yıllık adet,senelik satış"),
                ("measure", "urnOzt", "hiz", "satış hızı",
                    "Günlük ortalama satış hızı (urnOzt.hiz)",
                    "AVG(urnOzt.hiz)",
                    "satış hızı,günlük satış,velocity,hız"),
                ("kpi", "urnOzt", null, "devir hızı",
                    "Stok devir hızı (urnOzt.devir veya oztDevirHizAy)",
                    "AVG(urnOzt.oztDevirHizAy)",
                    "devir hızı,devir,stok devir,inventory turnover"),
                ("kpi", "urnOzt", null, "stok bakiye (irsHrk)",
                    "irsHrk'dan hesaplanan stok bakiyesi — tüm giriş/çıkış toplamı",
                    "SUM(irsHrk.ehAdetN) -- mağaza ve ürün bazında",
                    "stok bakiye,hesaplanan stok,gerçek stok,irshrk stok"),
                ("measure", "urnOzt", "oztKritikStok", "kritik stok seviyesi",
                    "Minimum stok seviyesi (bu altına düşerse uyarı)",
                    "urnOzt.oztKritikStok",
                    "kritik stok,minimum stok,alarm seviyesi,reorder point"),
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
