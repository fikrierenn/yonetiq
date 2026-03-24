# YonetIQ — Claude Çalışma Rehberi

## Session Başlangıç Protokolü
> **İLK İŞ:** `C:/Users/fikri.eren/.claude/projects/D--Dev-yonet/memory/MAIN_INDEX.md` oku.
> INDEX'ten sadece görevle ilgili domain dosyalarını oku — hepsini değil (token tasarrufu).
> **UNIFIED PLAN:** `D:/Dev/yonet/YONETIQ_UNIFIED_MASTER.md` — 49 adım, 7 faz. Faz 1 adım 1 tamamlandı, adım 2'den devam.
> **SKILL:** `.claude/skills/yonetiq-platform/SKILL.md` — proje bilgi tabanı.

## Memory Sistemi (Hiyerarşik Context Architecture)
```
MAIN_INDEX.md          ← Merkezi referans, her session buradan başlar
├── coding_rules.md    ← Kritik kurallar, gotcha'lar (her kod yazımında)
├── ai_skill_system.md ← 33 skill, AI servisleri, prompt dosyaları
├── architecture.md    ← Stack, DI, fallback, CSS sistemi
├── services.md        ← Servis metot imzaları
├── ui_patterns.md     ← CSS değişkenleri, dark mode, bileşenler
├── completed_features.md ← Tekrar yapmamak için
├── decisions.md       ← Mimari kararlar ve gerekçeleri
├── todo_list.md       ← Açık/kapalı TODO'lar
└── strategy_ai_first_transformation.md ← AI roadmap (büyük ölçüde uygulandı)
```
**Konum:** `C:/Users/fikri.eren/.claude/projects/D--Dev-yonet/memory/`

## Sub-Agent'lar (Master Plan)
```
.claude/agents/
├── debt-fixer.md         ← Bölüm 1: Teknik borç düzeltme (6 madde)
├── ai-gap-closer.md      ← Bölüm 2: AI boşluk kapatma (10 madde)
├── domain-expert.md      ← Bölüm 4: BKM Kitap domain zekası
├── resilience-engineer.md ← Bölüm 6: Polly retry + circuit breaker
├── ui-enhancer.md        ← Bölüm 7: Dashboard + UI iyileştirme
├── build-doctor.md       ← Bölüm 8: Build uyarı düzeltme
├── test-writer.md        ← Bölüm 10: Test coverage artırma
├── analyzer.md           ← Genel kod analizi
├── executor.md           ← Genel implementasyon
├── validator.md          ← Build, test, doğrulama
└── ai-skill-developer.md ← Yeni AI skill geliştirme
```

## Build & Run
```bash
dotnet build D:/Dev/yonet/yonetiq.sln -nologo
dotnet run --project D:/Dev/yonet/yonetiq --urls http://127.0.0.1:7116
dotnet test D:/Dev/yonet/YonetIQ.Tests/YonetIQ.Tests.csproj  # 54 test
RUN_AI_QA=1 dotnet run --project D:/Dev/yonet/yonetiq --urls http://127.0.0.1:7116  # QA
```

## Mimari Özet
- .NET 10 Blazor Server, API katmanı YOK
- Dapper + MSSQL (runtime), EF Core (sadece migration)
- UI: Tabler/TabBlazor + Bootstrap Icons (lokal)
- AI: Multi-provider (Gemini birincil + OpenAI/Anthropic fallback), 33 skill
- DB bağlantı: `.env` → `YONET_CONN` veya `SQLCLI_CONN`

## Kritik Kurallar (Kısa)
- Servisler `BaseService` extend → `ExecuteServiceAsync` + `ServiceResult.Failure()` (exception atma)
- Dapper: `SELECT *` yasak, kolonlar explicit
- Schema: `InfrastructureSeed.cs` (IF COL_LENGTH guard) + `SeedData.cs`
- SP'ler: `Data/StoredProcedures/*.sql` → `StoredProcedureLoader` runtime'da yükler
- Lookup: hardcoded ID yasak → `LookupService.GetLookupIdAsync()`
- `User.RoleName`: `[NotMapped]`, `QueryRecord.ResultType`/`QuerySet.Type`: plain string (enum yok)
- CSS: `var(--yi-*)` kullan, `html[data-theme='dark']` (özgüllük 0,1,1)
- Razor: loop değişkeninde `section` adını kullanma
- AI prompt: `SanitizePrompt()` injection koruması, 32K char budget

> **Detaylı kurallar:** `memory/coding_rules.md`

## Dosya Yapısı
```
yonetiq/
├── Components/Layout/          # MainLayout, NavMenu, LoginLayout
├── Components/Pages/           # 18 modül, her biri kendi klasöründe
├── Components/Shared/AI/       # CommandBar, AiSidebar, DashboardBriefing, InlineSuggestion + 4 widget
├── Components/Shared/          # GlobalSearch, ThemeSwitcher, DynamicTable, ApexRenderer
├── Data/Models/AI/             # SkillDefinition, AiRequest/Response, AiInteraction, AiFeedback
├── Data/Services/AI/           # Orchestration, SkillRegistry, ContextBuilder, SkillExecutor, AiProvider
├── Data/AiSkills/Prompts/      # 66 prompt template (versiyonlanmış, system+user çiftleri)
├── Data/AiSkills/Definitions/  # Skill tanımları (7 sınıf, 33 skill)
├── Data/Infrastructure/        # ServiceResult<T>, BaseService, StoredProcedureLoader, StatisticalAnalyzer, AiProviderConfig
├── Data/StoredProcedures/      # 6 SP referans kopyası (.sql)
└── wwwroot/css/site.css        # ~4275 satır, yi-* prefix
```

## AI Sistemi Özet
- **33 skill** aktif (Task 6, Meeting 5, Note 4, Query 6, Executive 6, Approval 5, OKR 2)
- **66 prompt** dosyası (her skill × system + user)
- **15 sayfa** AiSidebar entegrasyonu
- **AiProviderService**: Gemini (birincil) + OpenAI + Anthropic fallback
- **StatisticalAnalyzer**: Deterministik IQR outlier + karşılaştırmalı delta rapor
- **Güvenlik**: prompt sanitization, 32K char budget, userId auth check
- **54 xUnit test** (unit + integration)

## Tamamlanan AI Fazları
- **Faz 1:** 6 core skill, orchestration, CommandBar, AiSidebar, DashboardBriefing
- **Faz 2:** 15 skill, prompt versiyonlama, semantic layer, AI Dashboard, 9 sayfa AI
- **Faz 3:** ProactiveInsight, Golden Memory, NL→SQL Semantic, Permission-aware, Confidence
- **Faz 4 (24.03):** 18 yeni skill (toplam 33), AiInsightService kaldırma, xUnit 54 test, SP ayırma, multi-provider, StatisticalAnalyzer, 15 sayfa AI sidebar, güvenlik katmanı
