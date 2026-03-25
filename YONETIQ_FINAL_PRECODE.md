# YonetIQ — Koda Geçiş Öncesi Son Belge

**Tarih:** 25.03.2026 | **Kapsam:** Veri kalitesi + Deployment + Streaming + Test stratejisi

---

## BÖLÜM A — VERİ KALİTESİ STRATEJİSİ

### A.1 — Problem Tespiti

AI'dan önce veri temiz olmalı. "Geçen ay ciro" sorusuna doğru cevap gelmesinin önkoşulu:
* Satış satırlarında net tutar mı var, brüt mü? İade ayrı mı?
* Mağaza kodu tutarlı mı? ("İst-Kad", "IST_KAD", "Kadıköy" hepsi aynı mağaza mı?)
* ISBN formatı tutarlı mı? (10 haneli, 13 haneli karışık?)
* Kampanya fiyatı kayıtlarda nasıl tutuluyor? Discount ayrı alan mı yoksa net fiyat mı?
* Kategoriler normalize mi? (Roman, roman, ROMAN — hepsi var mı?)

Bu soruları Claude Code'dan önce sen cevaplamalısın — BKM ERP erişiminle.

### A.2 — Veri Kalite Kontrol SP'leri

Dosya: `yonetiq/Data/StoredProcedures/sp_DataQuality_Check.sql`

```sql
-- sp_DataQuality_Check
-- Çalıştır, sonuçları gör, SemanticDefinitions'ı buna göre yaz
IF OBJECT_ID('sp_DataQuality_Check','P') IS NOT NULL DROP PROCEDURE sp_DataQuality_Check;
GO
CREATE PROCEDURE sp_DataQuality_Check
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Mağaza kodu varyantları
    SELECT 'Mağaza varyantları' AS Kontrol,
           StoreName, COUNT(*) AS SatirSayisi
    FROM SalesLines
    GROUP BY StoreName
    ORDER BY COUNT(*) DESC;

    -- 2. Boş/null kritik alanlar
    SELECT 'Null kontrol' AS Kontrol,
        SUM(CASE WHEN ISBN IS NULL OR ISBN='' THEN 1 ELSE 0 END) AS NullISBN,
        SUM(CASE WHEN StoreName IS NULL OR StoreName='' THEN 1 ELSE 0 END) AS NullMagaza,
        SUM(CASE WHEN CategoryName IS NULL OR CategoryName='' THEN 1 ELSE 0 END) AS NullKategori,
        SUM(CASE WHEN NetAmount IS NULL THEN 1 ELSE 0 END) AS NullCiro,
        SUM(CASE WHEN SaleDate IS NULL THEN 1 ELSE 0 END) AS NullTarih,
        COUNT(*) AS ToplamSatir
    FROM SalesLines;

    -- 3. Negatif/sıfır tutar (iade mi, hata mı?)
    SELECT 'Tutar anomalisi' AS Kontrol,
        SUM(CASE WHEN NetAmount < 0 THEN 1 ELSE 0 END) AS NegatifTutar,
        SUM(CASE WHEN NetAmount = 0 THEN 1 ELSE 0 END) AS SifirTutar,
        SUM(CASE WHEN Quantity <= 0 THEN 1 ELSE 0 END) AS SifirAdet
    FROM SalesLines;

    -- 4. Tarih aralığı tutarlılık
    SELECT 'Tarih aralığı' AS Kontrol,
        MIN(SaleDate) AS IlkSatis,
        MAX(SaleDate) AS SonSatis,
        COUNT(DISTINCT CAST(SaleDate AS DATE)) AS FarkliGunSayisi,
        DATEDIFF(day, MIN(SaleDate), MAX(SaleDate)) AS ToplamGun
    FROM SalesLines;

    -- 5. Kategori varyantları
    SELECT 'Kategori varyantları' AS Kontrol,
        CategoryName, COUNT(*) AS UrunSayisi
    FROM Products
    WHERE IsActive = 1
    GROUP BY CategoryName
    ORDER BY COUNT(*) DESC;

    -- 6. ISBN format kontrolü
    SELECT 'ISBN format' AS Kontrol,
        LEN(ISBN) AS ISBNUzunluk,
        COUNT(*) AS Adet
    FROM Products
    WHERE ISBN IS NOT NULL AND ISBN != ''
    GROUP BY LEN(ISBN)
    ORDER BY LEN(ISBN);

    -- 7. Stok tutarlılık
    SELECT 'Stok anomalisi' AS Kontrol,
        SUM(CASE WHEN StockQuantity < 0 THEN 1 ELSE 0 END) AS NegatifStok,
        SUM(CASE WHEN StockQuantity > 10000 THEN 1 ELSE 0 END) AS AsiriYuksekStok,
        COUNT(CASE WHEN IsActive=1 AND StockQuantity IS NULL THEN 1 END) AS StokNullAktif
    FROM Products;
END;
GO
```

