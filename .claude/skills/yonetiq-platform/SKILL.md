---
name: yonetiq-platform
description: >
  YonetIQ yönetim platformuna özgü bilgi tabanı — mimari kararlar, mevcut özellikler,
  kod yapısı, AI skill sistemi, teknoloji stack'i ve geliştirme kuralları. YonetIQ ile
  ilgili her türlü kod yazımı, özellik önerisi, mimari karar, AI skill tasarımı, Blazor
  component geliştirme, servis ekleme veya roadmap tartışmasında bu skill'i devreye al.
  Proje hakkında kod, strateji veya ürün kararı gerektiğinde önce bu skill'i oku.
---

# YonetIQ Platform Skill

## Proje Kimliği
**YonetIQ** — Blazor Server tabanlı AI-first kurumsal yönetim platformu.
- Konum: `D:\Dev\yonet\` (root), `D:\Dev\yonet\yonetiq\` (uygulama)
- Vizyon: "bazı AI özellikleri olan portal" → "AI-first yönetim çalışma alanı"
- Sahip/Geliştirici: Fikri (tek geliştirici)

---

## Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| Framework | .NET 10 Blazor Server |
| ORM (runtime) | Dapper + `Microsoft.Data.SqlClient` |
| ORM (migration) | EF Core 10 (`AppDbContext`) |
| Veritabanı | SQL Server (multi-DataSource) |
| UI | Tabler/TabBlazor + Bootstrap Icons (lokal) |
| Chart | Blazor-ApexCharts |
| AI Provider | Gemini (birincil) → OpenAI → Anthropic (fallback) |
| Email | MailKit |
| Export | EPPlus (Excel) |
| Test | xUnit (54 test) |

---

## Klasör Yapısı

```
yonetiq/
├── Components/
│   ├── Layout/           MainLayout, NavMenu, LoginLayout
│   ├── Pages/            18 modül (her biri kendi klasöründe)
│   │   ├── Admin/        AiDashboard, AuditLog, KpiTargets, Settings
│   │   ├── Approval/     ApprovalCenter
│   │   ├── Auth/         Login, ForgotPassword, ResetPassword
│   │   ├── Calendar/     CalendarPanel
│   │   ├── Dashboard/    Home, Analytics
│   │   ├── DataSource/   DataSourceForm, DataSourceList
│   │   ├── Meeting/      MeetingList, MeetingForm, MeetingDetail, DecisionList, DecisionForm
│   │   ├── Notes/        Notes
│   │   ├── Okr/          OkrBoard
│   │   ├── Profile/      Profile
│   │   ├── Query/        QueryEditor, QueryList, QueryAiAssistant, QueryPreviewCard, QuerySchemaPanel
│   │   ├── Report/       ReportList, ReportRun, ScheduledReportList
│   │   ├── Set/          SetForm, SetList, SetView
│   │   └── Task/         TaskList, TaskForm, TaskKanban, TaskTemplateList, TimeReport
│   └── Shared/
│       └── AI/           CommandBar, AiSidebar, DashboardBriefing, InlineSuggestion,
│                         AiFeedbackWidget, ConfidenceBadge, InsightCards, SkillResultCard
├── Data/
│   ├── Models/           BaseEntity, User, TaskItem, Meeting, Decision, ...
│   ├── Models/AI/        SkillDefinition, AiRequest, AiResponse, AiInteraction, ...
│   ├── Services/         30+ servis (BaseService'ten türer)
│   ├── Services/AI/      AiOrchestrationService, SkillExecutor, SkillRegistry,
│   │                     PromptEngine, ContextBuilderService, AiMemoryService,
│   │                     AiProviderService, ProactiveInsightService, AiEvaluationService,
│   │                     AiQualityTestService, SemanticService
│   ├── AiSkills/
│   │   ├── Definitions/  ExecutiveSkills, TaskSkills, MeetingSkills, NoteSkills,
│   │   │                 ReportSkills, ApprovalSkills, OkrSkills
│   │   └── Prompts/      *.system.md, *.user.md (disk'ten okunur)
│   ├── Infrastructure/   AesEncryption, ServiceResult, StatisticalAnalyzer,
│   │                     StoredProcedureLoader, DapperTypeHandlers, LookupConstants,
│   │                     AiProviderConfig
│   └── StoredProcedures/ *.sql (runtime'da StoredProcedureLoader yükler)
└── Program.cs
```

---

## AI Skill Sistemi (33 Skill)

### Mevcut Skill Grupları

| Grup | Skill'ler |
|------|-----------|
| **Executive** | DailyBriefing, ChangeDetection, Focus, KpiAnalysis, WeeklyDigest, RiskScan |
| **Task** | Clarify, Risk, Decompose, Bottleneck, Followup, Prioritize |
| **Meeting** | Summarize, ExtractActions, ExtractDecisions, Preparation, Unresolved |
| **Note** | Structure, SuggestTags, ExtractActions, ToAgenda |
| **Report** | Explain, NlToSql, ExecSummary, Anomaly, NextQuestion, Trends |
| **Approval** | Summarize, Risk, Bottleneck, Anomaly |
| **OKR** | Progress, Action |

### Skill Akışı

```
AiRequest → AiOrchestrationService
  → SkillRegistry.Resolve()
  → ContextBuilderService.BuildContextAsync()
  → AiMemoryService.GetRecentInteractionsAsync()     (few-shot context)
  → AiMemoryService.GetApprovedPatternsAsync()       (golden memory)
  → SkillExecutor.ExecuteAsync()
      → PromptEngine.LoadSystemPromptAsync()          (_system_rules.md auto-prepend)
      → PromptEngine.RenderTemplate()
      → FlattenEntity()                               (TaskItem→taskTitle vb.)
      → SanitizePrompt()                              (injection guard)
      → AiProviderService.GenerateAsync()             (Gemini → OpenAI → Anthropic)
  → AiMemoryService.SaveInteractionAsync()
  → AiResponse
```

### SkillDefinition Alanları (kritikler)

- `Id`: "module.action" formatı (örn: "task.clarify")
- `Module`: hangi sayfada göründüğü
- `TriggerMode`: Reactive / Proactive
- `SystemPromptFile`, `UserPromptFile`: Prompts/ klasöründe
- `RequiredEntities`, `RequiredContext`: ContextBuilder neyi çekecek bilir
- `Temperature`: 0.2f (analiz), 0.4f (yaratıcı/öneri)
- `MaxTokenInput`, `MaxOutputLength`: guardrail sınırları
- `HallucinationRisk`: Low/Medium/High

---

## Kritik Kodlama Kuralları

### Servis Katmanı

```csharp
// HER servis BaseService'ten türer
public class MyService(IConfiguration config, AuditService auditSvc)
    : BaseService(config, auditSvc)

// HER metot ServiceResult döner — exception ATMA
return ServiceResult<T>.Failure("hata mesajı", "HATA_KODU");
return ServiceResult<T>.Success(data);

// DB işlemi için ExecuteServiceAsync kullan
return await ExecuteServiceAsync<T>(async conn => { ... });
```

### Dapper Kuralları

- `SELECT *` yasak — kolonları explicit yaz
- Parametreli sorgu zorunlu (`@Param`)
- SP kullan, fallback SQL'e `IsCompatibleWithFallback()` ile düş

### Schema / Migration

- Yeni kolon: `InfrastructureSeed.cs`'de `IF COL_LENGTH('Table','Col') IS NULL` guard ile
- Seed data: `SeedData.cs`
- SP'ler: `Data/StoredProcedures/*.sql` — `StoredProcedureLoader` runtime yükler

### Lookup Kuralları

- Hardcoded Lookup ID yasak
- `LookupService.GetLookupIdAsync("Group", "Value")` kullan
- `LookupConstants` sınıfında sabit değerler tutulur

### UI/CSS

- CSS değişkenler: `var(--yi-*)` kullan
- Dark mode: `html[data-theme='dark']` (özgüllük 0,1,1)
- Razor loop'ta `section` değişken adı olarak kullanma
- AI butonları: `yi-btn-ai` class'ı

### Model Kuralları

- `User.RoleName`: `[NotMapped]` — EF görmez, Dapper JOIN doldurur
- `QueryRecord.ResultType` / `QuerySet.Type`: plain string (enum yok)
- `BaseEntity`: `int Id` + `DateTime CreatedAt` (UTC)

### Güvenlik

- DataSource bağlantıları: `AesEncryption.Encrypt/Decrypt` (AES-256-CBC)
- Şifre: `HMACSHA256` + random salt (32 byte)
- AI prompt: `SanitizePrompt()` injection koruması + 32K char budget + userId auth check
- `DataSourceEncryptionKey`: appsettings veya env var

### Tarih Formatı

- SQL literal'ları: DMY (dd.MM.yyyy) — ISO (yyyy-MM-dd) KULLANMA

---

## Mevcut Modüller & Durum

| Modül | Sayfa | AI Skill'leri |
|-------|-------|---------------|
| Dashboard | Home, Analytics | DailyBriefing, Focus, KpiAnalysis, WeeklyDigest, RiskScan |
| Görevler | TaskList, TaskForm, TaskKanban, TaskTemplateList, TimeReport | Clarify, Risk, Decompose, Bottleneck, Followup, Prioritize |
| Toplantılar | MeetingList, MeetingForm, MeetingDetail, DecisionList/Form | Summarize, ExtractActions, ExtractDecisions, Preparation, Unresolved |
| Notlar | Notes | Structure, SuggestTags, ExtractActions, ToAgenda |
| Raporlar | ReportList, ReportRun, ScheduledReportList | Explain, NlToSql, ExecSummary, Anomaly, NextQuestion, Trends |
| Sorgular | QueryEditor, QueryList, QueryAiAssistant | NlToSql (paylaşılan) |
| Onaylar | ApprovalCenter | Summarize, Risk, Bottleneck, Anomaly |
| OKR | OkrBoard | Progress, Action |
| Takvim | CalendarPanel | — |
| DataSource | DataSourceList, DataSourceForm | — |
| Mesajlar | Messages | — |
| Profil | Profile | — |

---

## Tamamlanan Fazlar

- **Faz 1**: DB schema modernizasyonu, Auth (HMACSHA256), temel servisler
- **Faz 2**: AI entegrasyonu (Gemini), QueryService (DDL/DML güvenlik), çoklu DataSource, ReportingService
- **Faz 3 (kısmi)**: MessageService, AttachmentService, Email/Telegram, iç bildirimler
- **Faz 4 (kısmi)**: AI skill sistemi genişletme — 33 skill, ContextBuilder, SkillExecutor, AiMemory, ProactiveInsight, AiEvaluation, AiQualityTest
- **Faz 4+ (24.03.2026)**: Multi-provider (Gemini+OpenAI+Anthropic), StatisticalAnalyzer, 15 sayfa AiSidebar, FlattenEntity, output format zorlaması, _system_rules.md, 54 xUnit test

---

## Bilinen Borçlar & Eksikler

| Alan | Sorun |
|------|-------|
| Git | Tanımsız — kritik risk |
| DataSourceEncryptionKey | appsettings'de yok, runtime üretiliyor |
| SessionService null safety | ContextBuilder'da ActiveUser null guard yok |
| BaseEntity.UpdatedAt | Eksik — audit trail için gerekli |
| NotificationService | Static event → memory leak riski |
| ContextBuilderService | 12 bağımlılık → test edilebilirlik düşük |
| Schema detection | HasAllowDmlColumn her sorguda çalışıyor, cache yok |
| PromptEngine | Cache invalidation yok, hot-reload yok |
| Sayfa yetki kontrolü | @attribute [Authorize] eksik |
| SignalR | Gerçek zamanlı bildirim yok |

---

## Yeni Skill Ekleme Şablonu

```csharp
// Data/AiSkills/Definitions/XxxSkills.cs
public static SkillDefinition YeniSkill => new()
{
    Id = "modul.aksiyon",
    Name = "Türkçe Ad",
    Description = "Ne yapar, kısaca",
    Category = SkillCategory.Xxx,
    TriggerMode = TriggerMode.Reactive,
    Module = "modul",
    Icon = "bi-icon-adi",
    RequiredEntities = ["EntityAdı"],
    RequiredContext = ["context_key"],
    OutputType = SkillOutputType.Text,
    Temperature = 0.2f,
    SystemPromptFile = "modul.aksiyon.system.md",
    UserPromptFile = "modul.aksiyon.user.md",
    MaxOutputLength = 1500,
    HallucinationRisk = RiskLevel.Medium,
    SuccessCriteria = "Başarı kriteri",
    DependencyServices = ["XxxService"]
};

// Program.cs'e ekle:
registry.Register(YonetIQ.Data.AiSkills.Definitions.XxxSkills.YeniSkill);
```

Prompt dosyaları: `Data/AiSkills/Prompts/modul.aksiyon.system.md` ve `.user.md`

_system_rules.md evrensel kuralları otomatik prepend eder (PromptEngine.LoadSystemPromptAsync).

---

## AI Geliştirme Stratejisi

**Temel pozisyon:** YonetIQ "bazı AI özellikleri olan portal" değil, "AI-first yönetim çalışma alanı" olmalı.

**Uygulanan:** 33 skill, multi-provider, golden memory (AiPatterns), ProactiveInsight (deterministik), confidence scoring (5 faktör + yapısal format penalty), AiEvaluation dashboard, AiQualityTestService, prompt sanitization, evrensel çıktı kuralları (_system_rules.md)

**Hedef:** Bağlam farkında, tahmin eden, önceliklendiren, operasyonel değer üreten AI layer — gimmick değil, karar desteği.

**Çıktı Kalite Kuralları (tüm skill'ler için):**
- Salt metin YASAK → yapısal format zorunlu (Özet→Çıktı→Aksiyon→Risk)
- Max 150 kelime — kullanıcı okumaz
- Tek tık aksiyona dönüşebilir öneriler
- Somut örnek ver, kavramsal açıklama YASAK
- Gerçek veride kaos varsay
- Her öneride ölçülebilir başarı kriteri
- Gereksiz karmaşıklık önerme
