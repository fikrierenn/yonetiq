# YonetIQ — Kullanıcı Rehberi

**Sürüm:** 2026 | **Dil:** Türkçe | **Hedef Kitle:** Tüm Sistem Kullanıcıları

---

## İçindekiler

1. [Genel Bakış](#1-genel-bakış)
2. [Sisteme Giriş](#2-sisteme-giriş)
3. [Dashboard (Ana Panel)](#3-dashboard-ana-panel)
4. [Görevler (Kanban)](#4-görevler-kanban)
5. [Toplantılar ve Kararlar](#5-toplantılar-ve-kararlar)
6. [Raporlar ve Sorgular](#6-raporlar-ve-sorgular)
7. [Dashboard Setleri](#7-dashboard-setleri)
8. [Mesajlar](#8-mesajlar)
9. [Notlar](#9-notlar)
10. [Takvim](#10-takvim)
11. [Kullanıcılar](#11-kullanıcılar)
12. [Ayarlar](#12-ayarlar)
13. [Profil](#13-profil)
14. [Denetim İzi](#14-denetim-i̇zi)
15. [SSS (Sıkça Sorulan Sorular)](#15-sss-sıkça-sorulan-sorular)

---

## 1. Genel Bakış

YonetIQ, kurumsal iş süreçlerinizi tek çatı altında toplayan modern bir yönetim portalıdır. Toplantı kararlarından otomatik görev oluşturma, yapay zeka destekli raporlama ve anlık bildirimlerle ekibinizin verimliliğini artırır.

### Temel Özellikler

| Özellik | Açıklama |
|---|---|
| Görev Takibi | Kanban panosu ile görsel görev yönetimi |
| Toplantı Yönetimi | Gündem, karar ve dosya takibi |
| İzlenebilirlik | Karar → Görev dönüşümü ve kaynak takibi |
| AI Desteği | Google Gemini ile SQL üretimi ve rapor yorumlama |
| Raporlama | 12+ farklı grafik tipiyle dinamik raporlar |
| Mesajlaşma | Ekip içi anlık mesajlaşma |
| Kişisel Notlar | Hatırlatıcılı kişisel not defteri |

---

## 2. Sisteme Giriş

![Giriş Ekranı](/docs/screens/login.png)

### Adım Adım Giriş

1. Tarayıcınızda sistem adresini açın (ör. `http://sunucu-adi:7116`).
2. **E-posta** alanına kurumsal e-posta adresinizi girin.
3. **Şifre** alanına şifrenizi girin. Göz ikonuna tıklayarak şifreyi görünür yapabilirsiniz.
4. **Giriş Yap** düğmesine tıklayın.

### Şifrenizi Unuttuysanız

1. Giriş ekranında **"Şifremi Unuttum"** bağlantısına tıklayın.
2. Kayıtlı e-posta adresinizi girin ve **Gönder** deyin.
3. E-postanıza gelen bağlantıya tıklayarak yeni şifrenizi belirleyin.
4. Sıfırlama bağlantısı 2 saat geçerlidir.

> **Not:** Hesabınız kilitliyse veya pasif durumdaysa sistem yöneticinize başvurun.

---

## 3. Dashboard (Ana Panel)

![Dashboard](/docs/screens/dashboard.png)

Sisteme giriş yaptıktan sonra karşılaştığınız ilk ekrandır. Kurumunuzun anlık durumunu özetler.

### Ekran Bölümleri

**KPI Kartları (Üst Bant)**

Her kart, tek bir kritik göstergeyi özetler. Örnek:
- Aktif Görev Sayısı
- Bekleyen Kararlar
- Bu Haftaki Toplantılar
- Tamamlanma Oranı (%)

**Canlı Metrikler**

İlerleme çubukları ve mini grafiklerle departman veya kişi bazlı performansı gösterir.

**Özet Tablolar**

Son görevler, bekleyen kararlar gibi hızlı erişim listeleri.

### Kullanım İpuçları

- KPI kart başlıklarına tıklayarak ilgili modüle hızla geçiş yapabilirsiniz.
- Dashboard verileri sayfa her yüklendiğinde güncellenir.

---

## 4. Görevler (Kanban)

![Görev Listesi](/docs/screens/tasks-list.png)

Görev modülü, hem liste hem de Kanban (sürükle-bırak) görünümü sunar.

### Yeni Görev Oluşturma

1. Sol menüden **Görevler** seçin.
2. Sağ üstteki **+ Yeni Görev** düğmesine tıklayın.
3. Formu doldurun:
   - **Başlık:** Kısa ve anlaşılır bir görev adı (ör. "Aylık satış raporu hazırlanacak")
   - **Açıklama:** Görevin detayları
   - **Atanan Kişi:** Sorumlu kullanıcıyı seçin (ör. Ahmet Yılmaz)
   - **Öncelik:** Düşük / Normal / Yüksek / Kritik
   - **Bitiş Tarihi:** Takvimden seçin
   - **Durum:** Bekliyor / Devam Ediyor / Tamamlandı / İptal
4. **Kaydet** düğmesine tıklayın.

> **AI İpucu:** Açıklama kutusunun yanındaki **AI Düzenleme** butonuna tıklayarak kısa notunuzu profesyonel bir görev tanımına dönüştürebilirsiniz.

### Kanban Görünümü

![Kanban](/docs/screens/tasks-kanban.png)

1. Sayfa sağ üstündeki **Kanban** sekmesine geçin.
2. Görev kartlarını sütunlar arasında sürükleyip bırakarak durumunu güncelleyin.
3. Kart başlığına tıklayarak detayları düzenleyebilirsiniz.

### Görev İzlenebilirliği

Bir görev toplantı kararından oluşturulmuşsa, görev detay sayfasında kaynak toplantı bilgisi görünür. **Kaynak Toplantıya Git** bağlantısı ile tek tıkla o toplantıya ulaşabilirsiniz.

### Dosya Ekleme

1. Görev detay sayfasında **Ekler** bölümüne inin.
2. **Dosya Seç** düğmesine tıklayın ve dosyanızı yükleyin (maks. 10 MB).
3. Yüklenen dosyalar listede görünür; üzerine tıklayarak indirebilirsiniz.

---

## 5. Toplantılar ve Kararlar

![Toplantı Listesi](/docs/screens/meetings-list.png)

### Toplantı Oluşturma

1. Sol menüden **Toplantılar** seçin.
2. **+ Yeni Toplantı** düğmesine tıklayın.
3. Doldurulacak alanlar:
   - **Başlık:** Toplantı adı (ör. "Q2 Bütçe Değerlendirmesi")
   - **Tarih ve Saat**
   - **Yer / Lokasyon** (ör. "Toplantı Odası A" veya "Online – Teams")
   - **Gündem:** Görüşülecek maddeleri yazın
   - **Tekrarlı Toplantı:** Haftalık / Aylık tekrar seçeneği
4. **Kaydet** düğmesine tıklayın.

### Tekrarlı Toplantı Ayarlama

1. Toplantı formunda **Tekrarlı** seçeneğini işaretleyin.
2. Tekrar türünü seçin: Günlük / Haftalık / Aylık.
3. Bitiş tarihini belirleyin.
4. Kaydettiğinizde tüm seri otomatik oluşturulur.

### Karar Ekleme

![Toplantı Detayı](/docs/screens/meeting-detail.png)

1. Toplantı listesinden ilgili toplantıya tıklayın.
2. Detay sayfasında **Kararlar** bölümünde **+ Karar Ekle** düğmesine tıklayın.
3. Formu doldurun:
   - **Karar Metni:** Alınan kararın özeti
   - **Sorumlu:** Kararı uygulayacak kişi (ör. Fatma Demir)
   - **Bitiş Tarihi:** Son tarih
   - **Öncelik:** Düşük / Normal / Yüksek / Kritik
4. **Kaydet** düğmesine tıklayın.

### Kararı Göreve Dönüştürme

1. Kararlar tablosunda, dönüştürmek istediğiniz kararın yanındaki ok (→) ikonuna tıklayın.
2. Sistem sizi otomatik olarak **Yeni Görev** formuna yönlendirir; karar bilgileri önceden doldurulmuş gelir.
3. Gerekirse düzenleyin ve **Görev Oluştur** deyin.
4. Artık hem toplantı sayfasında hem de görev sayfasında karşılıklı bağlantılar görünür.

### AI ile Toplantı Notu Oluşturma

1. Toplantı detay sayfasında **AI Notu Oluştur** düğmesine tıklayın.
2. Yapay zeka, toplantı gündemine ve kararlarına göre otomatik bir özet ve aksiyon listesi üretir.
3. Üretilen metni düzenleyerek kaydedebilirsiniz.

### Dosya Ekleme

Toplantı veya karar detay sayfasındaki **Ekler** bölümünden belgelerinizi yükleyebilirsiniz.

---

## 6. Raporlar ve Sorgular

![Rapor Listesi](/docs/screens/reports-list.png)

### Kayıtlı Rapor Çalıştırma

1. Sol menüden **Raporlar** seçin.
2. Listeden çalıştırmak istediğiniz rapora tıklayın.
3. Rapor parametreleriniz varsa, açılan formda değerleri girin.
4. **Çalıştır** düğmesine tıklayın.
5. Sonuçlar tablo veya grafik olarak görünür.

### Sorgu Editörü (Gelişmiş Kullanım)

![Sorgu Editörü](/docs/screens/query-editor.png)

1. Sol menüden **Sorgular** seçin.
2. **Yeni Sorgu** oluşturun veya mevcut bir sorguyu açın.
3. SQL kodunu metin kutusuna yazın.
4. **AI SQL Üret** düğmesiyle doğal dilde sorgu tanımlaması yazıp otomatik SQL üretebilirsiniz.
5. Görselleştirme tipini seçin: Tablo, Bar Grafik, Pasta Grafik, KPI Kartı vb.
6. **Çalıştır** ile sonuçları önizleyin.
7. **Kaydet** ile sorguyu kalıcı hale getirin.

### Raporu E-posta ile Gönderme

1. Rapor sonuç ekranında **E-posta Gönder** düğmesine tıklayın.
2. Alıcı e-posta adresini girin.
3. **Gönder** deyin. Rapor HTML tablo formatında e-posta olarak iletilir.

### Raporu Telegram'a Gönderme

1. Rapor sonuç ekranında **Telegram'a Gönder** düğmesine tıklayın.
2. Sistem, profilinize kayıtlı Telegram hesabınıza rapor özetini iletir.

### Favori Raporlar

Sık kullandığınız raporları favori olarak işaretleyebilirsiniz. Rapor başlığı yanındaki yıldız (★) ikonuna tıklayarak favorinize ekleyin.

### Rapor Paylaşma

1. Rapor detay sayfasında **Paylaş** düğmesine tıklayın.
2. Paylaşmak istediğiniz kullanıcıyı seçin.
3. Seçilen kullanıcı Raporlar listesinde bu raporu görebilir.

---

## 7. Dashboard Setleri

Dashboard Setleri, birden fazla raporu tek ekranda birleştirmenizi sağlar.

### Yeni Set Oluşturma

1. Sol menüden **Dashboard Setleri** seçin.
2. **+ Yeni Set** düğmesine tıklayın.
3. Set adını ve tipini belirleyin.
4. **Sorgu Ekle** ile mevcut raporlarınızdan istediğinizi seçin.
5. Her sorgu için genişlik (küçük / orta / büyük) ve sırasını ayarlayın.
6. **Kaydet** deyin.

### Set Görüntüleme

Set listesinden herhangi bir sete tıkladığınızda tüm raporlar tek ekranda yan yana/alt alta görünür.

---

## 8. Mesajlar

![Mesajlar](/docs/screens/messages.png)

### Yeni Mesaj Gönderme

1. Sol menüden **Mesajlar** seçin.
2. **+ Yeni Konu** düğmesine tıklayın.
3. Konu başlığını ve alıcıyı seçin (ör. Mehmet Kaya).
4. Mesajınızı yazın ve **Gönder** deyin.

### Mesaja Yanıt Verme

1. İlgili konuya tıklayın.
2. Alt kısımdaki metin kutusuna yanıtınızı yazın.
3. **Gönder** düğmesine tıklayın.

### Dosya Paylaşımı

Mesaj yazarken **Ekle** düğmesiyle dosya ekleyebilirsiniz (maks. 10 MB).

---

## 9. Notlar

![Notlar](/docs/screens/notes.png)

Kişisel not defteri modülüdür. Notlarınız yalnızca size görünür.

### Not Oluşturma

1. Sol menüden **Notlarım** seçin.
2. **+ Yeni Not** düğmesine tıklayın.
3. Doldurulacak alanlar:
   - **Başlık:** Notunuzun adı
   - **İçerik:** Not metnini yazın
   - **Etiketler:** Virgülle ayrılmış etiketler (ör. "toplantı, bütçe, Q2")
   - **Hatırlatma Tarihi:** Belirli bir tarihte size hatırlatma yapılmasını sağlar
4. **Kaydet** deyin.

### Not → Görev Dönüşümü

1. Not detay sayfasında **Göreve Dönüştür** düğmesine tıklayın.
2. Görev formu, not içeriğiyle önceden doldurulur.
3. Gerekirse düzenleyin ve **Görev Oluştur** deyin.

### AI ile Not Analizi

1. Not detay sayfasında **AI Analiz** düğmesine tıklayın.
2. Yapay zeka notunuzu analiz ederek öneriler sunar.

### Hatırlatmalar

Hatırlatma tarihi geldiğinde dashboard'unuzda bir uyarı belirir.

---

## 10. Takvim

![Takvim](/docs/screens/calendar.png)

### Takvim Görüntüleme

Sol menüden **Takvim** seçin. Aylık görünümde toplantılarınız, görev bitiş tarihleri ve özel günler renk kodlarıyla gösterilir.

### Özel Gün Ekleme

1. Takvimde boş bir güne tıklayın.
2. **Özel Gün Ekle** seçeneğini seçin.
3. Gün adını ve notunu yazın, ardından **Kaydet** deyin.

### Etkinliğe Tıklama

Takvimde bir etkinliğe tıkladığınızda ilgili toplantı veya görev detay sayfasına yönlendirilirsiniz.

---

## 11. Kullanıcılar

> Bu modül yalnızca **Yönetici** rolündeki kullanıcılara açıktır.

### Yeni Kullanıcı Ekleme

1. Sol menüden **Kullanıcılar** seçin.
2. **+ Yeni Kullanıcı** düğmesine tıklayın.
3. Formu doldurun:
   - **Ad Soyad:** (ör. Ayşe Çelik)
   - **E-posta:** Benzersiz kurumsal e-posta
   - **Rol:** Admin / Personel
   - **Departman:** İlgili birimi seçin
   - **Şifre:** İlk giriş şifresi
4. **Kaydet** deyin.

### Kullanıcı Düzenleme / Pasif Yapma

1. Kullanıcı listesinde, ilgili kullanıcının düzenle (kalem) ikonuna tıklayın.
2. Değişikliklerinizi yapın.
3. Hesabı geçici devre dışı bırakmak için **Aktif** toggle'ını kapatın.
4. **Kaydet** deyin.

> **Not:** Kullanıcılar sistemden silinemez; yalnızca pasif yapılabilir. Bu sayede geçmiş veriler bozulmaz.

---

## 12. Ayarlar

> Bu modül yalnızca **Yönetici** rolündeki kullanıcılara açıktır.

### E-posta Ayarları

1. Sol menüden **Ayarlar** seçin.
2. **E-posta** sekmesine geçin.
3. SMTP sunucu bilgilerini girin (Host, Port, Kullanıcı, Şifre).
4. **Bağlantıyı Test Et** ile doğrulayın.
5. **Kaydet** deyin.

### Telegram Bot Ayarları

1. **Telegram** sekmesine geçin.
2. Bot token'ınızı girin.
3. **Botu Test Et** ile doğrulayın.
4. **Kaydet** deyin.

### Diğer Ayarlar

Sistem genelindeki yapılandırmalar (API anahtarları, bildirim kuralları vb.) bu ekrandan yönetilir. Hassas değerler (şifreler, token'lar) veritabanında AES-256 ile şifreli saklanır.

---

## 13. Profil

### Profil Sayfasına Erişim

Sol menünün alt bölümündeki kullanıcı adınıza tıklayarak `/profil` sayfasına ulaşabilirsiniz.

### Bilgilerinizi Güncelleme

1. **Ad Soyad** ve **E-posta** alanlarını güncelleyin.
2. **Kaydet** deyin.

### Şifre Değiştirme

1. Profil sayfasında **Şifremi Değiştir** bölümüne inin.
2. **Mevcut Şifre** alanına şu anki şifrenizi girin.
3. **Yeni Şifre** ve **Yeni Şifre (Tekrar)** alanlarını doldurun.
4. Şifre güç göstergesi, şifrenizin güvenlik düzeyini anlık olarak gösterir.
5. **Güncelle** düğmesine tıklayın.

---

## 14. Denetim İzi

> Bu modül yalnızca **Yönetici** rolündeki kullanıcılara açıktır.

Sistemde gerçekleştirilen tüm önemli işlemler (giriş, görev oluşturma, şifre değiştirme vb.) otomatik olarak kaydedilir.

1. Sol menüden **Denetim İzi** / **Audit Log** seçin.
2. Tarih aralığı, kullanıcı veya işlem tipine göre filtreleyin.
3. Her kayıt; kim, ne zaman, hangi modülde ve ne yaptığını gösterir.

---

## 15. SSS (Sıkça Sorulan Sorular)

**S: Şifremi unuttum, ne yapmalıyım?**
C: Giriş ekranındaki "Şifremi Unuttum" bağlantısını kullanın. E-postanıza 2 saatlik geçerlilik süresi olan bir sıfırlama bağlantısı gönderilir.

**S: Bir karar otomatik olarak göreve dönüştürülür mü?**
C: Hayır, dönüşüm için toplantı detay sayfasındaki ok (→) ikonuna manuel olarak tıklamanız gerekir. Bu tasarım bilerek yapılmıştır; tüm kararlar görev gerektirmeyebilir.

**S: Yüklediğim dosyanın boyut sınırı nedir?**
C: Tüm modüllerde maksimum dosya boyutu 10 MB'tır.

**S: Raporlarımı başkalarıyla paylaşabilir miyim?**
C: Evet. Rapor detay sayfasındaki "Paylaş" düğmesiyle belirli kullanıcılara erişim açabilirsiniz.

**S: Kanban panosunda kaç sütun var?**
C: Standart dört sütun vardır: Bekliyor, Devam Ediyor, Tamamlandı, İptal.

**S: AI özelliği çalışmıyorsa ne olur?**
C: AI servisi erişilemez durumdaysa sistem otomatik yerel bir özet üretir ve hata mesajı gösterir. Raporlar ve diğer modüller AI olmadan çalışmaya devam eder.

**S: Toplantı tekrar serisi oluşturduğumda tek bir toplantıyı iptal edebilir miyim?**
C: Evet. Seri içindeki her toplantı bağımsız düzenlenebilir veya iptal edilebilir.

**S: Notlarım başkaları tarafından görülebilir mi?**
C: Hayır. Kişisel notlar yalnızca size görünür; yöneticiler dahil kimse notlarınızı göremez.

**S: Şifre değiştirme işlemi için mevcut şifremi bilmem gerekiyor mu?**
C: Evet. Güvenlik gereği mevcut şifrenizi doğrulamanız zorunludur.

**S: Pasif yapılan kullanıcıların verileri silinir mi?**
C: Hayır. Pasif kullanıcılar sisteme giriş yapamaz ama tüm geçmiş verileri (görevler, kararlar, mesajlar) korunur.

---

*YonetIQ — Kurumsal Yönetim Portali | © 2026*
