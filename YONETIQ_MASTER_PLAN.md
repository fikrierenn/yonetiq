# YonetIQ — Claude Code Master Implementation Plan
**Tarih:** 25.03.2026  
**Kapsam:** Tüm düzeltmeler + AI derinleştirme + BKM Kitap domain zekası  
**Hedef:** OzBI'yı geçen, BKM'nin beyni olan AI-first yönetim platformu

---

## BÖLÜM 0 — BAŞLAMADAN ÖNCE

### Repo Kurulumu (GİT YOK — KRİTİK)

```bash
cd D:\Dev\yonet
git init
git add .
git commit -m "chore: initial commit — pre-implementation baseline"
git checkout -b dev
```

`.gitignore` oluştur (`D:\Dev\yonet\.gitignore`):
```
# Secrets
yonetiq/appsettings.Development.json
*.env
.env

# Build
**/bin/
**/obj/
**/.vs/

# Logs
*.txt
!CLAUDE.md
!README.md
!TRAINING_MANUAL.md
!USER_GUIDE.md
!DEVELOPER_GUIDE.md
!PROJECT_GUIDE.md
!task.md

# OS
Thumbs.db
.DS_Store
```

`appsettings.Development.json` içinden `DataSourceEncryptionKey` ve connection string'i `.env`'e taşı:
```
YONET_CONN=Server=.;Database=YonetIQ;Trusted_Connection=true;
YONETIQ_ENCRYPTION_KEY=<mevcut_key_buraya>
```

`Program.cs`'de env var'dan oku (mevcut config yerine):
```csharp
// Program.cs başına ekle — builder.Services'den önce
var connStr = Environment.GetEnvironmentVariable("YONET_CONN")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;

var encKey = Environment.GetEnvironmentVariable("YONETIQ_ENCRYPTION_KEY")
    ?? builder.Configuration["DataSourceEncryptionKey"];
builder.Configuration["DataSourceEncryptionKey"] = encKey;
```

---

## BÖLÜM 1 — KRİTİK BORÇLAR (Önce bunlar)

### 1.1 — BaseEntity.UpdatedAt Eklenmesi

**Dosya:** `yonetiq/Data/Models/BaseEntity.cs`

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }  // YENİ
}
```

**Migration:** `yonetiq/Data/InfrastructureSeed.cs` içinde (mevcut pattern'e uy):
```csharp
// InfrastructureSeed içindeki ApplyAsync metoduna ekle
await EnsureColumnAsync(conn, "TaskItems",    "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "Meetings",     "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "Decisions",    "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "PersonalNotes","UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "Users",        "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "QueryRecords", "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "DataSources",  "UpdatedAt", "DATETIME2 NULL");
await EnsureColumnAsync(conn, "ApprovalRequests","UpdatedAt","DATETIME2 NULL");
await EnsureColumnAsync(conn, "Objectives",   "UpdatedAt", "DATETIME2 NULL");
```

Tüm Dapper UPDATE sorgularında `UpdatedAt = SYSUTCDATETIME()` ekle.  
EF migration'a gerek yok — InfrastructureSeed çalıştırır.

---

### 1.2 — SessionService Null Safety

**Dosya:** `yonetiq/Data/Services/AI/ContextBuilderService.cs`

`sessionSvc.ActiveUser` null dönebilir. Her kullanım noktasında guard ekle:

```csharp
// BuildContextAsync başına ekle
var activeUser = sessionSvc.ActiveUser;
if (activeUser is null || !activeUser.IsLoggedIn)
{
    logger.LogWarning("ContextBuilder: ActiveUser null veya login değil");
    return context; // boş context dön
}
context.User = activeUser;
```

Aynı kontrolü `AiOrchestrationService.ProcessRequestAsync`'ta da uygula:
```csharp
// Mevcut UserId <= 0 kontrolünden önce:
if (request.UserId <= 0 || sessionSvc?.ActiveUser?.IsLoggedIn != true)
{
    return AiResponse.Failure("Oturum süresi dolmuş. Lütfen tekrar giriş yapın.", "AuthError");
}
```

---

### 1.3 — Schema Detection Cache

**Dosya:** `yonetiq/Data/Services/QueryService.cs`

Mevcut yapı her sorguda DB round-trip yapıyor. Static field ile çöz:

```csharp
public class QueryService(...) : BaseService(...)
{
    // Mevcut field'ların altına ekle:
    private static bool? _hasAllowDmlColumn;
    private static bool? _hasUsagePurposeColumn;
    private static readonly SemaphoreSlim _schemaCacheLock = new(1, 1);

    private async Task<bool> HasAllowDmlColumnAsync(SqlConnection conn)
    {
        if (_hasAllowDmlColumn.HasValue) return _hasAllowDmlColumn.Value;
        await _schemaCacheLock.WaitAsync();
        try
        {
            if (_hasAllowDmlColumn.HasValue) return _hasAllowDmlColumn.Value;
            var result = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN COL_LENGTH('dbo.QueryRecords','AllowDml') IS NULL 
                THEN 0 ELSE 1 END");
            _hasAllowDmlColumn = result == 1;
            return _hasAllowDmlColumn.Value;
        }
        finally { _schemaCacheLock.Release(); }
    }

    private async Task<bool> HasUsagePurposeColumnAsync(SqlConnection conn)
    {
        if (_hasUsagePurposeColumn.HasValue) return _hasUsagePurposeColumn.Value;
        await _schemaCacheLock.WaitAsync();
        try
        {
            if (_hasUsagePurposeColumn.HasValue) return _hasUsagePurposeColumn.Value;
            var result = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN COL_LENGTH('dbo.QueryRecords','UsagePurposeLookupId') IS NULL 
                THEN 0 ELSE 1 END");
            _hasUsagePurposeColumn = result == 1;
            return _hasUsagePurposeColumn.Value;
        }
        finally { _schemaCacheLock.Release(); }
    }
}
```

---

### 1.4 — NotificationService Memory Leak Fix

**Dosya:** `yonetiq/Components/Layout/MainLayout.razor`

```razor
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        NotificationService.OnNotificationCreated += HandleNotificationAsync;
    }

    private async Task HandleNotificationAsync(int userId)
    {
        if (userId == SessionSvc.ActiveUser?.UserId)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        NotificationService.OnNotificationCreated -= HandleNotificationAsync;
    }
}
```

---

### 1.5 — Sayfa Yetki Kontrolü

Her Admin sayfasının `@code` bloğunun `OnInitializedAsync`'ına ekle:

```csharp
// Admin sayfaları: AiDashboard, AuditLog, KpiTargets, Settings, UserList
protected override async Task OnInitializedAsync()
{
    if (!SessionSvc.IsLoggedIn)
    { Nav.NavigateTo("/giris"); return; }
    
    if (SessionSvc.ActiveUser.Role is not ("Admin" or "Yönetici"))
    { Nav.NavigateTo("/"); return; }
    
    // ... mevcut kod
}
```

---

### 1.6 — PromptEngine Hot-Reload (Development)

**Dosya:** `yonetiq/Data/Services/AI/PromptEngine.cs`

```csharp
public async Task<string> LoadPromptAsync(string fileName)
{
    // Development'ta cache bypass — her sorguda disk'ten oku
    if (env.IsDevelopment())
    {
        var diskPath = Path.Combine(env.ContentRootPath, "Data", "AiSkills", "Prompts", fileName);
        if (File.Exists(diskPath))
        {
            var raw = await File.ReadAllTextAsync(diskPath);
            var (version, content) = ParseVersionHeader(raw);
            _cache[fileName] = (version, content); // yine cache'le ama her seferinde oku
            return content;
        }
    }
    
    // Production: mevcut cache mantığı aynen devam
    if (_cache.TryGetValue(fileName, out var cached)) return cached.Content;
    // ... geri kalan mevcut kod
}
```

---

## BÖLÜM 2 — AI ALTYAPISI: KAPATILMASI GEREKEN BOŞLUKLAR

### 2.1 — SuggestedAction Rendering (EN ÖNEMLİ)

Şu an `AiResponse.Actions` listesi hiçbir yerde gösterilmiyor.  
**Dosya:** `yonetiq/Components/Shared/AI/SkillResultCard.razor` (varsa) veya `AiSidebar.razor`

```razor
@* Mevcut content render'ının ALTINA ekle *@
@if (Response?.Actions?.Count > 0)
{
    <div class="yi-ai-actions mt-3 d-flex flex-wrap gap-2">
        @foreach (var action in Response.Actions)
        {
            <button class="btn btn-sm yi-btn-outline yi-btn-ai-action"
                    @onclick="() => ExecuteActionAsync(action)">
                <i class="bi @action.Icon"></i> @action.Label
            </button>
        }
    </div>
}

