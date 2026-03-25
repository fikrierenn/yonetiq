# YonetIQ — Proje Dokümantasyonu

**Sürüm:** 2026 | **Dil:** Türkçe | **Hedef Kitle:** Teknik Liderler, Mimari Kararlar, Proje Yöneticileri

---

## İçindekiler

1. [Proje Amacı ve Kapsamı](#1-proje-amacı-ve-kapsamı)
2. [Teknik Stack Detayları](#2-teknik-stack-detayları)
3. [Veri Modeli](#3-veri-modeli)
4. [Servisler Tablosu](#4-servisler-tablosu)
5. [Kritik İş Akışları](#5-kritik-i̇ş-akışları)
6. [API Entegrasyonları](#6-api-entegrasyonları)
7. [Güvenlik Mimarisi](#7-güvenlik-mimarisi)
8. [Bilinen Sınırlamalar ve Teknik Borç](#8-bilinen-sınırlamalar-ve-teknik-borç)
9. [Gelecek Geliştirme Yol Haritası](#9-gelecek-geliştirme-yol-haritası)

---

## 1. Proje Amacı ve Kapsamı

### Vizyon

YonetIQ, kurumsal yönetim süreçlerini modernize eden, yapay zeka destekli, entegre bir yönetim portalıdır. Hedef; toplantılardan doğan kararları izlenebilir görevlere dönüştürmek, veri odaklı karar alma süreçlerini hızlandırmak ve ekipler arası şeffaflığı artırmaktır.

### Kapsam

| Modül | Durum |
|---|---|
| Dashboard (KPI, metrikler) | Tamamlandı |
| Görev Yönetimi (CRUD, Kanban) | Tamamlandı |
| Toplantı + Karar Yönetimi | Tamamlandı |
| Karar → Görev Dönüşümü (izlenebilirlik) | Tamamlandı |
| Raporlar + Dinamik Sorgular | Tamamlandı |
| Dashboard Setleri | Tamamlandı |
| İç Mesajlaşma | Tamamlandı |
| Kişisel Notlar + Hatırlatma | Tamamlandı |
| Takvim + Özel Günler | Tamamlandı |
| Kullanıcı Yönetimi (CRUD) | Tamamlandı |
| Sistem Ayarları (DB tabanlı) | Tamamlandı |
| Kullanıcı Profili + Şifre Yönetimi | Tamamlandı |
| Denetim İzi (Audit Log) | Tamamlandı |
| Lookup/Taxonomy Yönetimi | Tamamlandı |
| Dosya Ekleri (polimorfik) | Tamamlandı |
| AI Entegrasyonu (Gemini) | Tamamlandı |
| E-posta Entegrasyonu (MailKit) | Tamamlandı |
| Telegram Bot Entegrasyonu | Tamamlandı |
| Rol Tabanlı Erişim Kontrolü (RBAC) | Kısmi (Login çalışıyor, UI gizleme yok) |
| Şifre Sıfırlama E-posta Akışı | Tamamlandı |
| Tekrarlı Toplantı Serileri | Tamamlandı |
| Veri Kaynağı Yönetimi (DataSource) | Tamamlandı |

---

## 2. Teknik Stack Detayları

### Framework ve Çalışma Modeli

```
.NET 10 — ASP.NET Core
└── Blazor Server (Interactive)
    ├── SignalR WebSocket (tarayıcı-sunucu iletişimi)
    ├── Scoped DI (her SignalR circuit için bağımsız servis örnekleri)
    └── API katmanı YOK — servisler doğrudan DB'ye erişir
```

Blazor Server modeli:
- Tüm C# kodu sunucuda çalışır.
- UI değişiklikleri diff olarak tarayıcıya gönderilir.
- Gerçek zamanlı güncellemeler için ek altyapı gerekmez.

### Veri Erişim Katmanı

```
Runtime Veri Erişimi     → Dapper (ADO.NET üzeri mikro-ORM)
Schema/Migration Yönetimi → EF Core (sadece model + migration)
```

EF Core'un `DbContext`'i yalnızca `AppDbContext` içinde ve sadece migration amacıyla kullanılır. Hiçbir çalışma zamanı sorgusu EF üzerinden geçmez.

### UI Kütüphaneleri

| Kütüphane | Kullanım |
|---|---|
| Tabler (Bootstrap 5 tabanlı) | Layout, kart, tablo, form bileşenleri |
| TabBlazor | Tabler'ın Blazor bileşen sarmalayıcısı |
| Bootstrap Icons | İkon seti (lokal asset olarak dahil) |
| Blazored.Toast | Bildirim (toast) mesajları |
| ApexCharts (JS interop) | Grafik ve chart görselleştirme |

### CSS Mimarisi

- Tüm özel stiller: `wwwroot/css/site.css`
- Prefix kuralı: `yi-*` (ör. `yi-card`, `yi-sidebar-nav`)
- Renk sistemi: CSS custom properties (`--yi-*`)
- Dark mode: `html[data-theme='dark']` bloğu (özgüllük 0,1,1)

---

## 3. Veri Modeli

### Ana Tablolar

| Tablo | Açıklama | Birincil İlişkiler |
|---|---|---|
| `Users` | Kullanıcı hesapları | Lookups (Role, Department) |
| `Tasks` | Görevler | Users (AssignedUser), Lookups (Status, Priority) |
| `Meetings` | Toplantılar | Users, Lookups (Type), Meetings (ParentMeeting) |
| `Decisions` | Toplantı kararları | Meetings, Users (AssignedTo), Tasks (LinkedTask) |
| `Lookups` | Taxonomy/Enum değerleri | — (tüm tablolar tarafından kullanılır) |
| `SystemLogs` | Audit trail | — |
| `CommunicationMessages` | İç mesajlar | Users |
| `FileAttachments` | Dosya ekleri (polimorfik) | RelatedEntityType + RelatedEntityId |
| `QueryRecords` | Kayıtlı sorgular | Lookups (ResultType) |
| `QuerySets` | Dashboard setleri | Lookups (SetType) |
| `SetQueries` | Set-Sorgu ilişkisi | QuerySets, QueryRecords |
| `ReportFavorites` | Favori raporlar | Users, QueryRecords |
| `ReportShares` | Paylaşılan raporlar | Users (Owner, Target), QueryRecords |
| `PersonalNotes` | Kişisel notlar | Users, Tasks (LinkedTask) |
| `CalendarSpecialDays` | Özel günler | — |
| `DataSources` | Harici veri kaynakları | — (AES şifreli bağlantı stringleri) |
| `SystemSettings` | Uygulama ayarları | — (hassas değerler AES şifreli) |

### Temel Model Sınıfları

```
BaseEntity
├── User
├── TaskItem
├── Meeting
│   └── Decision (Meeting'e bağlı)
├── Lookup
├── CommunicationMessage
├── FileAttachment (polimorfik)
├── QueryRecord
├── QuerySet
│   └── SetQuery (QuerySet ↔ QueryRecord many-to-many)
├── ReportFavorite
├── ReportShare
├── PersonalNote
├── CalendarSpecialDay
└── SystemLog
```

### Önemli İlişkiler

**Görev İzlenebilirliği:**
```
Meeting → Decision (IsConvertedToTask = true)
       → TaskItem (LinkedTaskId)
              ↑
         SourceEntityType = "Decision"
         SourceEntityId = Decision.Id
```

**Polimorfik Dosya Ekleri:**
```
FileAttachment
├── RelatedEntityType = "Task"    → TaskItem.Id
├── RelatedEntityType = "Meeting" → Meeting.Id
└── RelatedEntityType = "Message" → CommunicationMessage.Id
```

**Tekrarlı Toplantılar:**
```
Meeting (ParentMeetingId = null, RecurrenceType, RecurrenceInterval, RecurrenceEndDate)
└── Meeting (ParentMeetingId = üst toplantı ID)
└── Meeting (ParentMeetingId = üst toplantı ID)
```

### Lookups Tablosu (Taxonomy)

Tüm sabit liste değerleri Lookups tablosunda tutulur. Enum kullanılmaz:

```sql
-- Örnek Lookups kayıtları
[Group]        [Value]        [Id]
'Role'         'Admin'        1
'Role'         'Personel'     2
'Status'       'Bekliyor'     10
'Status'       'Devam Ediyor' 11
'Status'       'Tamamlandı'   12
'Priority'     'Düşük'        20
'Priority'     'Normal'       21
'Priority'     'Yüksek'       22
'Priority'     'Kritik'       23
'ResultType'   'Table'        30
'ResultType'   'BarChart'     31
'ResultType'   'KpiCard'      32
'SetType'      'Dashboard'    40
```

Lookup ID'leri `LookupService.GetLookupIdAsync(group, value)` ile dinamik alınır — kodda hardcoded ID kullanılmaz.

---

## 4. Servisler Tablosu

| Servis | Sorumluluk | Bağımlılıklar | DI Ömrü |
|---|---|---|---|
| `AuthService` | HMACSHA256 şifre hash, giriş, şifre değiştirme, şifre sıfırlama | AuditService, EmailService | Scoped |
| `SessionService` | Blazor circuit başına oturum bilgisi | — | Scoped |
| `TaskService` | Görev CRUD, durum güncelleme, Kanban | AuditService, LookupService | Scoped |
| `MeetingService` | Toplantı CRUD, karar yönetimi, karar→görev dönüşümü, tekrarlı seri | AuditService, LookupService | Scoped |
| `QueryService` | Dinamik SQL çalıştırma, şema keşfi, keyword filtresi, parametre tespiti | AuditService | Scoped |
| `QuerySetService` | Dashboard set yönetimi, set-sorgu ilişkisi | AuditService | Scoped |
| `ReportingService` | Rapor listeleme, çalıştırma, favori, paylaşım | AuditService | Scoped |
| `CalendarService` | Takvim etkinlikleri, özel günler | AuditService | Scoped |
| `UserService` | Kullanıcı CRUD | AuditService, LookupService | Scoped |
| `LookupService` | Lookup listeleme, GetLookupIdAsync | — | Scoped |
| `MessageService` | İç mesajlaşma, mesaj thread'leri | AuditService | Scoped |
| `NoteService` | Kişisel not CRUD, hatırlatma, not→görev | AuditService | Scoped |
| `AttachmentService` | Dosya yükleme/indirme (disk + DB) | AuditService | Scoped |
| `AuditService` | SystemLogs'a kayıt | — | Scoped |
| `AiInsightService` | Gemini API: SQL üretimi, rapor yorumu, toplantı notu, not analizi | HttpClient, IConfiguration | Scoped |
| `EmailService` | MailKit ile SMTP e-posta gönderimi | SettingsService | Scoped |
| `TelegramService` | Telegram Bot API mesaj gönderimi, Login Widget doğrulama | SettingsService, HttpClient | Singleton (HttpClient) |
| `SettingsService` | SystemSettings tablosu CRUD, AES şifreleme/çözme | AuditService | Scoped |
| `DataSourceService` | Harici veri kaynağı CRUD (bağlantı stringleri AES şifreli) | AuditService | Scoped |
| `AnalyticsService` | Dashboard metrik ve analitik hesaplamaları | — | Scoped |

---

## 5. Kritik İş Akışları

### 5.1 Kullanıcı Giriş Akışı

```
1. Kullanıcı e-posta + şifre girer
2. AuthService.LoginAsync(email, password)
   a. DB'den kullanıcıyı e-posta ile bul
   b. Lookups JOIN ile RoleName doldur
   c. PasswordHash + PasswordSalt ile HMACSHA256 doğrulama
   d. Başarısız → ServiceResult.Failure + AuditLog
   e. Başarılı → ServiceResult.Success(user)
3. SessionService.SetUser(user) — circuit-scoped bilgi saklanır
4. NavigationManager ile Dashboard'a yönlendir
```

### 5.2 Karar → Görev Dönüşümü

```
1. Kullanıcı toplantı detay sayfasında "Dönüştür" ikonuna tıklar
2. Seçilen Decision.Id ile yeni TaskItem formu açılır
   (Başlık, öncelik, sorumlu karar bilgisinden önceden doldurulur)
3. Kullanıcı onaylar → MeetingService.ConvertDecisionToTaskAsync(decisionId, taskItem)
   a. Başlangıç durum ve öncelik ID'leri LookupService'ten alınır
   b. TaskItem INSERT — SourceEntityType = "Decision", SourceEntityId = decision.Id
   c. Decision UPDATE — IsConvertedToTask = true, LinkedTaskId = yeni task ID
   d. AuditLog kaydı
4. Her iki sayfa çapraz bağlantı gösterir
```

### 5.3 Dinamik Rapor Çalıştırma

```
1. Kullanıcı QueryRecord seçer
2. SQL'de @Param regex ile parametreler tespit edilir
3. Parametre formu gösterilir (varsa)
4. "Çalıştır" → QueryService.RunQueryAsync(sql, parameters, dataSourceId?)
   a. Tehlikeli keyword filtresi (DROP, TRUNCATE, DELETE vb.)
   b. Bağlantı DataSourceService'ten alınır (varsayılan = uygulama DB)
   c. Dapper ile sorgu çalıştırılır
   d. QueryResult (Columns, Rows) döner
5. ResultType'a göre DynamicTable veya ApexRenderer bileşeni render eder
6. Opsiyonel: AI yorumu, E-posta, Telegram gönderimi
```

### 5.4 Şifre Sıfırlama Akışı

```
1. Kullanıcı "Şifremi Unuttum" formuna e-posta girer
2. AuthService.InitiatePasswordResetAsync(email, emailSvc, appBaseUrl)
   a. E-posta ile kullanıcı aranır
   b. Kullanıcı yoksa bile "Başarılı" döner (enumeration koruması)
   c. 32 byte hex token üretilir, 2 saat geçerlilik süresiyle DB'ye yazılır
   d. MailKit ile reset linki e-posta olarak gönderilir
3. Kullanıcı e-postadaki linke tıklar (/sifre-sifirla?token=...)
4. AuthService.ResetPasswordAsync(token, newPassword)
   a. Token + geçerlilik süresini kontrol et
   b. Yeni HMACSHA256 hash üret
   c. PasswordHash, PasswordSalt güncelle
   d. Token ve expiry null yap
```

### 5.5 AI SQL Üretimi

```
1. Kullanıcı doğal dil sorgu açıklaması yazar
2. AiInsightService: Şema bilgisi + açıklama prompt olarak Gemini'ye gönderilir
3. Gemini SQL döner
4. SQL, editörde otomatik doldurulur
5. Kullanıcı inceleyip çalıştırır
6. AI erişilemezse hata mesajı gösterilir, akış devam eder
```

---

## 6. API Entegrasyonları

### 6.1 Google Gemini AI

| Özellik | Değer |
|---|---|
| Model | gemini-1.5-flash (varsayılan, ayarlanabilir) |
| Endpoint | https://generativelanguage.googleapis.com |
| Yapılandırma | `.env` GEMINI_API_KEY veya `appsettings.json` AI:Gemini:ApiKey |
| Kullanım Alanları | SQL üretimi, rapor yorumlama, toplantı notu, görev önerisi, not analizi |
| Fallback | AI erişilemezse yerel özet üretilir; uygulama çalışmaya devam eder |
| HTTP İstemci | `HttpClient` (DI ile enjekte, `AddHttpClient<AiInsightService>`) |

### 6.2 SMTP E-posta (MailKit)

| Özellik | Değer |
|---|---|
| Kütüphane | MailKit + MimeKit |
| Ayarlar | SystemSettings tablosu (DB tabanlı, UI'dan yapılandırılır) |
| Güvenlik | StartTLS (port 587 varsayılan) veya None |
| Kullanım Alanları | Şifre sıfırlama, rapor gönderimi |
| Şifreli Saklanma | SMTP şifresi AES-256 ile şifreli |
| Test | `EmailService.TestConnectionAsync()` |

E-posta için DB'de saklanan ayarlar (`SystemSettings` tablosu, `Group = 'Email'`):
- `Email.Host` — SMTP sunucu adresi
- `Email.Port` — Port numarası
- `Email.UseSsl` — TLS kullanım durumu
- `Email.Username` — SMTP kullanıcı adı
- `Email.Password` — SMTP şifresi (AES şifreli)
- `Email.FromAddress` — Gönderen e-posta
- `Email.FromName` — Gönderen görünen adı

### 6.3 Telegram Bot API

| Özellik | Değer |
|---|---|
| Endpoint | https://api.telegram.org/bot{token}/ |
| Ayarlar | SystemSettings tablosu (`Telegram.BotToken`, AES şifreli) |
| Kullanım Alanları | Rapor iletimi, bildirimler |
| Login Widget | HMACSHA256(SHA256(BotToken), data_check_string) doğrulama |
| Test | `TelegramService.TestBotAsync()` |
| Mesaj Limiti | 4000 karakter (Telegram 4096 limiti, güvenli sınır) |

---

## 7. Güvenlik Mimarisi

### Kimlik Doğrulama

```
Hash = HMACSHA256(key=Base64(Salt_32byte), data=UTF8(Password))
Salt = RandomNumberGenerator.GetBytes(32) — her kullanıcı için benzersiz
```

- Açık metin şifre asla saklanmaz.
- Giriş başarısız olduğunda kullanıcı adı/şifre ayrımı yapılmaz ("InvalidCredentials").
- Başarısız girişler audit log'a yazılır.

### Veri Şifreleme (AES-256-CBC)

DataSource bağlantı stringleri ve hassas ayar değerleri şifrelenir:

```
IV (16 byte, rastgele) + CipherText → Base64 → DB'ye yazılır
Anahtar: 32 byte, Base64 kodlu → appsettings.json DataSourceEncryptionKey
```

### SQL Injection Koruması

- Dapper ile tüm parametreler parameterized query olarak geçilir.
- Query Editor'de tehlikeli kelime listesi:

```
DROP, TRUNCATE, DELETE, INSERT, UPDATE, ALTER, CREATE, EXEC,
EXECUTE, xp_, sp_executesql, OPENROWSET, BULK, GRANT, REVOKE
```

### Şifre Sıfırlama Güvenliği

- Token: 32 byte rastgele → 64 karakter hex
- Geçerlilik süresi: 2 saat
- Kullanıcı bulunamasa da aynı başarı mesajı döner (enumeration koruması)
- Token tek kullanımlıktır; kullanıldıktan sonra null'a set edilir

### Telegram Login Widget Doğrulama

Telegram'ın resmi yöntemi kullanılır:
```
SecretKey = SHA256(BotToken)
HMACSHA256(SecretKey, sorted_field=value\n...) == hash parametresi
auth_date 24 saatten eski değil
```

---

## 8. Bilinen Sınırlamalar ve Teknik Borç

### Aktif Teknik Borç

| Konu | Etki | Öncelik |
|---|---|---|
| RBAC — NavMenu'de rol bazlı gizleme eksik | Yetkisiz kullanıcılar menüyü görebilir (erişim engeli var ama görünür) | Yüksek |
| CalendarService SP adı uyumsuzluğu | CalendarService belirli SP'yi çağırırken hata alabilir | Orta |
| Profil sayfası topbar kullanıcı chip'ten erişilemiyor | UX sorunu, MainLayout.razor'da link eksik | Düşük |
| SP'ler inline SQL içinde gömülü | Bakım zorluğu, ayrı .sql dosyalarına taşınmalı | Düşük |
| Unit/Integration test yok | Regresyon riski | Orta |

### Mimari Sınırlamalar

- **API katmanı yok:** Mobil uygulama veya harici sistem entegrasyonu için REST/gRPC API eklenmesi gerekir.
- **Gerçek zamanlı bildirimler:** SignalR circuit bazında çalışır; sunucu-başlatmalı push bildirimi için ek altyapı gerekir.
- **Dosya depolama:** Dosyalar uygulama sunucusunun diskinde saklanır; kümeleme/yük dengeleme durumunda paylaşımlı depolama gerekir.
- **Yatay ölçekleme:** Blazor Server'ın circuit yapısı nedeniyle yük dengeleme için sticky session gerekir.

### Veri Sınırlamaları

- Dosya boyutu limiti: 10 MB (AttachmentService sabit)
- E-posta rapor satır limiti: 500 satır (EmailService sabit)
- Telegram mesaj limiti: 4000 karakter

---

## 9. Gelecek Geliştirme Yol Haritası

### Kısa Vadeli (1-3 Ay)

- **RBAC Tamamlama:** NavMenu'de `SessionService.ActiveUser.RoleName` kontrolü ile rol bazlı menü gizleme.
- **Profil Topbar Linki:** `MainLayout.razor` kullanıcı chip bileşenine `/profil` linki ekleme.
- **CalendarService SP Uyumsuzluğu:** SP adını düzelt veya inline SQL fallback ekle.
- **SP'leri .sql Dosyalarına Taşıma:** Bakım kolaylığı için SP tanımlarını `Data/Sql/` klasörüne al.

### Orta Vadeli (3-6 Ay)

- **Unit ve Integration Testler:** xUnit + Moq ile servis katmanı testleri.
- **Rapor Export:** PDF (iText) ve Excel (EPPlus) export desteği.
- **Global Arama:** Tüm modüllerde arama (görev, toplantı, not, kullanıcı).
- **Bildirim Merkezi:** Uygulama içi bildirimler ve okunmamış mesaj sayacı.

### Uzun Vadeli (6+ Ay)

- **REST API Katmanı:** Mobil ve harici entegrasyon için ASP.NET Core Minimal API veya Controller.
- **Çoklu Dil Desteği:** .NET resource dosyaları ile i18n.
- **Gelişmiş RBAC:** Sayfa bazlı izin matrisi, özel roller.
- **Harici SSO:** Azure AD veya Google OAuth entegrasyonu.
- **Zaman Serisi Analitik:** Trend analizi için AnalyticsService genişletmesi.
- **Mobil Uygulama:** .NET MAUI veya React Native ile mobil istemci.

---

## Ek: Migration Geçmişi

| Migration | Tarih | İçerik |
|---|---|---|
| InitialCreate | 2025 | İlk şema: Users, Tasks, Meetings, Decisions, Lookups |
| AddKullaniciSifre | 2025 | PasswordHash, PasswordSalt kolonları |
| UnifyEnglishSchema | 2025 | Tüm tablo ve kolon adları İngilizce'ye çevrildi |
| AddTaskItemSourceTraceability | 2026 | SourceEntityType, SourceEntityId kolonları |
| AddPersonalNotes | 2026 | PersonalNotes tablosu |

---

*YonetIQ — Proje Dokümantasyonu | © 2026 YonetIQ Proje Ekibi*