### A.3 — Veri Temizleme Kuralları (BKM'ye özel)

Dosya: `yonetiq/Data/StoredProcedures/sp_DataQuality_Fix.sql`

```sql
-- SADECE test ortamında çalıştır önce, production'da DBA onayıyla
IF OBJECT_ID('sp_DataQuality_Fix','P') IS NOT NULL DROP PROCEDURE sp_DataQuality_Fix;
GO
CREATE PROCEDURE sp_DataQuality_Fix
    @DryRun BIT = 1  -- 1 = sadece raporla, 0 = gerçekten düzelt
AS
BEGIN
    SET NOCOUNT ON;

    IF @DryRun = 0
    BEGIN
        UPDATE SalesLines SET StoreName = LTRIM(RTRIM(StoreName));
        UPDATE Products    SET CategoryName = LTRIM(RTRIM(CategoryName));
    END
    ELSE
    BEGIN
        SELECT 'StoreName boşluk' AS Fix, COUNT(*) AS Adet
        FROM SalesLines WHERE StoreName != LTRIM(RTRIM(StoreName));
        SELECT 'CategoryName boşluk' AS Fix, COUNT(*) AS Adet
        FROM Products WHERE CategoryName != LTRIM(RTRIM(CategoryName));
    END
END;
GO
```

### A.4 — DataSourceService: Veri Kalite Skoru

```csharp
/// <summary>
/// Bağlı veri kaynağının kalite skorunu hesaplar (0-100).
/// </summary>
public async Task<ServiceResult<DataQualityReport>> GetQualityReportAsync(int dataSourceId)
{
    // ... (tam implementasyon için belgeye bakın)
}
```

Model:
```csharp
public class DataQualityReport
{
    public int  DataSourceId    { get; set; }
    public bool HasSalesTable   { get; set; }
    public bool HasProductTable { get; set; }
    public bool HasStoreTable   { get; set; }
    public decimal NullRateEstimate { get; set; }  // -1 = test edilemedi
    public int  Score           { get; set; }      // 0-100
    public string ScoreLabel    => Score >= 80 ? "İyi" : Score >= 50 ? "Orta" : "Zayıf";
}
```

---

## BÖLÜM B — DEPLOYMENT

### B.1 — Hedef: IIS + Windows Server (BKM iç sunucu)

```
[BKM İç Ağ]
    └── Windows Server (IIS 10)
            └── Site: yonetiq.bkm.local
                    ├── .NET 10 Hosting Bundle
                    ├── appsettings.json (sadece non-secret)
                    ├── .env dosyası (secrets)
                    └── Uygulama klasörü: C:\inetpub\yonetiq\
```

### B.2 — IIS Kurulum Adımları

```powershell
# 1. .NET 10 Windows Hosting Bundle kur
# 2. IIS Application Pool
Import-Module WebAdministration
New-WebAppPool -Name "YonetIQ"
Set-ItemProperty IIS:\AppPools\YonetIQ -Name processModel.identityType -Value NetworkService
Set-ItemProperty IIS:\AppPools\YonetIQ -Name managedRuntimeVersion -Value ""  # No Managed Code

# 3. IIS Site
New-Website -Name "YonetIQ" -Port 80 -PhysicalPath "C:\inetpub\yonetiq" -ApplicationPool "YonetIQ"

# 4. Klasör izinleri
icacls "C:\inetpub\yonetiq" /grant "IIS_IUSRS:(OI)(CI)F"
icacls "C:\inetpub\yonetiq\Data\AiSkills\Prompts" /grant "IIS_IUSRS:(OI)(CI)F"
```