@code {
    private async Task ExecuteActionAsync(SuggestedAction action)
    {
        switch (action.Type)
        {
            case ActionType.ConvertToTask:
                var title = action.Parameters.TryGetValue("title", out var t) 
                    ? t?.ToString() : "Yeni Görev";
                // TaskService.SaveAsync çağır
                var taskResult = await TaskSvc.SaveAsync(new TaskItem 
                { 
                    Title = title ?? "Yeni Görev",
                    SourceEntityType = action.Parameters.TryGetValue("sourceType", out var st) 
                        ? st?.ToString() : null,
                    SourceEntityId = action.Parameters.TryGetValue("sourceId", out var si) 
                        ? Convert.ToInt32(si) : null,
                    AssigneeId = SessionSvc.ActiveUser.UserId,
                    AssignedAt = DateTime.Today
                }, SessionSvc.ActiveUser.UserId);
                if (taskResult.IsSuccess)
                    Toast.ShowSuccess($"Görev oluşturuldu: {title}");
                break;

            case ActionType.CopyToClipboard:
                await JS.InvokeVoidAsync("navigator.clipboard.writeText", Response?.Content);
                Toast.ShowSuccess("Kopyalandı");
                break;

            case ActionType.NavigateTo:
                if (action.Parameters.TryGetValue("url", out var url))
                    Nav.NavigateTo(url?.ToString() ?? "/");
                break;
        }
    }
}
```

---

### 2.2 — Toplantı Hazırlık Otomatı

**Dosya:** `yonetiq/Components/Pages/Meeting/MeetingDetail.razor`

```csharp
protected override async Task OnInitializedAsync()
{
    // ... mevcut kod ...
    
    // YENİ: Toplantı 4 saat içindeyse otomatik hazırlık skill'i çalıştır
    if (_meeting is not null && 
        _meeting.MeetingDate > DateTime.UtcNow &&
        _meeting.MeetingDate <= DateTime.UtcNow.AddHours(4) &&
        SessionSvc.IsLoggedIn)
    {
        _ = Task.Run(async () =>
        {
            var req = new AiRequest
            {
                SkillId = "meeting.preparation",
                UserId = SessionSvc.ActiveUser.UserId,
                Module = "meeting",
                Parameters = new() { ["meetingId"] = _meeting.Id }
            };
            _preparationResponse = await AiOrchestration.ProcessRequestAsync(req);
            await InvokeAsync(StateHasChanged);
        });
    }
}
```

Razor'a ekle (toplantı bilgileri kartının altına):
```razor
@if (_preparationResponse is { IsSuccess: true })
{
    <div class="yi-card yi-ai-briefing mt-3">
        <div class="yi-card-header d-flex align-items-center gap-2">
            <i class="bi bi-stars yi-ai-icon"></i>
            <span class="fw-semibold">Toplantı Hazırlık Özeti</span>
            <ConfidenceBadge Confidence="@_preparationResponse.Confidence" />
        </div>
        <div class="yi-card-body">
            <div class="yi-ai-content">@((MarkupString)FormatContent(_preparationResponse.Content))</div>
            @if (_preparationResponse.Actions?.Count > 0)
            {
                @* SuggestedAction butonları *@
            }
            <AiFeedbackWidget InteractionId="@_preparationResponse.InteractionId!.Value"
                              UserId="@SessionSvc.ActiveUser.UserId" />
        </div>
    </div>
}
```

---

### 2.3 — Görev Kaydetme Sonrası AI Değerlendirmesi

**Dosya:** `yonetiq/Components/Pages/Task/TaskForm.razor`

Mevcut `HandleSaveAsync` metoduna ekle:

```csharp
private async Task HandleSaveAsync()
{
    // ... mevcut kaydetme kodu ...
    
    if (saveResult.IsSuccess)
    {
        Toast.ShowSuccess("Görev kaydedildi.");
        
        // YENİ: Arka planda risk + netleştirme analizi
        _ = RunPostSaveAiAsync(savedTaskId);
        
        Nav.NavigateTo("/gorevler");
    }
}

private async Task RunPostSaveAiAsync(int taskId)
{
    var req = new AiRequest
    {
        SkillId = "task.risk",
        UserId = SessionSvc.ActiveUser.UserId,
        Module = "task",
        Parameters = new() { ["taskId"] = taskId }
    };
    var response = await AiOrchestration.ProcessRequestAsync(req);
    if (response is { IsSuccess: true } && response.Confidence > 0.5f)
    {
        // Bildirim olarak kaydet — kullanıcı görev listesinde görsün
        await NotificationSvc.CreateAsync(
            SessionSvc.ActiveUser.UserId,
            "AiInsight",
            $"Görev riski tespit edildi",
            response.Content[..Math.Min(200, response.Content.Length)] + "...",
            $"/gorev/duzenle/{taskId}"
        );
    }
}
```

---

### 2.4 — Not Kaydedilince Otomatik Etiket + Aksiyon

**Dosya:** `yonetiq/Components/Pages/Notes/Notes.razor`

```csharp
private async Task HandleNoteSaveAsync()
{
    // ... mevcut kaydetme kodu ...
    
    if (saveResult.IsSuccess)
    {
        _ = RunNoteAiAnalysisAsync(savedNoteId);
    }
}

