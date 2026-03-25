# YonetIQ — Teknik Personel Eğitim Kılavuzu

**Sürüm:** 2026 | **Dil:** Türkçe | **Hedef Kitle:** Sistem Yöneticileri, IT Destek Personeli

---

## İçindekiler

1. [Sistem Gereksinimleri](#1-sistem-gereksinimleri)
2. [Kurulum Adımları](#2-kurulum-adımları)
3. [Yapılandırma (.env ve appsettings.json)](#3-yapılandırma-env-ve-appsettingsjson)
4. [Veritabanı Kurulumu](#4-veritabanı-kurulumu)
5. [Uygulamayı Başlatma ve Yönetme](#5-uygulamayı-başlatma-ve-yönetme)
6. [Backup ve Yedekleme Stratejisi](#6-backup-ve-yedekleme-stratejisi)
7. [Log Yönetimi](#7-log-yönetimi)
8. [Güvenlik Notları](#8-güvenlik-notları)
9. [Sorun Giderme Kılavuzu](#9-sorun-giderme-kılavuzu)
10. [Bakım Görevleri](#10-bakım-görevleri)

---

## 1. Sistem Gereksinimleri

### Sunucu (Minimum)

| Bileşen | Minimum | Önerilen |
|---|---|---|
| İşletim Sistemi | Windows Server 2019 | Windows Server 2022 |
| CPU | 2 vCPU | 4 vCPU |
| RAM | 4 GB | 8 GB |
| Disk | 20 GB SSD | 50 GB SSD |
| .NET Runtime | .NET 10 | .NET 10 |
| SQL Server | SQL Server 2019 | SQL Server 2022 |

### Geliştirici / Test Ortamı

- Windows 10/11, macOS veya Linux
- .NET 10 SDK
- SQL Server 2019+ veya SQL Server Express / Docker

### Ağ Gereksinimleri

| Servis | Port | Açıklama |
|---|---|---|
| YonetIQ Web | 7116 (varsayılan) | HTTP; reverse proxy arkasında HTTPS önerilir |
| SQL Server | 1433 | Sunucu-iç erişim, dışa açmayın |
| SMTP | 587 (StartTLS) | E-posta gönderimi |
| Telegram API | 443 | Telegram bildirim servisi |
| Gemini API | 443 | Google AI servisi |

---

## 2. Kurulum Adımları

### 2.1 .NET 10 Runtime Kurulumu

```powershell
# Windows — winget ile
winget install Microsoft.DotNet.AspNetCore.10

# Kurulumu doğrula
dotnet --version
# Çıktı: 10.x.x
```

Alternatif olarak Microsoft'un resmi sitesinden `ASP.NET Core Runtime 10` indirilip kurulabilir.

### 2.2 Kaynak Kodunu Alma

```bash
# Git ile klonlama
git clone https://github.com/kurumunuz/yonetiq.git D:/YonetIQ

# Belirli bir sürüm/tag ile
git clone --branch v2026.1 https://github.com/kurumunuz/yonetiq.git D:/YonetIQ
```

### 2.3 Uygulamayı Derleme (Publish)

```bash
cd D:/YonetIQ
dotnet publish yonetiq/YonetIQ.csproj -c Release -o D:/publish/yonetiq --nologo
```

Yayın çıktısı `D:/publish/yonetiq` klasörüne yazılır.

### 2.4 Yapılandırma Dosyalarını Hazırlama

Publish klasörüne `.env` dosyası oluşturun (detaylar için Bölüm 3).

### 2.5 Windows Servisi Olarak Kaydetme

```powershell
# Servis oluştur
sc create YonetIQ binPath="D:/publish/yonetiq/YonetIQ.exe --urls http://0.0.0.0:7116" start=auto DisplayName="YonetIQ Portal"

# Servisi başlat
sc start YonetIQ

# Servisi durdur
sc stop YonetIQ
```

Alternatif olarak **IIS** veya **Nginx** reverse proxy arkasında çalıştırılabilir.

### 2.6 IIS ile Yayınlama (Opsiyonel)

1. IIS'e **ASP.NET Core Hosting Bundle** kurun.
2. Yeni site oluşturun ve `D:/publish/yonetiq` dizinini gösterin.
3. Uygulama havuzunu **"No Managed Code"** olarak ayarlayın.
4. `web.config` dosyasının publish klasöründe oluşturulduğunu doğrulayın.

---

## 3. Yapılandırma (.env ve appsettings.json)

### 3.1 .env Dosyası

Uygulama çalıştırılabilir dosyasının bulunduğu klasörde veya üst klasörlerden birinde `.env` dosyası oluşturun:

```env
# Veritabanı bağlantısı (zorunlu)
YONET_CONN=Server=DB_SUNUCU;Database=YonetIQ;User Id=yonetiq_user;Password=GucluSifre123!;TrustServerCertificate=True;

# Google Gemini AI (opsiyonel — tanımlanmazsa AI özellikleri çalışmaz)
GEMINI_API_KEY=AIzaSy...
```

> **Not:** `.env` dosyası kaynak kontrolüne (git) eklenmemelidir. `.gitignore` içinde olduğundan emin olun.

### 3.2 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DB_SUNUCU;Database=YonetIQ;User Id=yonetiq_user;Password=GucluSifre123!;TrustServerCertificate=True;"
  },
  "DataSourceEncryptionKey": "BURAYA-32-BYTE-BASE64-ANAHTAR",
  "AI": {
    "Gemini": {
      "ApiKey": "AIzaSy...",
      "Model": "gemini-1.5-flash",
      "Endpoint": "https://generativelanguage.googleapis.com"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3.3 AES Şifreleme Anahtarı Üretimi

İlk kurulumda uygulama konsola geçici bir anahtar yazar. Bu anahtarı kalıcı olarak `appsettings.json`'a ekleyin:

```bash
# Uygulamayı çalıştırıp konsol çıktısını inceleyin
dotnet YonetIQ.dll --urls http://127.0.0.1:7116

# Konsol çıktısı:
# [WARN] 'DataSourceEncryptionKey' appsettings.json içinde tanımlı değil!
# [WARN] Geçici anahtar oluşturuldu: abc123...
```

Bu geçici anahtarı `appsettings.json`'a `DataSourceEncryptionKey` olarak ekleyin.

> **KRİTİK:** Bu anahtar değişirse veritabanındaki şifreli bağlantı stringleri okunamaz hale gelir. Anahtarı güvenli bir yerde saklayın ve asla değiştirmeyin.

### 3.4 E-posta ve Telegram Ayarları

E-posta ve Telegram ayarları `.env` yerine **veritabanında** (SystemSettings tablosu) saklanır ve uygulama içi **Ayarlar** ekranından yapılandırılır:

1. Uygulamaya yönetici hesabıyla giriş yapın.
2. Sol menüden **Ayarlar** seçin.
3. **E-posta** sekmesinden SMTP bilgilerini girin.
4. **Telegram** sekmesinden bot token'ını girin.
5. **Test Et** düğmeleriyle doğrulayın.

Hassas değerler (şifre, token) veritabanında AES-256 ile şifreli saklanır.

---

## 4. Veritabanı Kurulumu

### 4.1 SQL Server Kullanıcısı Oluşturma

```sql
-- SQL Server Management Studio veya sqlcmd ile çalıştırın

-- Veritabanı oluştur (uygulama da otomatik oluşturur ama manuel daha güvenli)
CREATE DATABASE YonetIQ COLLATE Turkish_CI_AS;

-- Uygulama kullanıcısı oluştur
CREATE LOGIN yonetiq_user WITH PASSWORD = 'GucluSifre123!';
USE YonetIQ;
CREATE USER yonetiq_user FOR LOGIN yonetiq_user;

-- Gerekli izinleri ver
ALTER ROLE db_datareader ADD MEMBER yonetiq_user;
ALTER ROLE db_datawriter ADD MEMBER yonetiq_user;

-- DDL izni gerekiyor (tablo/SP oluşturma için)
GRANT CREATE TABLE TO yonetiq_user;
GRANT CREATE PROCEDURE TO yonetiq_user;
GRANT CREATE FUNCTION TO yonetiq_user;
GRANT ALTER ON SCHEMA::dbo TO yonetiq_user;
```

> **Not:** Üretim ortamında kullanıcı izinlerini en aza indirmek için DBA ile koordine edin. İlk kurulumdan sonra `GRANT CREATE TABLE` gibi izinler kısıtlanabilir.

### 4.2 Otomatik Schema Kurulumu

Uygulama ilk başladığında `SeedData.SeedAsync()` otomatik olarak:

1. Bekleyen EF Core migration'larını uygular.
2. `InfrastructureSeed.cs` ile tabloları, stored procedure'leri oluşturur (yoksa).
3. `DataSeed.cs` ile başlangıç Lookup verilerini ekler (yoksa).

Bu süreç **idempotent**'tir — birden fazla çalıştırılsa da güvenlidir.

### 4.3 İlk Yönetici Hesabı

`DataSeed.cs` başlangıç verisi olarak bir yönetici kullanıcı oluşturur. İlk girişten sonra mutlaka şifreyi değiştirin:

1. Giriş ekranında seed kullanıcısıyla giriş yapın.
2. Profil → Şifremi Değiştir ile yeni güçlü bir şifre belirleyin.
3. Kullanıcılar ekranından yeni yönetici hesapları oluşturun.

---

## 5. Uygulamayı Başlatma ve Yönetme

### Doğrudan Çalıştırma (Geliştirme/Test)

```bash
dotnet run --project D:/YonetIQ/yonetiq --urls http://127.0.0.1:7116
```

### Yayın Dosyasından Çalıştırma

```bash
cd D:/publish/yonetiq
dotnet YonetIQ.dll --urls http://0.0.0.0:7116
```

### Windows Servis Yönetimi

```powershell
# Durum kontrolü
sc query YonetIQ

# Başlat
net start YonetIQ

# Durdur
net stop YonetIQ

# Yeniden başlat
net stop YonetIQ && net start YonetIQ
```

### Port Değiştirme

`--urls` parametresini değiştirin:

```bash
dotnet YonetIQ.dll --urls http://0.0.0.0:8080
```

Veya `appsettings.json`:

```json
{
  "Urls": "http://0.0.0.0:8080"
}
```

### Reverse Proxy (IIS veya Nginx)

Üretimde uygulamayı doğrudan internete açmak yerine bir reverse proxy arkasında çalıştırmak önerilir. Proxy HTTPS'i sonlandırır ve iç port'a yönlendirir.

**IIS ARR (Application Request Routing):**
```xml
<!-- web.config — IIS proxy kuralı -->
<rewrite>
  <rules>
    <rule name="ReverseProxyInboundRule" stopProcessing="true">
      <match url="(.*)" />
      <action type="Rewrite" url="http://localhost:7116/{R:1}" />
    </rule>
  </rules>
</rewrite>
```

---

## 6. Backup ve Yedekleme Stratejisi

### Veritabanı Yedekleme

**Tam Yedek (Full Backup) — Günlük:**

```sql
BACKUP DATABASE YonetIQ
TO DISK = N'D:\Backup\YonetIQ_Full_20260315.bak'
WITH FORMAT, STATS = 10;
```

**Fark Yedek (Differential Backup) — Her 6 Saatte:**

```sql
BACKUP DATABASE YonetIQ
TO DISK = N'D:\Backup\YonetIQ_Diff_20260315_1800.bak'
WITH DIFFERENTIAL, STATS = 10;
```

**Log Yedek (Transaction Log) — Her Saat:**

```sql
BACKUP LOG YonetIQ
TO DISK = N'D:\Backup\YonetIQ_Log_20260315_1200.bak'
WITH STATS = 10;
```

### Otomatik Yedek Planı

SQL Server Agent ile yedek planı oluşturun:

1. SQL Server Management Studio → SQL Server Agent → Jobs → New Job
2. Tam yedek: Her gece 02:00
3. Fark yedek: Her 6 saatte bir
4. Log yedek: Her saatte bir (Recovery Model = Full ise)

### Yedek Saklama Süresi

| Tür | Saklama Süresi |
|---|---|
| Tam yedek | 30 gün |
| Fark yedek | 7 gün |
| Log yedek | 24 saat |
| Uzak kopya (offsite) | 3 ay |

### Dosya Yedekleme

Uygulama disk üzerine dosya yüklemesi yapıyorsa (AttachmentService), ilgili klasörü de yedekleyin. Varsayılan konum `appsettings.json`'daki `AttachmentPath` ayarından kontrol edilir.

### Konfigürasyon Yedekleme

```powershell
# appsettings.json ve .env'i güvenli konuma kopyala (şifreleri içerdiği için dikkatli olun)
Copy-Item D:/publish/yonetiq/appsettings.json D:/SecureBackup/config/
```

---

## 7. Log Yönetimi

### Uygulama Logları (Konsol)

Uygulama `Microsoft.Extensions.Logging` kullanır. Loglar konsola (stdout) yazılır.

Windows Servisi olarak çalışırken logları Event Viewer'da görebilirsiniz:
- **Uygulama** → Kaynak: `YonetIQ`

Log seviyesi `appsettings.json` ile kontrol edilir:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "YonetIQ": "Information"
    }
  }
}
```

### Audit Log (SystemLogs Tablosu)

Kullanıcı eylemleri veritabanındaki `SystemLogs` tablosunda tutulur:

```sql
-- Son 100 audit kaydı
SELECT TOP 100
    Id,
    Module,
    Action,
    Detail,
    CreatedAt
FROM SystemLogs
ORDER BY CreatedAt DESC;
```

Kayıt edilen önemli olaylar:
- Kullanıcı girişleri (başarılı ve başarısız)
- Şifre değiştirme / sıfırlama
- Görev, toplantı, karar oluşturma/güncelleme
- Ayar değişiklikleri
- Dosya yükleme

### Audit Log Temizliği

Audit logları süresiz büyür. Düzenli temizlik için:

```sql
-- 1 yıldan eski logları sil (periyodik çalıştırın)
DELETE FROM SystemLogs
WHERE CreatedAt < DATEADD(YEAR, -1, SYSUTCDATETIME());
```

Bu işlemi SQL Server Agent Job ile otomatikleştirin.

---

## 8. Güvenlik Notları

### Şifre Güvenliği

- Kullanıcı şifreleri **HMACSHA256 + rastgele salt** ile hashlanır.
- Açık metin şifre asla veritabanında saklanmaz.
- `PasswordHash` ve `PasswordSalt` sütunları dışarı sızmamalıdır.

### Şifreleme Anahtarı Yönetimi

- `DataSourceEncryptionKey` (AES-256 anahtarı) kritik öneme sahiptir.
- Bu anahtarı güvenli bir kasada (ör. Azure Key Vault, HashiCorp Vault veya şifreli doküman) saklayın.
- Kaybedilirse veritabanındaki şifreli bağlantı stringleri kurtarılamaz.
- Sunucu yeniden kurulumlarında bu anahtarı `appsettings.json`'a geri yüklemeyi unutmayın.

### Ağ Güvenliği

- Uygulama HTTP üzerinden çalışır; üretimde bir reverse proxy (IIS / Nginx) arkasına alarak HTTPS kullanın.
- SQL Server portunu (1433) internete açmayın; yalnızca uygulama sunucusundan erişilebilir yapın.
- Uygulama sunucusu ile DB sunucusu arasına güvenlik duvarı kuralı ekleyin.

### Uygulama Güvenliği

- Kimlik doğrulaması e-posta + şifre ile yapılır; başarısız girişler audit log'a yazılır.
- Dinamik SQL çalıştırma özelliği (Query Editor) tehlikeli keyword filtresi içerir (`DROP`, `TRUNCATE`, `DELETE`, `INSERT`, `UPDATE`, `EXEC` vb.).
- Dosya yükleme maksimum 10 MB ile sınırlıdır.
- Şifre sıfırlama tokeni 2 saat sonra geçersiz olur.

### Düzenli Yapılması Gerekenler

| Görev | Sıklık |
|---|---|
| Yönetici şifresi değiştirme | 90 günde bir |
| Pasif kullanıcı kontrolü | Aylık |
| Audit log inceleme | Haftalık |
| Güvenlik güncellemeleri (.NET, OS) | Aylık |
| Backup testi (geri yükleme deneyi) | Üç ayda bir |

---

## 9. Sorun Giderme Kılavuzu

### Port Zaten Kullanımda

**Belirti:**
```
System.IO.IOException: Failed to bind to address http://0.0.0.0:7116: address already in use.
```

**Çözüm:**
```powershell
# Portu kullanan işlemi bul
netstat -ano | findstr :7116

# PID ile işlemi sonlandır (dikkatli kullanın)
taskkill /PID 12345 /F

# Veya farklı port kullan
dotnet YonetIQ.dll --urls http://0.0.0.0:8080
```

### Veritabanı Bağlantı Hatası

**Belirti:**
```
InvalidOperationException: ConnectionStrings:DefaultConnection veya YONET_CONN tanimli degil.
```

**Kontrol Listesi:**
1. `.env` dosyasının uygulamanın çalıştığı dizinde (veya üst dizinde) olduğundan emin olun.
2. Bağlantı stringindeki sunucu adı, port ve kimlik bilgilerini doğrulayın.
3. SQL Server servisinin çalıştığını kontrol edin:
   ```powershell
   Get-Service -Name MSSQLSERVER
   ```
4. Güvenlik duvarının 1433 portuna izin verdiğini doğrulayın.
5. `TrustServerCertificate=True` parametresini ekleyin (öz-imzalı sertifika kullanılıyorsa).

### Uygulama Başlamıyor — AES Anahtar Hatası

**Belirti:**
```
[WARN] 'DataSourceEncryptionKey' appsettings.json içinde tanımlı değil!
```

**Çözüm:** Konsol çıktısındaki geçici anahtarı `appsettings.json`'a ekleyin. Üretim ortamında sunucu yeniden başlatılmadan önce anahtarın tanımlı olduğunu doğrulayın.

### Migration Hatası

**Belirti:** Uygulama başlarken migration hatası.

**Çözüm:**
```bash
# Migration durumunu kontrol et
dotnet ef migrations list --project D:/YonetIQ/yonetiq

# Eğer veri tabanı schema ile uyumsuzsa
dotnet ef database update --project D:/YonetIQ/yonetiq
```

### AI Özellikleri Çalışmıyor

**Belirti:** Rapor yorumlama veya SQL üretimi sonuç vermiyor.

**Kontrol Listesi:**
1. `GEMINI_API_KEY` değerini doğrulayın.
2. Uygulama sunucusunun `generativelanguage.googleapis.com:443` adresine erişebildiğini kontrol edin:
   ```powershell
   Test-NetConnection -ComputerName generativelanguage.googleapis.com -Port 443
   ```
3. API kotasını Google Cloud Console'dan kontrol edin.

**Not:** AI servisi erişilemez olduğunda uygulama çalışmaya devam eder; sadece AI özellikleri devre dışı kalır.

### E-posta Gönderilmiyor

**Belirti:** Şifre sıfırlama maili ulaşmıyor.

**Kontrol Listesi:**
1. Ayarlar → E-posta sekmesinden SMTP ayarlarını doğrulayın.
2. "Bağlantıyı Test Et" düğmesine tıklayın.
3. Firewall'ın 587 portuna izin verdiğini kontrol edin.
4. SMTP sunucusunun TLS/SSL gereksinimlerini doğrulayın (`UseSsl` ayarı).

### Telegram Bildirimleri Çalışmıyor

**Belirti:** Rapor Telegram'a gönderilemiyor.

**Kontrol Listesi:**
1. Ayarlar → Telegram sekmesinde "Botu Test Et" düğmesine tıklayın.
2. Bot token'ının geçerli olduğunu doğrulayın.
3. Kullanıcının profilinde Telegram Chat ID'sinin tanımlı olduğundan emin olun.
4. `api.telegram.org:443` adresine erişim izni olduğunu kontrol edin.

### Büyük Dosya Yükleme Hatası

**Belirti:** 10 MB üzeri dosyalar yüklenemiyor.

**Çözüm:** Bu sınır tasarım gereğidir. Daha büyük dosyalar için bir harici dosya depolama sistemi entegrasyonu gereklidir (SharePoint, MinIO vb.).

### Yavaş Performans

**Olası Sebepler ve Çözümler:**

| Sebep | Kontrol | Çözüm |
|---|---|---|
| DB sorgu performansı | SQL Profiler ile yavaş sorguları tespit edin | Eksik index ekleyin |
| Bellek sızıntısı | Görev yöneticisinden RAM kullanımını izleyin | Uygulamayı yeniden başlatın |
| Çok sayıda aktif circuit | IIS/servis loglarını kontrol edin | Kullanıcı sayısına göre sunucu boyutunu artırın |
| Yüksek CPU | Görev yöneticisi ile tespit edin | SP'leri optimize edin |

---

## 10. Bakım Görevleri

### Aylık Görevler

- [ ] Güvenlik güncellemeleri (Windows Update, .NET patch)
- [ ] Disk alanı kontrolü (uygulama ve yedek klasörü)
- [ ] Pasif kullanıcı hesaplarını gözden geçir
- [ ] Audit log boyutunu kontrol et ve eski kayıtları temizle
- [ ] Yedek dosyalarının oluşturulduğunu doğrula

### Üç Aylık Görevler

- [ ] Backup geri yükleme testi yap
- [ ] Yönetici şifrelerini değiştir
- [ ] API anahtar rotasyonu değerlendir (Gemini, Telegram)
- [ ] Veritabanı bakım planını çalıştır (index rebuild, statistics update)

### Yıllık Görevler

- [ ] Kapasite planlaması (disk, RAM, CPU)
- [ ] Felaket kurtarma planını gözden geçir
- [ ] Kullanıcı erişim hakları denetimi

### Güncelleme Prosedürü

1. Yedek alın (veritabanı + uygulama konfigürasyonu).
2. Uygulamayı durdurun: `net stop YonetIQ`
3. Yeni sürümü derleyip yayın klasörüne aktarın.
4. `appsettings.json` ve `DataSourceEncryptionKey`'i kontrol edin (overwrite olmadığından emin olun).
5. Uygulamayı başlatın: `net start YonetIQ`
6. Giriş yaparak temel işlevleri test edin.
7. Herhangi bir sorun olursa yedekten geri yükleyin.

---

*YonetIQ — Teknik Personel Eğitim Kılavuzu | © 2026*