### B.3 — Publish Script

Dosya: `D:\Dev\yonet\scripts\publish.ps1`

```powershell
param(
    [string]$Target = "C:\inetpub\yonetiq",
    [string]$Config = "Release",
    [switch]$StopSite
)

$ProjectPath = "D:\Dev\yonet\yonetiq\yonetiq.csproj"
$PublishPath  = "D:\Dev\yonet\_publish"

Write-Host "=== YonetIQ Publish ===" -ForegroundColor Cyan

if ($StopSite) {
    Stop-Website -Name "YonetIQ" -ErrorAction SilentlyContinue
    Write-Host "Site durduruldu" -ForegroundColor Yellow
}

dotnet publish $ProjectPath `
    -c $Config `
    -r win-x64 `
    --self-contained false `
    -o $PublishPath `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD HATA!" -ForegroundColor Red
    exit 1
}

$excludeFiles = @("appsettings.Development.json", "*.pdb")

Get-ChildItem $PublishPath -Recurse | Where-Object {
    $name = $_.Name
    -not ($excludeFiles | Where-Object { $name -like $_ })
} | ForEach-Object {
    $dest = $_.FullName.Replace($PublishPath, $Target)
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName -Destination $dest -Force
}

Write-Host "Dosyalar kopyalandı: $Target" -ForegroundColor Green

if ($StopSite) {
    Start-Website -Name "YonetIQ"
    Write-Host "Site başlatıldı" -ForegroundColor Green
}