private async Task RunNoteAiAnalysisAsync(int noteId)
{
    await Task.Delay(1500); // kullanıcı sayfada kalırsa banner görünsün
    
    var tagReq = new AiRequest
    {
        SkillId = "note.suggest_tags",
        UserId = SessionSvc.ActiveUser.UserId,
        Module = "note",
        Parameters = new() { ["noteId"] = noteId }
    };
    var tagResponse = await AiOrchestration.ProcessRequestAsync(tagReq);
    
    var actionReq = new AiRequest
    {
        SkillId = "note.extract_actions",
        UserId = SessionSvc.ActiveUser.UserId,
        Module = "note",
        Parameters = new() { ["noteId"] = noteId }
    };
    var actionResponse = await AiOrchestration.ProcessRequestAsync(actionReq);
    
    // Banner'ı göster
    _noteSuggestionBanner = BuildSuggestionSummary(tagResponse, actionResponse);
    await InvokeAsync(StateHasChanged);
}
```

---

### 2.5 — Anomali → Otomatik Bildirim Zinciri

**Dosya:** `yonetiq/Data/Services/ScheduledReportWorker.cs`

Mevcut rapor çalıştırma adımından sonra:

```csharp
// Rapor çalıştırıldıktan sonra anomali kontrolü
var result = await reportingService.RunAsync(report.QueryRecordId, ...);
if (result.IsSuccess && result.Data is not null)
{
    // StatisticalAnalyzer ile anomali tespiti
    var anomalies = StatisticalAnalyzer.DetectOutliers(result.Data);
    if (anomalies.Count > 0)
    {
        // AI ile anomali yorumu
        var req = new AiRequest
        {
            SkillId = "report.anomaly",
            UserId = report.CreatedBy,
            Module = "query",
            Parameters = new() 
            { 
                ["queryId"] = report.QueryRecordId,
                ["anomalyCount"] = anomalies.Count
            }
        };
        var aiResponse = await aiOrchestration.ProcessRequestAsync(req);
        
        if (aiResponse.IsSuccess)
        {
            await notificationService.CreateAsync(
                report.CreatedBy,
                "AnomalyAlert",
                $"{report.Title} — Anormallik Tespit Edildi",
                aiResponse.Content[..Math.Min(300, aiResponse.Content.Length)],
                $"/raporlar/calistir/{report.QueryRecordId}"
            );
        }
    }
}
```

---

### 2.6 — Golden Memory Feedback Loop

**Dosya:** `yonetiq/Data/Services/AI/ContinuousLearningService.cs` — **YENİ SINIF**

```csharp
using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Kullanıcı onaylarından ve SQL düzeltmelerinden öğrenme döngüsünü yönetir.
/// Thumbs-up veya SQL correction → AiPattern taslağı → admin onayı → golden memory.
/// </summary>
public class ContinuousLearningService(
    IConfiguration config, AuditService auditSvc,
    ILogger<ContinuousLearningService> logger)
    : BaseService(config, auditSvc)
{
    /// <summary>
    /// Kullanıcı thumbs-up verdiğinde çağrılır.
    /// Duplicate pattern önlenir; admin onayı bekler.
    /// </summary>
    public async Task<ServiceResult> ProposeFromFeedbackAsync(int interactionId, int userId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var interaction = await conn.QueryFirstOrDefaultAsync<AiInteraction>(@"
                SELECT Id, SkillId, InputSummary, OutputSummary, ConfidenceScore
                FROM AiInteractions WHERE Id = @Id", new { Id = interactionId });

            if (interaction is null) return;

            var exists = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM AiPatterns 
                WHERE SkillId = @SkillId AND InputPattern = @Input",
                new { interaction.SkillId, Input = interaction.InputSummary });

            if (exists > 0) return;

            await conn.ExecuteAsync(@"
                INSERT INTO AiPatterns 
                    (SkillId, InputPattern, ApprovedOutput, IsApproved,
                     SourceInteractionId, ProposedByUserId, CreatedAt)
                VALUES (@SkillId, @Input, @Output, 0, @IId, @UserId, SYSUTCDATETIME())",
                new {
                    interaction.SkillId,
                    Input  = interaction.InputSummary,
                    Output = interaction.OutputSummary,
                    IId    = interactionId,
                    UserId = userId
                });

            LogAction("AI", "ProposePattern", new { interactionId });
        });
    }

    /// <summary>
    /// Kullanıcı NlToSql çıktısını düzeltip çalıştırdığında çağrılır.
    /// Anlamlı fark varsa (similarity 0.1-0.95 arası) pattern olarak önerir.
    /// </summary>
    public async Task<ServiceResult> RecordSqlCorrectionAsync(
        int interactionId, int userId, string originalSql, string correctedSql)
    {
        var sim = ComputeSimilarity(originalSql, correctedSql);
        if (sim > 0.95) return ServiceResult.Success("Fark önemsiz");
        if (sim < 0.10) return ServiceResult.Success("Çok farklı — sıfırdan yazıldı");

        return await ProposeFromCorrectionAsync(interactionId, userId, correctedSql);
    }

    private async Task<ServiceResult> ProposeFromCorrectionAsync(
        int interactionId, int userId, string correctedSql)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var interaction = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT SkillId, InputSummary FROM AiInteractions WHERE Id = @Id",
                new { Id = interactionId });

            if (interaction is null) return;

            await conn.ExecuteAsync(@"
                INSERT INTO AiPatterns
                    (SkillId, InputPattern, ApprovedOutput, IsApproved,
                     SourceInteractionId, ProposedByUserId, IsCorrectionDerived, CreatedAt)
                VALUES (@SkillId, @Input, @Output, 0, @IId, @UserId, 1, SYSUTCDATETIME())",
                new {
                    SkillId = (string)interaction.SkillId,
                    Input   = (string)interaction.InputSummary,
                    Output  = correctedSql,
                    IId     = interactionId,
                    UserId  = userId
                });

            LogAction("AI", "SqlCorrectionPattern", new { interactionId });
        });
    }

    private static double ComputeSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)LevenshteinDistance(a, b) / maxLen;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int m = s.Length, n = t.Length;
        var d = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;
        for (int i = 1; i <= m; i++)
        for (int j = 1; j <= n; j++)
            d[i, j] = s[i-1] == t[j-1] ? d[i-1, j-1] :
                1 + Math.Min(d[i-1, j], Math.Min(d[i, j-1], d[i-1, j-1]));
        return d[m, n];
    }
}
```

**AiPatterns tablosuna kolon ekle** (`InfrastructureSeed`'e):
```csharp
await EnsureColumnAsync(conn, "AiPatterns", "IsCorrectionDerived", "BIT NOT NULL DEFAULT 0");
await EnsureColumnAsync(conn, "AiPatterns", "ProposedByUserId",     "INT NULL");
await EnsureColumnAsync(conn, "AiPatterns", "SourceInteractionId",  "INT NULL");
```

**DI kaydı** (`Program.cs`):
```csharp
builder.Services.AddScoped<YonetIQ.Data.Services.AI.ContinuousLearningService>();
```

---

### 2.7 — AiFeedbackWidget Güncelleme

**Dosya:** `yonetiq/Components/Shared/AI/AiFeedbackWidget.razor`

```razor
@inject ContinuousLearningService LearningService

<div class="yi-ai-feedback d-flex align-items-center gap-2 mt-2">
    @if (!_submitted)
    {
        <span class="yi-ai-feedback-label">Bu faydalı mıydı?</span>
        <button class="btn btn-sm yi-btn-ghost" @onclick='() => SubmitFeedbackAsync(3, true)'>
            <i class="bi bi-hand-thumbs-up"></i>
        </button>
        <button class="btn btn-sm yi-btn-ghost" @onclick='() => SubmitFeedbackAsync(1, false)'>
            <i class="bi bi-hand-thumbs-down"></i>
        </button>
    }
    else
    {
        <span class="text-success small"><i class="bi bi-check-circle"></i> Kaydedildi</span>
        @if (_wasAccepted && !_patternProposed)
        {
            <button class="btn btn-sm yi-btn-ghost" @onclick="ProposeAsPatternAsync"
                    title="Bu yanıtı örnek olarak kaydet">
                <i class="bi bi-bookmark-plus"></i> Örnek kaydet
            </button>
        }
        @if (_patternProposed)
        {
            <span class="text-muted small"><i class="bi bi-bookmark-check"></i> Örnek önerildi</span>
        }
    }
</div>

