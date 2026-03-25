# YonetIQ — Geliştirici Rehberi

**Sürüm:** 2026 | **Dil:** Türkçe | **Hedef Kitle:** Backend / Frontend Geliştiriciler

---

## İçindekiler

1. [Mimari Özet](#1-mimari-özet)
2. [Proje Yapısı](#2-proje-yapısı)
3. [Build ve Çalıştırma](#3-build-ve-çalıştırma)
4. [Servis Katmanı](#4-servis-katmanı)
5. [Veri Erişimi (Dapper)](#5-veri-erişimi-dapper)
6. [Model Kuralları](#6-model-kuralları)
7. [Migrations ve Schema Yönetimi](#7-migrations-ve-schema-yönetimi)
8. [CSS Sistemi](#8-css-sistemi)
9. [Razor Bileşen Kuralları](#9-razor-bileşen-kuralları)
10. [Yeni Özellik Ekleme Rehberi](#10-yeni-özellik-ekleme-rehberi)
11. [AI Entegrasyonu](#11-ai-entegrasyonu)
12. [Güvenlik](#12-güvenlik)
13. [Yaygın Hatalar ve Çözümleri](#13-yaygın-hatalar-ve-çözümleri)

---

## 1. Mimari Özet

YonetIQ, **API katmanı olmayan** bir Blazor Server uygulamasıdır. Servisler doğrudan Dapper ile veritabanına ulaşır; HTTP istek/yanıt döngüsü yoktur.

```
Tarayıcı (SignalR WebSocket)
    └── Blazor Server (Interactive)
            └── Scoped Servisler (DI)
                    └── Dapper → SQL Server
```

### Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Framework | .NET 10, Blazor Server (Interactive) |
| UI Kütüphanesi | Tabler / TabBlazor, Bootstrap 5 |
| İkon Seti | Bootstrap Icons (lokal asset) |
| Bildirim | Blazored.Toast |
| Veri Erişimi (Runtime) | Dapper + Microsoft.Data.SqlClient |
| Veri Erişimi (Schema) | EF Core (sadece migration) |
| Veritabanı | SQL Server 2022+ |
| AI | Google Gemini API (gemini-1.5-flash) |
| E-posta | MailKit (SMTP) |
| Mesajlaşma | Telegram Bot API |
| Şifreleme | HMACSHA256 (şifre), AES-256-CBC (bağlantı stringleri) |
| Yapılandırma | `.env` dosyası + `appsettings.json` |

---

## 2. Proje Yapısı

```
D:/Dev/yonet/
├── yonetiq.sln
└── yonetiq/
    ├── Program.cs                    # DI kaydı, middleware, SeedData çağrısı
    ├── YonetIQ.csproj
    ├── appsettings.json              # Temel yapılandırma
    ├── Components/
    │   ├── App.razor
    │   ├── Routes.razor
    │   ├── Layout/
    │   │   ├── MainLayout.razor      # Sidebar + Topbar + İçerik
    │   │   └── LoginLayout.razor     # Giriş sayfası layout'u
    │   ├── Pages/
    │   │   ├── Auth/                 # Login.razor, ForgotPassword.razor
    │   │   ├── Dashboard/            # Home.razor, alt bileşenler
    │   │   ├── Task/                 # TaskList.razor, TaskDetail.razor, Kanban
    │   │   ├── Meeting/              # MeetingList.razor, MeetingDetail.razor
    │   │   ├── Report/               # Rapor listeleme ve çalıştırma
    │   │   ├── Query/                # Sorgu editörü
    │   │   ├── Set/                  # Dashboard set yönetimi
    │   │   ├── Communication/        # Mesajlaşma
    │   │   ├── Notes/                # Kişisel notlar
    │   │   ├── Calendar/             # Takvim
    │   │   ├── User/                 # Kullanıcı yönetimi
    │   │   ├── Admin/                # Ayarlar, Audit log
    │   │   ├── Profile/              # Profil.razor
    │   │   ├── Help/                 # Kullanıcı kitapçığı
    │   │   └── DataSource/           # Veri kaynağı yönetimi
    │   └── Shared/
    │       ├── DynamicTable.razor    # Evrensel tablo bileşeni
    │       ├── ApexRenderer.razor    # ApexCharts grafik bileşeni
    │       ├── AttachmentManager.razor
    │       └── ThemeSwitcher.razor   # Dark/Light tema
    ├── Data/
    │   ├── AppDbContext.cs           # EF Core context (sadece migration)
    │   ├── SeedData.cs               # Seed orchestrator
    │   ├── Infrastructure/
    │   │   ├── ServiceResult.cs      # ServiceResult<T> ve ServiceResult
    │   │   ├── AesEncryption.cs      # AES-256-CBC şifreleme
    │   │   ├── ConnectionFactory.cs
    │   │   └── LookupConstants.cs
    │   ├── Models/                   # POCO entity sınıfları
    │   ├── Services/
    │   │   ├── BaseService.cs        # Tüm servislerin atası
    │   │   └── [ModulAdı]Service.cs
    │   ├── ViewModels/               # DTO sınıfları
    │   └── Migrations/               # EF Core migration dosyaları
    └── wwwroot/
        ├── css/
        │   └── site.css              # Tüm custom CSS
        ├── js/
        ├── brand/
        │   └── yonetiq-mark.svg      # Logo (kilitli, değiştirilmez)
        └── fonts/                    # Bootstrap Icons (lokal)
```

---

## 3. Build ve Çalıştırma

### Gereksinimler

- .NET 10 SDK
- SQL Server 2022+ (yerel veya uzak)
- Proje kökünde `.env` dosyası

### .env Dosyası

```env
YONET_CONN=Server=localhost;Database=YonetIQ;User Id=sa;Password=SifresinizBurada;TrustServerCertificate=True;
GEMINI_API_KEY=AIza...
```

### Build

```bash
dotnet build D:/Dev/yonet/yonetiq.sln -nologo
```

### Çalıştırma

```bash
dotnet run --project D:/Dev/yonet/yonetiq --urls http://127.0.0.1:7116
```

Uygulama ilk başladığında:
1. Bekleyen EF migration'lar otomatik uygulanır.
2. `SeedData.SeedAsync()` çalışarak tabloları, SP'leri ve başlangıç verilerini oluşturur (idempotent).

### Yayın (Publish)

```bash
dotnet publish D:/Dev/yonet/yonetiq -c Release -o D:/publish/yonetiq
```

---

## 4. Servis Katmanı

### BaseService

Tüm servisler `BaseService` sınıfından türetilir:

```csharp
public abstract class BaseService(IConfiguration config, AuditService? auditService = null)
```

`BaseService` şunları sağlar:

| Metot | Açıklama |
|---|---|
| `ExecuteServiceAsync<T>(Func<SqlConnection, Task<T>>)` | Değer döndüren DB işlemi |
| `ExecuteServiceAsync(Func<SqlConnection, Task>)` | Değer döndürmeyen DB işlemi |
| `LogAction(module, action, detail)` | Audit log kaydı |
| `IsCompatibleWithFallback(SqlException)` | SP yokluğu için fallback kontrolü (2812, 208, 201, 8144, 207) |
| `CreateConn()` | Yeni SqlConnection oluşturur |

### ServiceResult Pattern

İş hataları **exception fırlatmaz**, `ServiceResult` ile döner:

```csharp
// Başarılı sonuç
return ServiceResult<User>.Success(user, "Giriş başarılı.");

// Hata sonucu
return ServiceResult<User>.Failure("E-posta veya şifre hatalı.", "InvalidCredentials");
```

`ServiceResult<T>` özellikleri:
- `IsSuccess` (bool)
- `Data` (T?)
- `Message` (string)
- `ErrorCode` (string?)

### Örnek Servis Metodu

```csharp
public class TaskService(IConfiguration config, AuditService auditService)
    : BaseService(config, auditService)
{
    public async Task<ServiceResult<List<TaskItem>>> GetAllAsync()
    {
        return await ExecuteServiceAsync<List<TaskItem>>(async conn =>
        {
            var items = await conn.QueryAsync<TaskItem>(
                "SELECT Id, Title, StatusLookupId, AssignedUserId, DueDate, CreatedAt FROM Tasks");
            return items.ToList();
        });
    }

    public async Task<ServiceResult> CreateAsync(TaskItem task)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "INSERT INTO Tasks (Title, StatusLookupId, AssignedUserId, DueDate) VALUES (@Title, @StatusLookupId, @AssignedUserId, @DueDate)",
                task);
            LogAction("Task", "Create", new { task.Title });
        });
    }
}
```

### Razor Sayfasında Servis Kullanımı

```csharp
@inject TaskService TaskSvc

var result = await TaskSvc.GetAllAsync();
if (result.IsSuccess)
{
    tasks = result.Data!;
}
else
{
    // Hata mesajını kullanıcıya göster
    errorMessage = result.Message;
}
```

### Bağımlılık Kaydı

`Program.cs` içinde tüm servisler `Scoped` olarak kaydedilir:

```csharp
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<MeetingService>();
// ...
```

`Scoped`: Her Blazor SignalR devresi (circuit) için ayrı örnek oluşturulur.

---

## 5. Veri Erişimi (Dapper)

### Temel Kurallar

- `SELECT *` **kullanmayın** — kolonları her zaman explicit listeleyin.
- Parametreler anonim nesne veya model ile geçilir (SQL injection koruması).
- SP öncelidir; yoksa inline SQL fallback devreye girer.

### SP/Fallback Pattern

```csharp
try
{
    // Stored Procedure denemesi
    var result = await conn.QueryAsync<TaskItem>(
        "sp_Task_List", commandType: CommandType.StoredProcedure);
    return result.ToList();
}
catch (SqlException ex) when (IsCompatibleWithFallback(ex))
{
    // SP yoksa inline SQL ile devam et
    var result = await conn.QueryAsync<TaskItem>(
        "SELECT Id, Title, StatusLookupId, DueDate, CreatedAt FROM Tasks WHERE IsDeleted = 0");
    return result.ToList();
}
```

`IsCompatibleWithFallback` hata kodları:
- `2812` — SP bulunamadı
- `208` — Tablo/nesne bulunamadı
- `201` — Parametre eksik
- `8144` — Fazla parametre verildi
- `207` — Geçersiz kolon adı

### Lookup ID'leri

Hardcoded ID kullanmayın. Lookups tablosundan dinamik alın:

```csharp
var statusId = await _lookupService.GetLookupIdAsync("Status", "Bekliyor");
```

---

## 6. Model Kuralları

### BaseEntity

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### [NotMapped] Alanlar

EF migration'ın görmemesi gereken alanlar `[NotMapped]` ile işaretlenir; bu alanlar Dapper JOIN ile doldurulur:

```csharp
[NotMapped]
public string? RoleName { get; set; }   // Lookups.Value JOIN'den gelir
```

### QueryRecord.ResultType ve QuerySet.Type

Bu alanlar **plain string**'dir — enum yoktur:

```csharp
// YANLIS:
query.ResultType = ResultType.BarChart.ToString();  // enum YOK

// DOGRU:
query.ResultType = "BarChart";  // Lookups tablosundan gelen string
```

Geçerli değerler Lookups tablosundan yüklenir (Group = "ResultType").

### User.RoleName

`[NotMapped]` — EF görmez, migration etkilemez. Login sorgusu JOIN ile doldurur:

```sql
SELECT u.*, l.Value AS RoleName
FROM Users u
LEFT JOIN Lookups l ON l.Id = u.RoleLookupId AND l.[Group] = 'Role'
```

---

## 7. Migrations ve Schema Yönetimi

### Ne Zaman EF Migration?

Sadece `AppDbContext`'teki **model** değişikliklerinde:

```bash
cd D:/Dev/yonet/yonetiq
dotnet ef migrations add YeniMigrasyon
dotnet ef database update
```

### Yeni Veritabanı Kolonu Ekleme

EF migration **gerekmez**. Bunun yerine:

1. `InfrastructureSeed.cs` içinde guard pattern ile sütun ekle:

```csharp
// IF COL_LENGTH guard — kolonu tekrar eklemeye çalışmaz
await conn.ExecuteAsync(@"
    IF COL_LENGTH('Tasks', 'NewColumn') IS NULL
        ALTER TABLE Tasks ADD NewColumn NVARCHAR(200) NULL;
");
```

2. `SeedData.cs` içinde `InfrastructureSeed`'i çağırdığından emin ol.

### Yeni Tablo Oluşturma

```csharp
await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NewTable')
    BEGIN
        CREATE TABLE NewTable (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Name NVARCHAR(200) NOT NULL,
            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
    END
");
```

---

## 8. CSS Sistemi

### Prefix Kuralı

Tüm custom CSS sınıfları `yi-` prefix'i ile başlar:

```css
.yi-card { ... }
.yi-input { ... }
.yi-sidebar-nav { ... }
```

Tabler ve Bootstrap sınıfları da kullanılabilir, ama özelleştirme `yi-*` ile yapılmalıdır.

### CSS Değişkenleri

Renk ve boyut değerlerini her zaman CSS variable ile yazın — hardcoded hex/rgb kullanmayın:

```css
/* DOGRU */
.yi-card {
    background: var(--yi-card-bg);
    border: 1px solid var(--yi-card-border);
    color: var(--yi-text-main);
}

/* YANLIS */
.yi-card {
    background: #ffffff;    /* Tema değişkenini ezer */
    color: #333333;
}
```

Mevcut değişkenler (`wwwroot/css/site.css`):

```css
--yi-bg-main        /* Ana sayfa arka planı */
--yi-bg-soft        /* Hafif arka plan tonu */
--yi-card-bg        /* Kart arka planı */
--yi-card-border    /* Kart kenarlığı */
--yi-text-main      /* Ana metin rengi */
--yi-text-muted     /* İkincil metin rengi */
--yi-text-link      /* Bağlantı rengi */
--yi-input-bg       /* Input arka planı */
--yi-input-border   /* Input kenarlığı */
--yi-sidebar-bg     /* Sidebar arka planı */
--yi-topbar-bg      /* Üst bar arka planı */
```

### Dark Mode

Dark mode override'ları için `html[data-theme='dark']` selector'ü kullanın (özgüllük 0,1,1):

```css
/* DOGRU — :root'u override eder */
html[data-theme='dark'] .yi-card {
    background: var(--yi-card-bg);
}

html[data-theme='dark'] {
    --yi-card-bg: #1e2130;
    --yi-text-main: #e0e0e0;
}

/* YANLIS — yeterli özgüllük yok */
[data-theme='dark'] .yi-card { ... }
```

### CSS Dosyası

Tüm custom CSS tek dosyadadır: `wwwroot/css/site.css`

Yeni stiller eklerken:
- Mevcut `yi-*` bloklarını bulmak için dosyada arama yapın.
- Aynı sınıfın birden fazla yerde tanımlandığı durumda, sonraki blok öncekini ezer — renk değerleri hardcoded yazılmışsa temayı bozabilir.

---

## 9. Razor Bileşen Kuralları

### @foreach Loop Değişken Adı

`section` adını **kullanmayın** — Blazor bu kelimeyi direktif olarak parse eder:

```razor
@* YANLIS — @section.Title Blazor direktifi olarak yorumlanır *@
@foreach (var section in sections)
{
    <div>@section.Title</div>
}

@* DOGRU *@
@foreach (var item in sections)
{
    <div>@item.Title</div>
}
```

### Sayfa Boyutu Kuralı

`.razor` ve `.cs` dosyaları mümkün olduğunca **400 satırın** altında tutulmalıdır. Büyük sayfalar alt bileşenlere bölünmelidir.

### Bileşen İsimlendirmesi

- Sayfa bileşenleri: `[ModülAdı]List.razor`, `[ModülAdı]Detail.razor`
- Alt bileşenler: `[ModülAdı][İşlev].razor` (ör. `DecisionList.razor`, `KpiCards.razor`)

### SessionService Kullanımı

```csharp
@inject SessionService Session

// Aktif kullanıcıyı almak için
var user = Session.ActiveUser;
var isLoggedIn = Session.IsLoggedIn;
```

`Session.ActiveUser` gerçek DB verisinden gelir (artık hardcoded değil).

---

## 10. Yeni Özellik Ekleme Rehberi

Adım adım örnek: "Duyurular" modülü eklemek.

### Adım 1: Model Oluştur

`Data/Models/Announcement.cs`:

```csharp
namespace YonetIQ.Data.Models;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### Adım 2: Veritabanı Tablosunu Oluştur

`Data/SeedData/InfrastructureSeed.cs` içine ekle:

```csharp
await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Announcements')
    BEGIN
        CREATE TABLE Announcements (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Title NVARCHAR(200) NOT NULL,
            Content NVARCHAR(MAX) NOT NULL,
            CreatedByUserId INT NOT NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
    END
");
```

### Adım 3: Servis Yaz

`Data/Services/AnnouncementService.cs`:

```csharp
public class AnnouncementService(IConfiguration config, AuditService auditService)
    : BaseService(config, auditService)
{
    public async Task<ServiceResult<List<Announcement>>> GetAllAsync()
    {
        return await ExecuteServiceAsync<List<Announcement>>(async conn =>
        {
            var items = await conn.QueryAsync<Announcement>(
                "SELECT Id, Title, Content, CreatedByUserId, IsActive, CreatedAt FROM Announcements WHERE IsActive = 1 ORDER BY CreatedAt DESC");
            return items.ToList();
        });
    }

    public async Task<ServiceResult> CreateAsync(Announcement ann)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "INSERT INTO Announcements (Title, Content, CreatedByUserId) VALUES (@Title, @Content, @CreatedByUserId)",
                ann);
            LogAction("Announcement", "Create", new { ann.Title });
        });
    }
}
```

### Adım 4: DI Kaydı

`Program.cs`:

```csharp
builder.Services.AddScoped<AnnouncementService>();
```

### Adım 5: Razor Sayfası Oluştur

`Components/Pages/Announcement/AnnouncementList.razor`:

```razor
@page "/duyurular"
@inject AnnouncementService AnnSvc

<h3>Duyurular</h3>

@if (announcements is null)
{
    <p>Yükleniyor...</p>
}
else
{
    @foreach (var ann in announcements)
    {
        <div class="yi-card">
            <h5>@ann.Title</h5>
            <p>@ann.Content</p>
        </div>
    }
}

@code {
    private List<Announcement>? announcements;

    protected override async Task OnInitializedAsync()
    {
        var result = await AnnSvc.GetAllAsync();
        if (result.IsSuccess)
            announcements = result.Data;
    }
}
```

### Adım 6: NavMenu'ye Ekle

`Components/Layout/NavMenu.razor` içine menü öğesi ekle.

---

## 11. AI Entegrasyonu

### AiInsightService Kullanımı

```csharp
@inject AiInsightService AiSvc

// Rapor yorumlama
var result = await AiSvc.GenerateQueryCommentAsync(query, queryResult);

// Toplantı notu
var result = await AiSvc.GenerateMeetingNotesAsync(meeting);

// Görev önerisi
var result = await AiSvc.SuggestTaskFromNoteAsync(note);
```

### Yapılandırma

`appsettings.json` veya `.env`:

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "AIza...",
      "Model": "gemini-1.5-flash",
      "Endpoint": "https://generativelanguage.googleapis.com"
    }
  }
}
```

Alternatif olarak `.env` dosyasında `GEMINI_API_KEY` tanımlanabilir.

### Fallback Davranışı

AI servisi erişilemez olduğunda `AiInsightService` hata fırlatmaz; `IsSuccess = false` ve `Data` alanında yerel fallback içeriği döner.

---

## 12. Güvenlik

### Şifre Hashing

HMACSHA256 + rastgele salt:

```csharp
// Hash oluşturma
var (hash, salt) = authService.HashPassword(rawPassword);

// Doğrulama
bool isValid = authService.VerifyPassword(inputPassword, storedHash, storedSalt);
```

- Salt: 32 byte rastgele, her kullanıcı için benzersiz.
- Hash: HMACSHA256(key=salt, data=password), Base64 kodlu.

### AES Şifreleme

Bağlantı stringleri ve hassas ayarlar AES-256-CBC ile şifrelenir:

```csharp
string encrypted = AesEncryption.Encrypt(plainText, base64Key);
string decrypted = AesEncryption.Decrypt(encrypted, base64Key);
```

Şifreleme anahtarı `appsettings.json`'da `DataSourceEncryptionKey` olarak tanımlanmalıdır. Tanımlanmamışsa uygulama geçici anahtar üretir ve konsola uyarı yazar — üretimde bu durum kabul edilemez.

### SQL Injection Koruması

- Tüm parametreler Dapper parametre mekanizması ile geçilir.
- `QueryService`'te dinamik SQL için tehlikeli keyword filtresi (`DROP`, `TRUNCATE`, `DELETE` vb.) uygulanır.

---

## 13. Yaygın Hatalar ve Çözümleri

### CS9113 — Constructor parametresi kullanılmıyor

```
error CS9113: Parameter 'lookupService' is unread
```

Servis constructor'ına eklenen ama kullanılmayan parametre. Kullanmıyorsanız constructor'dan çıkarın.

### Razor @section Çakışması

```
error: The section block '@section.X' is not valid...
```

`@foreach` döngüsünde `section` adını kullanmayın. Değişkeni `item`, `entry` veya modüle özgü bir adla yeniden adlandırın.

### Dark Mode CSS Override Çalışmıyor

Selector özgüllüğü yetersiz. `[data-theme='dark']` yerine `html[data-theme='dark']` kullanın.

### SP Bulunamadı (SqlException 2812)

`IsCompatibleWithFallback` ile yakalayıp inline SQL fallback'e düşün. Uzun vadede SP'yi oluşturun.

### DataSourceEncryptionKey Eksik

```
[WARN] 'DataSourceEncryptionKey' appsettings.json içinde tanımlı değil!
```

Konsol çıktısındaki geçici anahtarı kopyalayıp `appsettings.json`'a ekleyin:

```json
{
  "DataSourceEncryptionKey": "buraya-key-yapistirin"
}
```

### Bağlantı Bulunamadı

```
InvalidOperationException: ConnectionStrings:DefaultConnection veya YONET_CONN tanimli degil.
```

`.env` dosyasında `YONET_CONN` tanımlı mı kontrol edin. `.env` dosyasının proje kökünde (veya üst klasörde) olduğundan emin olun.

### EF Migration Çatışması

Eğer yeni bir tablo zaten `InfrastructureSeed.cs` tarafından oluşturulduysa EF migration çakışabilir. Bu durumda:
1. Migration'da tablo oluşturmayı kaldırın ya da
2. `InfrastructureSeed`'deki guard'ı migration ile koordine edin.

---

*YonetIQ — Geliştirici Rehberi | © 2026 YonetIQ Geliştirme Ekibi*
