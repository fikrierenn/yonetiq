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

            // BKM Kitap şeması — SP analizi sonrası doğru tablo kullanımı:
            // SATIŞ RAPORU: irsAyr + irs (urnOzt365_tum bunu kullanıyor)
            // STOK BAKİYE: irsHrk (urnOzt_fifoTek bunu kullanıyor)
            // STOK ÖZET: urnOzt (gece güncellenir — precalculated)
            // ehAdet negatif = çıkış (satış), SUM(-ehAdet) ile pozitife çevir

            var defs = new List<(string Type, string? Table, string? Col, string Name, string Desc, string Sql, string Aliases)>
            {
                // ══════════════════════════════════════════════════
                // SATIŞ ÖLÇÜLERİ (irsAyr + irs — ERP ile tutarlı)
                // eTip IN (1,3,4,5,100,101) = satış + iade tipleri
                // SUM(-ehAdet) → satışta pozitif, iadede negatif
                // ══════════════════════════════════════════════════
                ("measure", "irsAyr", "ehAdet", "ciro",
                    "Net satış tutarı — irsAyr+irs, SUM(-ehAdet*birimFiyat) veya fatAyr.ehTutar. eTip IN (1,4,100) satış, (3,5,101) iade.",
                    "SUM(-irsAyr.ehAdet)",
                    "ciro,satış,gelir,hasılat,cirolar,toplam satış,toplam ciro"),
                ("measure", "irsAyr", "ehAdet", "satış adedi",
                    "Satılan ürün adedi — SUM(-ehAdet) WHERE eTip IN (1,4,100)",
                    "SUM(-irsAyr.ehAdet)",
                    "adet,kaç sattı,satış sayısı,kaç adet,kaç tane,satılan adet"),
                ("measure", "irsAyr", "ehAdet", "iade adedi",
                    "İade edilen ürün adedi — SUM(-ehAdet) WHERE eTip IN (3,5,101)",
                    "SUM(-irsAyr.ehAdet) -- WHERE irs.eTip IN (3,5,101)",
                    "iade,iade adet,kaç iade,iade sayısı,iade edilen"),
                ("measure", "irsAyr", null, "net satış adedi",
                    "Satış - İade = Net adet (satış+iade birlikte, işaret farkıyla zaten net)",
                    "SUM(-irsAyr.ehAdet) -- WHERE irs.eTip IN (1,3,4,5,100,101)",
                    "net satış,net adet,iade düşülmüş,gerçek satış"),

                // ══════════════════════════
                // TARİH ARALIKLARI (irs.eTarih)
                // ══════════════════════════
                ("date_range", "irs", "eTarih", "geçen ay",
                    "Bir önceki takvim ayı",
                    "irs.eTarih >= DATEADD(month,DATEDIFF(month,0,GETDATE())-1,0) AND irs.eTarih < DATEADD(month,DATEDIFF(month,0,GETDATE()),0)",
                    "geçen ay,önceki ay,son ay"),
                ("date_range", "irs", "eTarih", "bu ay",
                    "Cari takvim ayı",
                    "irs.eTarih >= DATEADD(month,DATEDIFF(month,0,GETDATE()),0) AND irs.eTarih < GETDATE()",
                    "bu ay,mevcut ay,aylık"),
                ("date_range", "irs", "eTarih", "bu yıl",
                    "Cari takvim yılı",
                    "YEAR(irs.eTarih) = YEAR(GETDATE())",
                    "bu yıl,cari yıl,yılbaşından beri"),
                ("date_range", "irs", "eTarih", "geçen yıl",
                    "Bir önceki takvim yılı",
                    "YEAR(irs.eTarih) = YEAR(GETDATE())-1",
                    "geçen yıl,önceki yıl,geçen sene"),
                ("date_range", "irs", "eTarih", "son 30 gün",
                    "Bugünden geriye 30 gün",
                    "irs.eTarih >= DATEADD(day,-30,GETDATE())",
                    "son 30 gün,son bir ay,30 günlük"),
                ("date_range", "irs", "eTarih", "son 7 gün",
                    "Bugünden geriye 7 gün",
                    "irs.eTarih >= DATEADD(day,-7,GETDATE())",
                    "son hafta,son 7 gün,haftalık"),

                // ══════════════════════════
                // BOYUTLAR
                // ══════════════════════════
                ("dimension", "posMagaza", "mekanAd", "mağaza",
                    "Mağaza/şube adı (JOIN: irs.eMekan = posMagaza.mekanID)",
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

                // ══════════════════════════
                // HAREKET TİPİ FİLTRELERİ
                // irs.eTip değerleri (irsTip_vw)
                // ══════════════════════════
                ("dimension", "irs", "eTip", "POS satış",
                    "Mağaza kasa satışları — eTip=100 (en yüksek hacim)",
                    "irs.eTip = 100",
                    "pos,kasa satışı,pos satış,mağaza kasa,perakende"),
                ("dimension", "irs", "eTip", "toptan satış",
                    "Toptan/kurumsal satış — eTip=1",
                    "irs.eTip = 1",
                    "toptan,kurumsal satış,toptan satış,b2b"),
                ("dimension", "irs", "eTip", "tüm satışlar",
                    "POS + toptan + mağaza satış — eTip IN (1,4,100)",
                    "irs.eTip IN (1, 4, 100)",
                    "tüm satışlar,satışlar,all sales"),
                ("dimension", "irs", "eTip", "tüm iadeler",
                    "POS iade + toptan iade + mağaza iade — eTip IN (3,5,101)",
                    "irs.eTip IN (3, 5, 101)",
                    "tüm iadeler,iadeler,all returns"),
                ("dimension", "irs", "eTip", "satış ve iade",
                    "Tüm satış+iade tipleri (net hesaplama için) — ERP standart filtre",
                    "irs.eTip IN (1, 3, 4, 5, 100, 101)",
                    "satış iade,hepsi,tüm işlemler"),

                // ══════════════════════════
                // KPI
                // ══════════════════════════
                ("kpi", "irsAyr", null, "iade oranı",
                    "Satışa karşı iade yüzdesi",
                    "SUM(CASE WHEN irs.eTip IN (3,5,101) THEN -irsAyr.ehAdet ELSE 0 END) * 100.0 / NULLIF(SUM(CASE WHEN irs.eTip IN (1,4,100) THEN -irsAyr.ehAdet ELSE 0 END),0)",
                    "iade oranı,iade yüzdesi,return rate"),

                // ══════════════════════════════════════════════════
                // STOK / ENVANTER (urnOzt — gece güncellenen özet)
                // Kaynak: DerinSis_Ozet job, her gece 23:00
                // FIFO maliyet: irsHrk üzerinden çift cursor ile hesaplanıyor
                // ══════════════════════════════════════════════════
                ("measure", "urnOzt", "stok", "stok",
                    "Stok miktarı (mağaza×ürün, gece güncellenir)",
                    "SUM(urnOzt.stok)",
                    "stok,stok miktarı,envanter,kaç adet var,mevcut stok,eldeki stok"),
                ("measure", "urnOzt", "oztSon7", "haftalık satış adet",
                    "Son 7 günde satılan adet (precalculated)",
                    "SUM(urnOzt.oztSon7)",
                    "haftalık satış,son 7 gün satış,bu hafta satış"),
                ("measure", "urnOzt", "oztSon30", "aylık satış adet",
                    "Son 30 günde satılan adet (precalculated)",
                    "SUM(urnOzt.oztSon30)",
                    "aylık satış,son 30 gün satış,bu ay satış adet"),
                ("measure", "urnOzt", "oztSon365", "yıllık satış adet",
                    "Son 365 günde satılan adet (precalculated)",
                    "SUM(urnOzt.oztSon365)",
                    "yıllık satış,yıllık adet,senelik satış"),
                ("measure", "urnOzt", "hiz", "satış hızı",
                    "Günlük ort. satış = toplam_satis / (aktif_gun - stoksuz_gun)",
                    "AVG(urnOzt.hiz)",
                    "satış hızı,günlük satış,velocity,hız"),
                ("kpi", "urnOzt", "devir", "stok devir süresi",
                    "FIFO bazlı ortalama stokta kalma süresi (gün). Düşük=iyi.",
                    "AVG(urnOzt.devir)",
                    "devir hızı,devir,stok devir,inventory turnover,stokta kalma"),
                ("measure", "urnOzt", "oztKritikStok", "kritik stok seviyesi",
                    "Minimum stok (altına düşerse uyarı)",
                    "urnOzt.oztKritikStok",
                    "kritik stok,minimum stok,alarm seviyesi,reorder point"),

                // ══════════════════════════════════════════════════
                // STOK BAKİYE (irsHrk — anlık hesaplama)
                // FIFO MALİYET de irsHrk.ehMlyt'de (gece güncellenir)
                // ══════════════════════════════════════════════════
                ("kpi", "irsHrk", null, "anlık stok bakiye",
                    "irsHrk'dan anlık stok: SUM(ehAdetN) — pozitif=giriş, negatif=çıkış",
                    "SUM(irsHrk.ehAdetN) -- GROUP BY ehstkID, ehMekan",
                    "anlık stok,gerçek stok,canlı stok,stok bakiye"),
                ("measure", "irsHrk", "ehMlyt", "FIFO maliyet",
                    "Çıkış satırlarındaki FIFO maliyet (gece hesaplanır)",
                    "SUM(irsHrk.ehMlyt)",
                    "maliyet,fifo maliyet,smm,satış maliyeti,cost"),
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