@code {
    [Parameter] public int InteractionId { get; set; }
    [Parameter] public int UserId { get; set; }

    private bool _submitted;
    private bool _wasAccepted;
    private bool _patternProposed;

    private async Task SubmitFeedbackAsync(int rating, bool accepted)
    {
        _wasAccepted = accepted;
        var feedback = new AiFeedback
        {
            InteractionId = InteractionId, UserId = UserId,
            Rating = rating, WasAccepted = accepted
        };
        await AiOrchestration.SubmitFeedbackAsync(feedback);
        _submitted = true;
    }

    private async Task ProposeAsPatternAsync()
    {
        await LearningService.ProposeFromFeedbackAsync(InteractionId, UserId);
        _patternProposed = true;
    }
}
```

---

### 2.8 — QueryEditor SQL Correction Tracking

**Dosya:** `yonetiq/Components/Pages/Query/QueryEditor.razor`

```csharp
// @code içine field'lar ekle:
private string _aiGeneratedSql = string.Empty;
private int? _lastAiInteractionId;

// AI SQL üretildiğinde kaydet:
private async Task HandleAiGenerateAsync()
{
    // ... mevcut AI generate kodu ...
    if (aiResponse.IsSuccess)
    {
        _aiGeneratedSql = ExtractSqlFromResponse(aiResponse.Content);
        _lastAiInteractionId = aiResponse.InteractionId;
        _sqlEditor = _aiGeneratedSql; // editor'a koy
    }
}

// Çalıştır butonunda, SQL değiştiyse kaydet:
private async Task HandleRunQueryAsync()
{
    if (_lastAiInteractionId.HasValue && 
        !string.IsNullOrEmpty(_aiGeneratedSql) &&
        _sqlEditor != _aiGeneratedSql)
    {
        _ = LearningService.RecordSqlCorrectionAsync(
            _lastAiInteractionId.Value,
            SessionSvc.ActiveUser.UserId,
            _aiGeneratedSql,
            _sqlEditor);
    }
    
    // ... mevcut çalıştırma kodu ...
}
```

---

### 2.9 — Semantic Enricher (NlToSql için)

**Dosya:** `yonetiq/Data/Services/AI/SemanticEnricher.cs` — **YENİ SINIF**

```csharp
using YonetIQ.Data.Services;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Kullanıcı sorgusundaki iş terimlerini SemanticDefinitions'dan çözerek
/// AI bağlamına ekler. "geçen ay" → tarih SQL, "ciro" → SUM(NetAmount).
/// </summary>
public class SemanticEnricher(SemanticService semanticSvc)
{
    public async Task<string> EnrichAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return string.Empty;

        var termsResult = await semanticSvc.SearchAsync(userInput);
        if (termsResult is not { IsSuccess: true, Data.Count: > 0 })
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## Şirket İş Terimleri Sözlüğü");
        sb.AppendLine("Bu terimleri SQL oluştururken kullan. SqlExpression varsa direkt kullan:");
        sb.AppendLine();