Write-Host "=== TAMAMLANDI ===" -ForegroundColor Cyan
```

### B.4 — Production appsettings

```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "DataSourceEncryptionKey": "",
  "AiProvider": {
    "PrimaryProvider": "gemini",
    "FallbackProvider": "openai",
    "GeminiEndpoint": "https://generativelanguage.googleapis.com",
    "GeminiModel": "gemini-1.5-flash"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "YonetIQ": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### B.5 — Migration Stratejisi

Production'da otomatik migration yerine logla + DBA uygulasın.

### B.6 — Health Check Endpoint

```csharp
app.MapGet("/health", async (AppDbContext db, SkillRegistry registry) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status  = "ok",
            db      = "connected",
            skills  = registry.Count,
            time    = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"DB bağlantı hatası: {ex.Message}");
    }
});
```

---

## BÖLÜM C — STREAMING AI YANITI

### C.1 — Neden Önemli

Şu an: kullanıcı sorar → 3-5 sn spinner → yanıt bir anda gelir.
Streaming ile: kullanıcı sorar → 200ms içinde ilk kelimeler gelir → metin akar.

### C.2 — AiProviderService Streaming

```csharp
public async IAsyncEnumerable<string> GenerateStreamAsync(
    string systemPrompt, string userPrompt, float temperature = 0.2f,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // Gemini SSE streaming endpoint
    // ... (tam implementasyon için belgeye bakın)
}
```

### C.3 — AiOrchestrationService Streaming Wrapper

```csharp
public async IAsyncEnumerable<string> ProcessRequestStreamAsync(
    AiRequest request,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // Skill çözümle → context topla → stream → etkileşim kaydet
}
```

### C.4 — CommandBar Streaming UI

- `_streaming` state + `StringBuilder _streamBuffer`
- Her chunk'ta `StateHasChanged()`
- `CancelStream()` desteği
- `.yi-stream-cursor` yanıp sönen cursor animasyonu

### C.5 — Streaming Olmayan Skill'ler İçin Fallback

```csharp
public bool SupportsStreaming(string skillId)
{
    var skill = skillRegistry.Resolve(skillId);
    return skill?.OutputType is SkillOutputType.Text or SkillOutputType.Suggestion;
}
```

---

## BÖLÜM D — TEST STRATEJİSİ

### D.1 — Test Felsefesi

54 test var. Yeni ~15 servis ekliyoruz. Kritik iş mantığını test et:

1. **LearningSignalService** — sinyal ağırlıkları yanlışsa sistem çöp öğrenir
2. **SemanticEnricher** — term çözümleme yanlışsa NlToSql bozulur
3. **SkillPersistenceService** — skill yükleme bozulursa uygulama başlamaz
4. **ConversationContextService** — bağlam yönetimi yanlışsa sohbet bozulur
5. **MetaSkillService** — JSON parse yanlışsa AI ürettiği skill sistemi çökertir

### D.2-D.7 — Test Dosyaları

- `LearningSignalServiceTests.cs` — Similarity, Hash, RecordSqlCorrection
- `SemanticEnricherTests.cs` — Enrich known terms, empty input, no match
- `MetaSkillServiceTests.cs` — JSON parse, invalid JSON, missing fields
- `ConversationContextTests.cs` — MaxTurns, serialization roundtrip
- `SkillPersistenceTests.cs` — ParseArr, MapToDefinition
- `AiPipelineIntegrationTests.cs` — SkillNotFound, InvalidUserId, Registry.Disable

### D.9 — csproj Güncellemesi

```xml
<PackageReference Include="Moq" Version="4.20.72" />
```

---

## BÖLÜM E — EKSİK KALAN KÜÇÜK ŞEYLER

- **E.1:** SkillExecutor BuildVariables/RenderTemplate → internal erişim
- **E.2:** ScheduledReportWorker → haftalık pattern decay
- **E.3:** AiProviderConfig API key fallback zinciri
- **E.4:** Blazor Circuit ID → ConversationContext sessionKey

---

## BÖLÜM F — TAM UYGULAMA SIRASI (55 Adım, 9 Faz)

### Faz 0 — Hazırlık (1-3)
1. git init + .gitignore + env var ✅
2. BKM ERP tabloları incele → sp_DataQuality_Check çalıştır
3. Semantic term mapping'i çıkar → SemanticDefinitions seed'i hazırla

### Faz 1 — Teknik borçlar (4-9) ✅
4. Build uyarıları temizle ✅
5. BaseEntity.UpdatedAt ✅
6. Schema detection cache ✅
7. Session null safety ✅
8. NotificationService memory leak ✅
9. Sayfa yetki + PromptEngine hot-reload ✅

### Faz 2 — Veritabanı (10-18)
10. AiSkillDefinitions tablosu
11. AiPatternSignals
12. AiPatterns ek kolonlar
13. SemanticLearningCandidates
14. AiQueryLog
15. AiConversationContext
16. AiSkillTriggerLog
17. UserDashboardPreferences
18. Decision ek kolonlar

### Faz 3 — Core servisler (19-27)
19. AiSkillRecord model
20. SkillPersistenceService
21. SkillRegistry güncelleme
22. LearningSignalService
23. SemanticEnricher
24. ConversationContextService
25. SkillTriggerEngine
26. SemanticDiscoveryService
27. ContinuousLearningService

### Faz 4 — Meta-Skill (28-33)
28. MetaSkills.cs tanımları
29. Meta prompt dosyaları (6 dosya)
30. MetaSkillService
31. SkillStudio.razor
32. SkillManagement.razor
33. SemanticAdmin.razor

### Faz 5 — Servis güncellemeleri (34-40)
34. AiOrchestrationService (ConversationContext + LearningSignal)
35. ContextBuilderService (Semantic + Conversation)
36. PromptEngine DB override
37. AiFeedbackWidget tüm sinyaller
38. QueryEditor SQL sinyal
39. AiMemoryService.GetApprovedPatterns güncelle
40. SkillExecutor — internal metotlar

### Faz 6 — Streaming (41-43)
41. AiProviderService.GenerateStreamAsync
42. AiOrchestrationService.ProcessRequestStreamAsync
43. CommandBar streaming UI

### Faz 7 — BKM Domain (44-47)
44. BookSkills.cs + 6 prompt dosyası
45. SemanticDefinitions BKM seed
46. ProactiveInsight BKM ekleri
47. Program.cs skill init güncelleme

### Faz 8 — AI Bağlantıları (48-51)
48. SuggestedAction butonları
49. Toplantı hazırlık otomatı
50. Görev + Not AI
51. Anomali bildirim zinciri

### Faz 9 — Deployment + Test (52-55)
52. Testler (LearningSignal + SemanticEnricher + MetaSkill + Pipeline)
53. Health check endpoint
54. IIS + publish script hazırlığı
55. Production env dosyası + ilk deploy
