# YonetIQ Geliştirici Rehberi (DEVELOPER_GUIDE)

Bu doküman, YonetIQ projesinde geliştirme yapacak mühendisler için mimari yapıyı, teknik standartları ve temel iş akışlarını özetler.

## 1. Mimari Genel Bakış
YonetIQ, **Blazor Server** ve **.NET 10** tabanlı, modern bir yönetim portalıdır. Proje, "English Técnico" (Teknik İngilizce) isimlendirme standartlarını takip ederken, kullanıcı arayüzünde tam Türkçe dil desteği sunar.

### Temel Teknolojiler:
- **Framework:** .NET 10.0 (Sdk="Microsoft.NET.Sdk.Web")
- **UI:** Blazor Server (Interactive), Bootstrap 5 tabanlı `yi-` teması.
- **Veritabanı:** SQL Server 2022+
- **Veri Erişimi:** 
    - **Dapper:** Birincil veri erişim yolu (Stored Procedure ve performanslı sorgular).
    - **EF Core:** Sadece veritabanı şeması ve Migration yönetimi için kullanılır.
- **AI Entegrasyonu:** Google Gemini API (Görev iyileştirme, SQL üretimi).

## 2. Klasör Yapısı
```
/yonetiq
  /Components
    /Dashboard      -> Dashboard alt bileşenleri (KpiCards, MetricProgress vb.)
    /Pages
      /Dashboard    -> Ana Dashboard sayfası (Home.razor)
      /Meeting      -> Toplantı ve Karar sayfaları (MeetingDetail, MeetingList vb.)
      /Task         -> Görev yönetimi sayfaları
      /User         -> Kullanıcı yönetimi
    /Shared         -> Ortak bileşenler (AttachmentManager, PageHero vb.)
  /Data
    /Infrastructure -> AuditService, BaseService, DotEnv, InfrastructureSeed
    /Models         -> POCO sınıfları (TaskItem, Decision, User, Lookup)
    /Services       -> İş mantığı (MeetingService, TaskService, UserService vb.)
  /wwwroot          -> Statik varlıklar (CSS, JS, Brand assets)
```

## 3. Kodlama Standartları
### İsimlendirme Kuralları:
- **Teknik İsimler (İngilizce):** Sınıflar, metodlar, özellikler ve veritabanı nesneleri kesinlikle İngilizce olmalıdır.
    - Örn: `SaveAsync`, `IsActive`, `PriorityLookupId`, `sp_User_List`
- **UI Metinleri (Türkçe):** Kullanıcıya görünen tüm etiketler ve mesajlar Türkçedir.
    - Örn: "Kaydet", "Görev Başarıyla Eklendi", "Bekleyen Kararlar"

### 400 Satır Kuralı:
Kodun okunabilirliğini korumak adına `.razor` ve `.cs` dosyaları mümkün olduğunca **400 satırın** altında tutulur. Büyük sayfalar mantıksal bileşenlere (`DecisionList.razor`, `DashboardTables.razor` vb.) bölünmelidir.

## 4. Kritik İş Mantığı
### Görev İzlenebilirliği (Task Traceability):
Toplantı kararlarından otomatik görev oluşturulması `MeetingService.ConvertDecisionToTaskAsync` üzerinden yönetilir. 
- Karar -> Görev dönüşümünde öncelik ve durum ID'leri `Lookups` tablosundan dinamik olarak eşlenir.
- Görevler üzerinde otomatik olarak kaynak toplantı linki oluşturulur.

### Veritabanı ve SeedData:
Sistem kurulumu `SeedData.cs` orkestrasyonunda `InfrastructureSeed` ve `DataSeed` dosyaları üzerinden yapılır. Bu yapı tabloların, SP'lerin ve başlangıç verilerinin idempotent (tekrarlanabilir) şekilde oluşturulmasını sağlar.

---
© 2026 YonetIQ Geliştirme Ekibi