        foreach (var def in termsResult.Data.Take(10))
        {
            sb.AppendLine($"**{def.BusinessName}**");
            if (!string.IsNullOrWhiteSpace(def.Description))
                sb.AppendLine($"  → {def.Description}");
            if (!string.IsNullOrWhiteSpace(def.SqlExpression))
                sb.AppendLine($"  SQL: `{def.SqlExpression}`");
            if (!string.IsNullOrWhiteSpace(def.Aliases))
                sb.AppendLine($"  Eşanlamlılar: {def.Aliases}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

**ContextBuilderService'e entegrasyon** — `BuildQueryContextAsync` içine:
```csharp
// Semantic enrichment — NlToSql bağlamını zenginleştir
if (!string.IsNullOrWhiteSpace(request.UserInput))
{
    var enriched = await _semanticEnricher.EnrichAsync(request.UserInput);
    if (!string.IsNullOrWhiteSpace(enriched))
    {
        context.Entities["semantic_terms"] = enriched;
        summaryParts.Add("semantik terimler çözümlendi");
    }
}
```

**SkillExecutor.BuildVariables** içine ekle:
```csharp
vars["{{semantic_terms}}"] = context.Entities.TryGetValue("semantic_terms", out var st) 
    ? st?.ToString() ?? "" : "";
```

**DI kaydı** (`Program.cs`):
```csharp
builder.Services.AddScoped<YonetIQ.Data.Services.AI.SemanticEnricher>();
```

**`ContextBuilderService` constructor'una** `SemanticEnricher _semanticEnricher` ekle.

---

### 2.10 — Admin: Bekleyen Pattern Onay Sayfası

**Dosya:** `yonetiq/Components/Pages/Admin/AiDashboard.razor`

Mevcut AiDashboard'a yeni bir sekme ekle: "Bekleyen Öneriler"

```razor
@* Tab navigation'a ekle *@
<button class="nav-link @(_activeTab == "patterns" ? "active" : "")" 
        @onclick='() => _activeTab = "patterns"'>
    Bekleyen Öneriler
    @if (_pendingPatternCount > 0)
    {
        <span class="badge bg-warning ms-1">@_pendingPatternCount</span>
    }
</button>

@* Tab içeriği *@
@if (_activeTab == "patterns")
{
    @foreach (var pattern in _pendingPatterns)
    {
        <div class="yi-card mb-2">
            <div class="yi-card-body">
                <div class="fw-semibold small mb-1">@pattern.SkillId</div>
                <div class="text-muted small mb-1">Girdi: @pattern.InputPattern</div>
                <pre class="yi-pre small">@pattern.ApprovedOutput</pre>
                <div class="d-flex gap-2 mt-2">
                    <button class="btn btn-sm btn-success" 
                            @onclick="() => ApprovePatternAsync(pattern.Id)">
                        <i class="bi bi-check"></i> Onayla
                    </button>
                    <button class="btn btn-sm btn-outline-danger"
                            @onclick="() => RejectPatternAsync(pattern.Id)">
                        <i class="bi bi-x"></i> Reddet
                    </button>
                </div>
            </div>
        </div>
    }
}
```

**AiEvaluationService'e** metot ekle:
```csharp
public async Task<ServiceResult<List<AiPattern>>> GetPendingPatternsAsync()
{
    return await ExecuteServiceAsync<List<AiPattern>>(async conn =>
    {
        return (await conn.QueryAsync<AiPattern>(@"
            SELECT Id, SkillId, InputPattern, ApprovedOutput, 
                   IsCorrectionDerived, ProposedByUserId, CreatedAt
            FROM AiPatterns WHERE IsApproved = 0
            ORDER BY CreatedAt DESC")).ToList();
    });
}

public async Task<ServiceResult> ApprovePatternAsync(int patternId)
{
    return await ExecuteServiceAsync(async conn =>
    {
        await conn.ExecuteAsync(@"
            UPDATE AiPatterns SET IsApproved = 1, ApprovedAt = SYSUTCDATETIME()
            WHERE Id = @Id", new { Id = patternId });
    });
}

public async Task<ServiceResult> RejectPatternAsync(int patternId)
{
    return await ExecuteServiceAsync(async conn =>
    {
        await conn.ExecuteAsync("DELETE FROM AiPatterns WHERE Id = @Id", 
            new { Id = patternId });
    });
}
```

`AiPatterns` tablosuna kolon ekle:
```csharp
await EnsureColumnAsync(conn, "AiPatterns", "ApprovedAt", "DATETIME2 NULL");
```

---

## BÖLÜM 3 — YENİ SKILL'LER

### 3.1 — Mevcut Skill Definition Dosyaları

**Dosya:** `yonetiq/Data/AiSkills/Definitions/TaskSkills.cs`

```csharp
// Mevcut skill'lerin ALTINA ekle:
public static SkillDefinition Assign => new()
{
    Id = "task.assign",
    Name = "Atama Önerisi",
    Description = "Görevi atamak için en uygun kişiyi önerir: iş yükü + gecikme geçmişi analizi",
    Category = SkillCategory.Task,
    TriggerMode = TriggerMode.Reactive,
    Module = "task",
    Icon = "bi-person-check",
    RequiredContext = ["team_workload", "assignee_history"],
    OutputType = SkillOutputType.Suggestion,
    Temperature = 0.2f,
    SystemPromptFile = "task.assign.system.md",
    UserPromptFile = "task.assign.user.md",
    MaxOutputLength = 800,
    HallucinationRisk = RiskLevel.Low,
    SuccessCriteria = "Önerilen kişi kabul edildi veya gerekçe değerlendirildi",
    DependencyServices = ["TaskService", "UserService"]
};
```

**Dosya:** `yonetiq/Data/AiSkills/Definitions/MeetingSkills.cs`

```csharp
public static SkillDefinition Quality => new()
{
    Id = "meeting.quality",
    Name = "Toplantı Verimlilik Skoru",
    Description = "Toplantı süresi × katılımcı sayısı / çıktı (karar + aksiyon) oranını hesaplar",
    Category = SkillCategory.Meeting,
    TriggerMode = TriggerMode.Proactive,
    Module = "meeting",
    Icon = "bi-graph-up-arrow",
    RequiredEntities = ["Meeting"],
    RequiredContext = ["meeting_decisions", "meeting_actions", "attendee_count"],
    OutputType = SkillOutputType.Text,
    Temperature = 0.1f,
    SystemPromptFile = "meeting.quality.system.md",
    UserPromptFile = "meeting.quality.user.md",
    MaxOutputLength = 600,
    HallucinationRisk = RiskLevel.Low,
    SuccessCriteria = "Skor hesaplaması doğrulanabilir verilerle desteklendi",
    DependencyServices = ["MeetingService"]
};
```

**Program.cs**'e ekle:
```csharp
registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Assign);
registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Quality);
```

---

### 3.2 — Skill Prompt Dosyaları

**Dosya:** `yonetiq/Data/AiSkills/Prompts/task.assign.system.md`
```markdown
---version: 1---
Sen BKM Kitap'ın görev atama asistanısın.
Verilen ekip üyelerinin mevcut iş yükünü ve gecikme geçmişini analiz et.
Göreve en uygun kişiyi gerekçesiyle öner.
Kısa ve net ol. Maksimum 3 paragraf.
```

**Dosya:** `yonetiq/Data/AiSkills/Prompts/task.assign.user.md`
```markdown
---version: 1---
Görev: {{task_title}}
Tahmini süre: {{estimated_hours}} saat

Ekip iş yükü:
{{team_workload}}

Gecikme geçmişi:
{{assignee_history}}

Bu görevi kime atamalıyız? Gerekçeni açıkla.
```

**Dosya:** `yonetiq/Data/AiSkills/Prompts/meeting.quality.system.md`
```markdown
---version: 1---
Sen toplantı verimliliği analizcisisin.
Katılımcı sayısı × süre (dakika) = toplam insan-dakika maliyeti.
Toplam karar + aksiyon sayısına böl = birim başı maliyet.
Sektör ortalamasıyla karşılaştır (iyi toplantı: 10-15 insan-dakika/çıktı).
Net bir skor ve 1-2 öneri ver.
```

**Dosya:** `yonetiq/Data/AiSkills/Prompts/meeting.quality.user.md`
```markdown
---version: 1---
Toplantı: {{meeting_title}}
Tarih: {{meeting_date}}
Süre: {{duration_minutes}} dakika
Katılımcı sayısı: {{attendee_count}}
Kararlar: {{decision_count}}
Aksiyonlar: {{action_count}}

Bu toplantının verimlilik skorunu hesapla ve yorumla.
```

---

## BÖLÜM 4 — BKM KİTAP DOMAIN ZEKASI

### 4.1 — Semantic Definitions Seed (BKM'ye Özel)

**Dosya:** `yonetiq/Data/SeedData.cs`

Mevcut `SeedAsync` metoduna ekle — bir kez çalışacak:

```csharp
await SeedSemanticDefinitionsAsync(serviceProvider);
```

Yeni metot:
```csharp
private static async Task SeedSemanticDefinitionsAsync(IServiceProvider sp)
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")!;
    
    await using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    
    // Zaten var mı?
    var count = await conn.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM SemanticDefinitions WHERE IsActive = 1");
    if (count > 0) return; // seed'i tekrar çalıştırma
    
    var defs = new[]
    {
        // Ölçüler
        ("measure","SalesLines","NetAmount","ciro",
         "Vergi hariç net satış tutarı","SUM(NetAmount)",
         "ciro,satış,gelir,hasılat,cirolar,toplam satış"),
        
        ("measure","SalesLines","Quantity","satış adedi",
         "Satılan ürün/kitap adedi","SUM(Quantity)",
         "adet,kaç sattı,satış sayısı,kaç adet,kaç tane"),
        
        ("measure","SalesLines","GrossProfit","brüt kâr",
         "Satış fiyatı - maliyet farkı","SUM(GrossProfit)",
         "kâr,brüt kâr,kârlılık,kazanç"),
        
        ("measure","SalesLines","GrossMarginPct","kâr marjı",
         "Brüt kâr / ciro yüzdesi",
         "AVG(CASE WHEN NetAmount > 0 THEN GrossProfit*100.0/NetAmount ELSE NULL END)",
         "marj,kâr marjı,yüzde kâr"),
        
        // Boyutlar
        ("dimension","Stores","StoreName","mağaza",
         "Mağaza veya şube adı","StoreName",
         "mağaza,şube,lokasyon,mağazalar,şubeler,hangi mağaza"),
        
        ("dimension","Products","CategoryName","kategori",
         "Ürün ana kategorisi (Roman, Çocuk, Akademik vb.)","CategoryName",
         "kategori,ürün grubu,bölüm,kategoriler,tür"),
        
        ("dimension","Authors","AuthorName","yazar",
         "Kitabın yazar adı","AuthorName",
         "yazar,kim yazmış,yazarlar,kim tarafından"),
        
        ("dimension","Publishers","PublisherName","yayınevi",
         "Kitabı yayınlayan kuruluş","PublisherName",
         "yayınevi,yayıncı,kim bastı"),
        
        ("dimension","Products","ISBN","ISBN",
         "Uluslararası kitap numarası","ISBN",
         "isbn,kitap kodu,barkod"),
        
        // Tarih aralıkları
        ("date_range","SalesLines","SaleDate","geçen ay",
         "Bir önceki takvim ayı",
         "SaleDate >= DATEADD(month,DATEDIFF(month,0,GETDATE())-1,0) AND SaleDate < DATEADD(month,DATEDIFF(month,0,GETDATE()),0)",
         "geçen ay,önceki ay,son ay,geçen ay boyunca"),
        
        ("date_range","SalesLines","SaleDate","bu ay",
         "Cari takvim ayı",
         "SaleDate >= DATEADD(month,DATEDIFF(month,0,GETDATE()),0) AND SaleDate < GETDATE()",
         "bu ay,mevcut ay,aylık"),
        
        ("date_range","SalesLines","SaleDate","geçen yıl",
         "Bir önceki takvim yılı",
         "YEAR(SaleDate) = YEAR(GETDATE())-1",
         "geçen yıl,önceki yıl,geçen sene"),
        
        ("date_range","SalesLines","SaleDate","bu yıl",
         "Cari takvim yılı başından bugüne",
         "YEAR(SaleDate) = YEAR(GETDATE())",
         "bu yıl,cari yıl,yılbaşından beri"),
        
        ("date_range","SalesLines","SaleDate","son 30 gün",
         "Bugünden geriye 30 gün",
         "SaleDate >= DATEADD(day,-30,GETDATE())",
         "son 30 gün,son bir ay,30 günlük"),
        
        ("date_range","SalesLines","SaleDate","son 7 gün",
         "Bugünden geriye 7 gün",
         "SaleDate >= DATEADD(day,-7,GETDATE())",
         "son hafta,son 7 gün,haftalık"),
        
        // KPI hesaplamaları
        ("kpi","SalesLines",null,"büyüme oranı",
         "Dönem büyümesi yüzde olarak",
         "(SUM(bu_donem) - SUM(gecen_donem)) * 100.0 / NULLIF(SUM(gecen_donem),0)",
         "büyüme,artış,değişim,yüzde değişim,büyüdü mü"),
        
        // Filtreler
        ("filter","Products","IsActive","aktif ürün",
         "Stokta ve satışa açık ürünler",
         "IsActive = 1 AND StockQuantity > 0",
         "aktif,mevcut,satışta olan,stokta olan"),
        
        ("filter","Products",null,"kritik stok",
         "7 günün altında stoku kalan ürünler",
         "StockQuantity < (AvgDailySales * 7) AND StockQuantity > 0",
         "bitmek üzere,kritik stok,az kalan,tükeniyor"),
        
        ("filter","Products",null,"sıfır stok",
         "Hiç stoğu kalmayan ürünler",
         "StockQuantity = 0 OR StockQuantity IS NULL",
         "stok yok,tükenmiş,sıfır stok"),
        
        ("filter","Sales",null,"kampanyalı satış",
         "İndirim uygulanmış satışlar",
         "DiscountAmount > 0",
         "kampanyalı,indirimli,promosyonlu"),
    };
    
    foreach (var (termType,table,col,bizName,desc,sqlExpr,aliases) in defs)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO SemanticDefinitions 
                (TermType,TableName,ColumnName,BusinessName,Description,
                 SqlExpression,Aliases,IsActive,CreatedAt)
            VALUES (@tt,@tn,@cn,@bn,@d,@sq,@al,1,SYSUTCDATETIME())",
            new { tt=termType,tn=table,cn=col,bn=bizName,d=desc,sq=sqlExpr,al=aliases });
    }
    
    logger.LogInformation("SemanticDefinitions seed: {Count} kayıt eklendi", defs.Length);
}
```

---

### 4.2 — BKM Özel Proaktif Insight'lar

**Dosya:** `yonetiq/Data/Services/AI/ProactiveInsightService.cs`

Mevcut `GenerateInsightsAsync`'ın sonuna ekle:

```csharp
// BKM: Kritik stok uyarısı
var criticalStockResult = await querySvc.ExecuteAsync(@"
    SELECT COUNT(*) as Cnt FROM Products 
    WHERE IsActive = 1 
    AND StockQuantity < (SELECT AVG(Quantity) * 7 
                          FROM SalesLines 
                          WHERE SaleDate >= DATEADD(day,-30,GETDATE()) 
                          AND ProductId = Products.Id)
    AND StockQuantity > 0");

if (criticalStockResult?.Rows?.Count > 0)
{
    var cnt = Convert.ToInt32(criticalStockResult.Rows[0]["Cnt"] ?? 0);
    if (cnt > 0)
    {
        insights.Add(new AiInsight
        {
            Type = InsightType.Risk,
            Priority = cnt >= 10 ? InsightPriority.Critical : InsightPriority.High,
            Title = $"{cnt} üründe kritik stok",
            Detail = "Ortalama satış hızına göre 7 günden az stoğu kalan ürünler.",
            Module = "query",
            NavigateUrl = "/sorgular?filter=kritik-stok",
            Icon = "bi-box-seam",
            IconColor = "text-warning",
            NumericValue = cnt
        });
    }
}

// BKM: Tükenen ürünler
var outOfStockResult = await querySvc.ExecuteAsync(@"
    SELECT COUNT(*) as Cnt FROM Products 
    WHERE IsActive = 1 AND (StockQuantity = 0 OR StockQuantity IS NULL)");

if (outOfStockResult?.Rows?.Count > 0)
{
    var cnt = Convert.ToInt32(outOfStockResult.Rows[0]["Cnt"] ?? 0);
    if (cnt > 0)
    {
        insights.Add(new AiInsight
        {
            Type = InsightType.Risk,
            Priority = InsightPriority.High,
            Title = $"{cnt} ürün stokta yok",
            Detail = "Aktif ürünlerde sıfır stok. Satış kaybı yaşanıyor olabilir.",
            Module = "query",
            NavigateUrl = "/sorgular?filter=sifir-stok",
            Icon = "bi-exclamation-circle-fill",
            IconColor = "text-danger",
            NumericValue = cnt
        });
    }
}

// BKM: OKR güncellenmemiş KR uyarısı
var staleKrResult = await okrSvc.ListAsync(year: DateTime.UtcNow.Year);
if (staleKrResult is { IsSuccess: true, Data: not null })
{
    var staleCount = staleKrResult.Data
        .SelectMany(o => o.KeyResults ?? [])
        .Count(kr => kr.Progress < 100 && 
                     kr.UpdatedAt < DateTime.UtcNow.AddDays(-21));
    
    if (staleCount > 0)
    {
        insights.Add(new AiInsight
        {
            Type = InsightType.Risk,
            Priority = InsightPriority.Normal,
            Title = $"{staleCount} KR 3 haftadır güncellenmedi",
            Detail = "Hedefler gerçek durumu yansıtmıyor olabilir.",
            Module = "okr",
            NavigateUrl = "/okr",
            Icon = "bi-bullseye",
            IconColor = "text-secondary",
            NumericValue = staleCount
        });
    }
}
```

---

### 4.3 — BKM Özel: 3 Yeni Skill

**Dosya:** `yonetiq/Data/AiSkills/Definitions/BookSkills.cs` — **YENİ DOSYA**

```csharp
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

/// <summary>
/// BKM Kitap'a özgü AI skill tanımları.
/// Kitap sektörü domain zekasını barındırır.
/// </summary>
public static class BookSkills
{
    /// <summary>
    /// Stok kritik seviyeye yaklaşan ürünleri tespit edip önceliklendirir.
    /// </summary>
    public static SkillDefinition StockAlert => new()
    {
        Id = "book.stock_alert",
        Name = "Kritik Stok Uyarısı",
        Description = "Tükenmek üzere olan kitapları tespit eder, sipariş önceliği önerir",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Proactive,
        Module = "query",
        Icon = "bi-box-seam",
        RequiredContext = ["critical_stock_products", "sales_velocity"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.1f,
        SystemPromptFile = "book.stock_alert.system.md",
        UserPromptFile = "book.stock_alert.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Tespit edilen ürünlerin stok güncellenmesi için sipariş yapıldı",
        DependencyServices = ["QueryService"]
    };

    /// <summary>
    /// Son 30 günde normalin üstünde tren gösteren yazar/kategori/kitapları yakalar.
    /// </summary>
    public static SkillDefinition TrendRadar => new()
    {
        Id = "book.trend_radar",
        Name = "Trend Radar",
        Description = "Satış ivmesi kazanan yazar, kategori veya kitapları tespit eder",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-graph-up-arrow",
        RequiredContext = ["sales_trend_30d", "sales_trend_prev_30d"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "book.trend_radar.system.md",
        UserPromptFile = "book.trend_radar.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Tespit edilen trendler gerçek veri artışıyla doğrulandı",
        DependencyServices = ["QueryService"]
    };

    /// <summary>
    /// Kampanya dönemindeki "gerçek büyüme" ile "indirim etkisi"ni birbirinden ayırır.
    /// </summary>
    public static SkillDefinition CampaignRoi => new()
    {
        Id = "book.campaign_roi",
        Name = "Kampanya ROI Analizi",
        Description = "Kampanya cirosu ile organik büyümeyi ayırt eder, gerçek ROI hesaplar",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-tag-fill",
        RequiredContext = ["campaign_sales", "baseline_sales", "discount_amounts"],
        RequiresUserInput = false,
        OutputType = SkillOutputType.Text,
        Temperature = 0.15f,
        SystemPromptFile = "book.campaign_roi.system.md",
        UserPromptFile = "book.campaign_roi.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "ROI hesabı doğrulanabilir rakamlarla sunuldu",
        DependencyServices = ["QueryService"]
    };
}
```

**Program.cs'e ekle:**
```csharp
// Faz 5: BKM Kitap domain skill'leri
registry.Register(YonetIQ.Data.AiSkills.Definitions.BookSkills.StockAlert);
registry.Register(YonetIQ.Data.AiSkills.Definitions.BookSkills.TrendRadar);
registry.Register(YonetIQ.Data.AiSkills.Definitions.BookSkills.CampaignRoi);
```

---

### 4.4 — BKM Skill Prompt Dosyaları

**`yonetiq/Data/AiSkills/Prompts/book.stock_alert.system.md`**
```markdown
---version: 1---
Sen BKM Kitap'ın stok yönetim asistanısın.
Verilen kritik stok listesini analiz et.
Her ürün için:
- Tahmini tükenme gününü belirt
- Aciliyet sırasına koy (bugün, 3 gün, 7 gün içinde)
- Sipariş miktarı öner (30 günlük satış hızı × 1.5 güvenlik faktörü)
Maksimum 20 ürün göster. Tablo formatı kullan.
```

**`yonetiq/Data/AiSkills/Prompts/book.stock_alert.user.md`**
```markdown
---version: 1---
Kritik stok durumu:
{{critical_stock_products}}

Satış hızları (son 30 gün):
{{sales_velocity}}

Tükenmek üzere olan ürünleri öncelik sırasına göre listele.
Sipariş önerilerini ekle.
```

**`yonetiq/Data/AiSkills/Prompts/book.trend_radar.system.md`**
```markdown
---version: 1---
Sen BKM Kitap trend analisti senin.
İki dönem satış verisini karşılaştır.
Normalin (ortalama + 1.5 standart sapma) üstünde büyüyen yazar/kategori/ürünleri işaretle.
"Bu trendi ne tetikliyor?" sorusuna varsayımlarını yaz (sosyal medya, haber, sezon, vs).
Top 5 trend ve top 3 düşüş listesi çıkar.
Türkçe yaz.
```

**`yonetiq/Data/AiSkills/Prompts/book.campaign_roi.system.md`**
```markdown
---version: 1---
Sen kampanya ROI analisti senin. BKM Kitap kampanyası değerlendiriyorsun.
GÖREV:
1. Kampanya döneminin cirosunu ile geçen yılın aynı dönemini karşılaştır
2. İndirim tutarını toplam artıştan çıkar → organik büyüme = gerçek kazanç
3. Kampanya maliyeti (indirim toplamı + pazarlama) / organik büyüme = ROI
4. "Sahte büyüme mi yoksa gerçek büyüme mi?" sorusunu net cevapla

Her rakamı tabloda göster. Yorum kısmı maksimum 3 paragraf.
```

---

## BÖLÜM 5 — OKR İYİLEŞTİRMELERİ

### 5.1 — KeyResult → Task Bağlantısı UI

**Dosya:** `yonetiq/Components/Pages/Okr/OkrBoard.razor`

Her Key Result kartında, `LinkedTaskId` varsa ilgili görevi göster:

```razor
@if (kr.LinkedTaskId.HasValue)
{
    <a href="/gorev/duzenle/@kr.LinkedTaskId" class="text-muted small">
        <i class="bi bi-check2-square"></i> Bağlı görev
    </a>
}
else
{
    <button class="btn btn-sm yi-btn-ghost" @onclick="() => LinkTaskAsync(kr)">
        <i class="bi bi-link"></i> Göreve bağla
    </button>
}
```

---

### 5.2 — Decision Takip Durumu

**Dosya:** `yonetiq/Data/Models/Decision.cs` — yeni alanlar:

```csharp
// Mevcut alanlara ekle:
public string DecisionStatus { get; set; } = "Açık"; // Açık / Uygulandı / Beklemede / İptal
public DateTime? FollowUpDate { get; set; }
public int? AssignedToUserId { get; set; }
```

Migration:
```csharp
await EnsureColumnAsync(conn, "Decisions", "DecisionStatus", "NVARCHAR(50) NOT NULL DEFAULT 'Açık'");
await EnsureColumnAsync(conn, "Decisions", "FollowUpDate",   "DATETIME2 NULL");
await EnsureColumnAsync(conn, "Decisions", "AssignedToUserId", "INT NULL");
```

---

## BÖLÜM 6 — AI PROVIDER İYİLEŞTİRMELERİ

### 6.1 — Polly Retry + Circuit Breaker

`yonetiq.csproj`'a ekle:
```xml
<PackageReference Include="Polly" Version="8.5.1" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.1" />
```

**Dosya:** `yonetiq/Data/Services/AI/AiProviderService.cs`

```csharp
// Constructor'a ekle:
private static readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline =
    new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(15),
            OnOpened = args => { /* logger */ return ValueTask.CompletedTask; }
        })
        .AddTimeout(TimeSpan.FromSeconds(25))
        .Build();

// CallGeminiAsync içinde kullan:
var response = await _resiliencePipeline.ExecuteAsync(
    async ct => await http.SendAsync(req, ct), cancellationToken);
```

---

## BÖLÜM 7 — KAZAN-KAZAN: HIZLI EKLENTİLER

### 7.1 — Dashboard Kişiselleştirme Tablosu

Migration:
```sql
CREATE TABLE UserDashboardPreferences (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    QuerySetId INT NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsVisible BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UDP_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UDP_Sets  FOREIGN KEY (QuerySetId) REFERENCES QuerySets(Id),
    CONSTRAINT UQ_UDP UNIQUE (UserId, QuerySetId)
);
```

InfrastructureSeed'e ekle:
```csharp
await EnsureTableAsync(conn, "UserDashboardPreferences", @"
    CREATE TABLE UserDashboardPreferences (
        Id INT IDENTITY PRIMARY KEY,
        UserId INT NOT NULL,
        QuerySetId INT NOT NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsVisible BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    )");
```

---

### 7.2 — Time Tracking → Analytics Bağlantısı

**Dosya:** `yonetiq/Data/Services/AnalyticsService.cs`

```csharp
public async Task<ServiceResult<QueryResult>> GetTimeTrackingResultAsync()
{
    const string sql = @"
        SELECT 
            u.FullName AS [Kişi],
            SUM(te.DurationMinutes) / 60.0 AS [Toplam Saat],
            COUNT(DISTINCT te.TaskItemId) AS [Görev Sayısı],
            CAST(SUM(te.DurationMinutes) / 60.0 / NULLIF(COUNT(DISTINCT te.TaskItemId),0) AS DECIMAL(5,1)) AS [Görev Başı Saat]
        FROM TimeEntries te
        LEFT JOIN Users u ON u.Id = te.UserId
        WHERE te.StartTime >= DATEADD(month,-1,GETDATE())
        GROUP BY u.FullName
        ORDER BY [Toplam Saat] DESC";

    return await RunAndWrapAsync(sql, "Zaman Takibi Özeti");
}
```

---

## BÖLÜM 8 — BUILD UYARILARI ÇÖZÜMÜ

### 8.1 — Mevcut Build Uyarıları

```
Home.razor(176): <DateTime> unclosed tag → DateTime C# tipi Razor'da component olarak yorumlanıyor
MeetingDetail.razor(89): UserService.ListAsync yok
MeetingDetail.razor(97): MeetingService.ListDecisionsAsync yok
QueryService.cs(12): lookupSvc parametresi okunmuyor
DecisionForm.razor(70,75): null reference uyarıları
MeetingDetail.razor(77): _showEditMeeting atanıyor ama kullanılmıyor
```

**Home.razor(176):** `<DateTime>` yerine `@DateTime.Now.ToString(...)` kullan

**MeetingDetail.razor(89):** `UserService.ListAsync` → mevcut metot adı neyse onu kullan (`GetAllAsync` veya doğru imzayı bul)

**MeetingDetail.razor(97):** `MeetingService.ListDecisionsAsync` → `GetDecisionsAsync(meetingId)` veya mevcut doğru metot

**QueryService.cs(12):** `lookupSvc` parametresini ya kullan ya da constructor'dan kaldır

**DecisionForm.razor(70,75):** Null-forgiving operator ekle: `value!` veya null check

**MeetingDetail.razor(77):** `_showEditMeeting` ya kullan ya kaldır

---

## BÖLÜM 9 — UYGULAMA SIRASI

```
1. Git init + .gitignore + env var (Bölüm 0)
2. BaseEntity.UpdatedAt migration (1.1)
3. Session null safety (1.2)
4. Schema detection cache (1.3)
5. Notification memory leak (1.4)
6. Build uyarıları (Bölüm 8) → clean build
7. SuggestedAction rendering (2.1) — en yüksek ROI
8. ContinuousLearningService yeni sınıf (2.6)
9. AiFeedbackWidget güncelleme (2.7)
10. SemanticEnricher yeni sınıf (2.9)
11. SemanticDefinitions seed — BKM (4.1)
12. Toplantı hazırlık otomatı (2.2)
13. Görev sonrası AI (2.3)
14. Not AI analizi (2.4)
15. Anomali bildirim zinciri (2.5)
16. QueryEditor SQL tracking (2.8)
17. Admin pattern onay sayfası (2.10)
18. BookSkills yeni dosya (4.3 + 4.4)
19. ProactiveInsight BKM ekleri (4.2)
20. Polly retry (6.1)
21. OKR + Decision iyileştirmeleri (5.x)
22. Dashboard kişiselleştirme (7.1)
23. Analytics time tracking (7.2)
24. Sayfa yetki kontrolü (1.5)
25. PromptEngine hot-reload (1.6)
```

---

## BÖLÜM 10 — TEST KONTROL LİSTESİ

Her bölüm tamamlandığında çalıştır:

```bash
dotnet build D:/Dev/yonet/yonetiq.sln -nologo
dotnet test D:/Dev/yonet/YonetIQ.Tests/YonetIQ.Tests.csproj --verbosity minimal
```

Yeni unit testler eklenecek (`YonetIQ.Tests`):
- `ContinuousLearningServiceTests.cs` → `ComputeSimilarity` edge case'leri
- `SemanticEnricherTests.cs` → mock `SemanticService` ile term çözümleme
- `BookSkillsTests.cs` → skill definition validasyonu

---

## EK — PROMPT DOSYALARI KONTROL LİSTESİ

Mevcut olması gerekenler (eksikse oluştur):
```
Data/AiSkills/Prompts/
├── exec.daily_briefing.system.md    ✓ kontrol et
├── exec.daily_briefing.user.md      ✓ kontrol et
├── task.clarify.system.md           ✓ kontrol et
├── task.risk.system.md              ✓ kontrol et
├── task.decompose.system.md         ✓ kontrol et
├── meeting.preparation.system.md    ✓ kontrol et
├── meeting.summarize.system.md      ✓ kontrol et
├── query.nl_to_sql.system.md        ✓ kontrol et — {{semantic_terms}} eklenmeli
├── query.nl_to_sql.user.md          ✓ kontrol et — {{semantic_terms}} eklenmeli
├── task.assign.system.md            → YENİ (Bölüm 3.2)
├── task.assign.user.md              → YENİ (Bölüm 3.2)
├── meeting.quality.system.md        → YENİ (Bölüm 3.2)
├── meeting.quality.user.md          → YENİ (Bölüm 3.2)
├── book.stock_alert.system.md       → YENİ (Bölüm 4.4)
├── book.stock_alert.user.md         → YENİ (Bölüm 4.4)
├── book.trend_radar.system.md       → YENİ (Bölüm 4.4)
├── book.trend_radar.user.md         → YENİ (Bölüm 4.4)
├── book.campaign_roi.system.md      → YENİ (Bölüm 4.4)
└── book.campaign_roi.user.md        → YENİ (Bölüm 4.4)
```

`query.nl_to_sql.system.md`'ye eklenecek blok (en alta):
```markdown
{{semantic_terms}}

Yukarıdaki sözlükte tanımlı terimler için verilen SQL ifadelerini doğrudan kullan.
Tahmin yapma — sözlükte varsa sözlükteki tanımı kullan.
```

---

---

## BÖLÜM 11 — DB SKİLL SİSTEMİ + ÖĞRENME MİMARİSİ

> Detaylı implementasyon: `D:/Dev/yonet/YONETIQ_LEARNING_SYSTEM.md`

### Özet
Master plan (Bölüm 0-10) tamamlandıktan sonra uygulanacak. Skill'leri code-defined'dan DB'ye taşır, multi-sinyal öğrenme sistemi kurar.

### Neden
- Skill değiştirmek → deploy gerektirmesin
- Temperature, prompt → admin panelden ayarlanabilsin
- A/B test yapılabilsin
- Pattern'ler kullanıcı davranışından otomatik öğrensin

### Sinyal Sistemi
| Sinyal | Ağırlık |
|--------|---------|
| Thumbs up | +3.0 |
| Implicit accept | +1.0 |
| SQL accepted | +1.5 |
| Thumbs down | -5.0 |
| SQL correction | -2.0 |
| Decay (30 gün) | -0.3 |

**Eşikler:** ≥8.0 oto-onay, 4.0-7.9 admin, <4.0 gürültü

### Yeni Dosyalar (15 adım)
1. DB: AiSkillDefinitions, AiPatternSignals, SemanticLearningCandidates, AiQueryLog tabloları
2. Model: AiSkillRecord.cs
3. Servis: SkillPersistenceService, LearningSignalService, SemanticDiscoveryService
4. UI: /admin/skill-yonetim, /admin/semantik-ogrenme
5. Güncelleme: SkillRegistry.Disable, AiMemoryService, AiOrchestration, PromptEngine DB override

### Uygulama Sırası
Bölüm 0-10 → Bölüm 11 (bu bölüm) → 15 alt adım sırayla

---

*Bu belge Claude Code session'larında referans alınmak üzere hazırlanmıştır.*
*Agent: `learning-architect.md` — bu bölümün uzman agent'ı*
