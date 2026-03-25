# YonetIQ Geliştirme — İş Planı

Son güncelleme: 15.03.2026 — Faz 6 yeni özellikler eklendi

---

## Faz 1: Temel Modernizasyon ve İngilizceye Geçiş [✅ TAMAMLANDI]

- [x] Veritabanı şemasının İngilizceye çevrilmesi (UnifyEnglishSchema migration)
- [x] SeedData / InfrastructureSeed / DataSeed mimarisi
- [x] SessionService: dinamik ActiveUser, IsLoggedIn (hardcoded kaldırıldı)
- [x] AuthService: HMACSHA256 hash, LoginAsync, ChangePasswordAsync
- [x] UserService, TaskService, MeetingService, LookupService, AuditService
- [x] Profil sayfası (/profil): ad/email düzenleme, şifre değiştirme, güç göstergesi
- [x] Login mobile uyum + şifre toggle

---

## Faz 2: AI Entegrasyonu ve Akıllı Raporlama [✅ TAMAMLANDI]

- [x] QueryService: DDL/DML ayrımı, güvenlik filtresi, temp table desteği
- [x] QueryService: DECLARE @Param tespiti, duplicate param fix, ParseParameterInfo
- [x] QueryService: DataSource çoklu bağlantı desteği
- [x] AiInsightService: Gemini API entegrasyonu (SQL üretimi, rapor analizi, toplantı notu, görev önerisi, not analizi)
- [x] AiInsightService: system_instruction (uzman rolü) her çağrıya eklendi
- [x] ReportingService: rapor çalıştırma, favori, paylaşım
- [x] ReportingService: allowDml + parameters parametreleri eklendi
- [x] QueryEditor: DML/Uzman Modu toggle, parametre modal (tip-aware: date/number/select/yesno)
- [x] ReportRun: parametre modal (rapor merkezinden çalıştırma), allowDml desteği
- [x] ResultRenderer: Tablo, Grafik, KPI, MultiKpi tüm tipler
- [x] QueryList: Dashboard/Rapor sekme ayrımı
- [x] SetView / SetForm: Dashboard setleri
- [x] Kişisel Notlar (/notlarim): CRUD, hatırlatma, göreve dönüştürme, AI analizi
- [x] Tekrarlı Toplantı desteği (RecurrenceType/Interval/EndDate)
- [x] ResultType + SetType enum kaldırıldı → Lookups tablosundan string

---

## Faz 3: Şirket İçi İletişim ve Bildirimler [🔄 KISMI]

- [x] MessageService: iç mesajlaşma, CRUD
- [x] AttachmentService: dosya ekleri (max 10MB, disk+DB)
- [x] EmailService: rapor e-posta gönderimi
- [x] TelegramService: rapor Telegram gönderimi
- [ ] Mesajlara "Rapor / Görev / Toplantı" iliştirme (Contextual Attachment)
- [ ] Otomatik bildirimler: görev atama, toplantı daveti, hatırlatma
- [ ] SignalR: gerçek zamanlı bildirim altyapısı

---

## Faz 4: Güvenlik ve Erişim Kontrolü [⏳ BEKLEMEDE]

- [ ] Sayfa düzeyinde yetki kontrolü (@attribute [Authorize(Roles=...)])
- [ ] CalendarService SP adı uyumsuzluğu düzeltmesi (tespit edildi, bekliyor)
- [ ] SP'leri ayrı .sql dosyalarına taşıma

---

## Faz 5: Kalite Güvence (QA) [⏳ PLANLANMADI]

### 5.1 Otomatik Test Altyapısı
- [ ] xUnit test projesi ekleme (`yonetiq.Tests`)
- [ ] BaseService / ServiceResult unit testleri
- [ ] QueryService güvenlik filtresi unit testleri (DDL/DML keyword kontrolü)
- [ ] ParseParameterInfo unit testleri (DECLARE, options comment, edge case'ler)
- [ ] AuthService şifre hash/doğrulama testleri

### 5.2 Servis Entegrasyon Testleri
- [ ] LookupService — GetLookupIdAsync doğruluğu
- [ ] ReportingService — CreateReportRunModelAsync (parametreli + parametresiz)
- [ ] MeetingService — karar→görev dönüşümü transaction testi
- [ ] NoteService — hatırlatma ve göreve dönüştürme

### 5.3 UI / Sayfa Regresyon Testleri (Elle veya Playwright)
- [ ] Login → Dashboard akışı
- [ ] Rapor Merkezi → Çalıştır (parametresiz)
- [ ] Rapor Merkezi → Çalıştır (parametreli — modal açılmalı)
- [ ] Rapor Merkezi → Çalıştır (DML raporlar — allowDml geçmeli)
- [ ] QueryEditor → AI SQL üretimi → çalıştır
- [ ] SetView → tüm widget tipleri render
- [ ] Görev CRUD → Kanban taşıma
- [ ] Toplantı → Karar → Göreve dönüştür
- [ ] Kişisel Not → AI analizi
- [ ] Dark/Light tema geçişi
- [ ] Profil → şifre değiştirme
- [ ] Mobil uyum (responsive breakpoint'ler)

### 5.4 Hata Yönetimi Testleri
- [ ] Geçersiz DataSource → hata mesajı doğru gösteriliyor mu?
- [ ] AI servisi ulaşılamaz → fallback çalışıyor mu?
- [ ] SQL syntax hatası → kullanıcıya anlaşılır mesaj mı?
- [ ] DDL komutu girişi → güvenlik uyarısı çıkıyor mu?
- [ ] 10MB üzeri dosya ekleme → hata mesajı var mı?

---

## Faz 6: İyileştirmeler ve Yeni Özellikler [🔄 DEVAM EDİYOR]

### 6.1 Kritik UX Düzeltmeleri
- [x] **RBAC**: NavMenu'de rol bazlı menü gizleme (SessionService.Role hazır, UI yok) ✅
- [x] **Topbar profil linki**: MainLayout.razor user chip → /profil bağlantısı ✅

### 6.2 Bildirim Merkezi
- [x] `NotificationService`: bildirim üretimi (görev atama, toplantı, mesaj, hatırlatma) ✅
- [x] `Notifications` tablosu: InfrastructureSeed + migration ✅
- [x] Topbar zil ikonu + okunmamış sayacı ✅
- [x] Bildirim dropdown paneli (son 10 bildirim) ✅
- [x] "Tümünü Okundu İşaretle" aksiyonu ✅
- [x] SignalR ile gerçek zamanlı push (Blazor circuit üzerinden) ✅

### 6.3 Rapor Export
- [x] **Excel export**: EPPlus ile .xlsx dosyası oluşturma ✅
- [x] **PDF export**: browser print yaklaşımı ile HTML→PDF çıktısı ✅
- [x] Rapor sonuç ekranına "Export" dropdown butonu ✅
- [ ] E-posta / Telegram ile export dosyası gönderimi

### 6.4 Görev Yorum & Aktivite Akışı
- [x] `TaskComments` tablosu: InfrastructureSeed ✅
- [x] `TaskActivity` tablosu: durum değişikliği, atama, ek logları ✅
- [x] Görev detay sayfasına yorum thread bileşeni ✅
- [x] Aktivite zaman çizelgesi (kim, ne zaman, ne yaptı) ✅
- [ ] Yorum bildirimlerini NotificationService'e bağlama

### 6.5 Zamanlı Raporlar (Scheduled Reports)
- [x] `ScheduledReports` tablosu (rapor ID, interval, kanal: email/telegram, alıcı) ✅
- [x] `ScheduledReportService`: çalıştırma + gönderim ✅
- [x] Zamanlanmış rapor yönetim UI'ı (/raporlar/zamanli) ✅
- [x] `ScheduledReportWorker`: BackgroundService ile tetikleme ✅

### 6.6 KPI Hedef Değerleri
- [x] `KpiTargets` tablosu (metric_key, target_value, operator, period) ✅
- [x] `KpiTargetService`: CRUD ✅
- [x] Hedef yönetim UI'ı (/ayarlar/kpi-hedefleri) ✅
- [x] Dashboard KPI kartlarına hedef göstergesi (yeşil/sarı/kırmızı renk) ✅

### 6.7 Görev Şablonları
- [x] `TaskTemplates` + `TaskTemplateItems` tabloları ✅
- [x] `TaskTemplateService`: CRUD + "şablondan görev oluştur" ✅
- [x] Şablon kütüphanesi sayfası (/gorevler/sablonlar) ✅

### 6.8 Onay Akışı (Approval Workflow)
- [x] `ApprovalFlows` + `ApprovalSteps` tabloları ✅
- [x] `ApprovalService`: talep oluşturma, onay/ret, adım geçişi ✅
- [x] Onay Merkezi sayfası (/onaylar) ✅
- [ ] Onay bekleyen öğeler dashboard widget'ı
- [ ] E-posta / bildirim ile onay talebi

### 6.9 Zaman Takibi (Time Tracking)
- [x] `TimeEntries` tablosu (task_id, user_id, started_at, ended_at, duration_min) ✅
- [x] `TimeTrackingService`: CRUD + toplam efor hesabı ✅
- [x] Efor Raporu sayfası (/gorevler/efor-raporu) ✅
- [x] Görev detayında süre ekleme formu ve timer UI ✅

### 6.10 OKR / Hedef Modülü
- [x] `Objectives` + `KeyResults` tabloları ✅
- [x] `OkrService`: CRUD, progress hesabı ✅
- [x] OKR sayfası (/hedefler): filtre, ilerleme çubukları, KR güncelleme ✅
- [ ] Görev ↔ KeyResult ilişkilendirme UI
- [ ] Dashboard'da OKR özet widget'ı
- [ ] Dashboard'da bekleyen onay sayacı widget'ı

### 6.11 Diğer
- [ ] Global arama (tüm modüller)
- [ ] PWA: manifest.json + service-worker.js
- [ ] Mobil off-canvas menü
- [ ] Kullanıcı kitapçığı (/yardim) güncel içerik

---

## MCP / Altyapı Sorunları [🔧 İNCELENECEK]

- [ ] Claude in Chrome MCP — bağlantı kurulamıyor (extension tab group oluşturulamıyor)
- [ ] Gmail MCP — bağlantı durumu kontrol edilecek
- [ ] Scheduled Tasks MCP — yapılandırma doğruluğu kontrol edilecek
- [ ] MCP Registry — arama fonksiyonu test edilecek

---

## Tamamlanan Son Oturum (15.03.2026)

| Konu | Durum |
|------|-------|
| Rapor merkezinden çalıştırma hatası | ✅ Düzeltildi |
| allowDml ReportRun'a geçirilmedi | ✅ Düzeltildi |
| Parametreli raporlar modal göstermiyordu | ✅ Düzeltildi |
| @inject AiInsightService @code içindeydi | ✅ Taşındı |
| AI uzman rolü (system_instruction) | ✅ Eklendi |
| Test: Rapor Merkezi → parametresiz rapor | ✅ Onaylandı |
| Test: Rapor Merkezi → parametreli rapor modal | ✅ Onaylandı |
| 6.1 RBAC NavMenu rol bazlı gizleme | ✅ Tamamlandı |
| 6.1 Topbar user chip → /profil bağlantısı (MainLayout) | ✅ Tamamlandı |
| 6.2 Bildirim Merkezi (NotificationService, tablo, UI) | ✅ Tamamlandı |
| 6.3 Excel export (EPPlus) | ✅ Tamamlandı |
| 6.3 PDF export (browser print, printHtmlReport JS) | ✅ Tamamlandı |
| 6.4 TaskComments + TaskActivity tabloları (InfrastructureSeed) | ✅ Tamamlandı |
| 6.4 TaskCommentService (yorum CRUD + aktivite loglama) | ✅ Tamamlandı |
| 6.4 TaskForm.razor yorum & aktivite akışı paneli | ✅ Tamamlandı |
