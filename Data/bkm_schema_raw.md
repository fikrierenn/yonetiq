# BKM Kitap DB Şema Keşif Notları (25.03.2026)

## Sunucu: 192.168.40.201
- Auth: SQL Auth (sa)
- Ana DB: DerinSISBkm (730 GB)

## DerinSISBkm — En Büyük Tablolar
| Tablo | Satır | Anlam |
|-------|-------|-------|
| api_log | 1.4B | API log |
| OneriSiparis | 160M | Sipariş önerileri |
| fytOzl | 128M | Fiyat özellikleri |
| irsAyr | 66M | İrsaliye ayrıntı (satış satırları) |
| irsHrk | 57M | İrsaliye hareket |
| sipAyr | 42M | Sipariş ayrıntı |
| fatAyr | 40M | Fatura ayrıntı |
| mhsFis | 39M | Muhasebe fişi |
| car | 24M | Cari hesap |
| tmpSATISLAR | 22M | Geçici satış tablosu |

## fatAyr Kolonları (Fatura Ayrıntı — 40M satır)
- ehID (int) — Fatura başlık ID (FK→fat)
- ehSira (int) — Satır sırası
- ehStkID (int) — Stok/Ürün ID (FK→urn)
- ehAdet (decimal) — Adet
- ehAdetN (decimal) — Net adet
- ehTutar (decimal) — Tutar
- ehIndirim (decimal) — İndirim tutarı
- ehTutarKDV (decimal) — KDV dahil tutar
- ehKDV (tinyint) — KDV oranı
- ehi1-ehi5 (decimal) — İndirim kademeleri
- ehTutarN (decimal) — Net tutar
- ehIrsID (int) — İrsaliye ID
- ehSipID (int) — Sipariş ID
- ehBirim (varchar) — Birim
- ehMaliyet (decimal) — Maliyet

## fat Kolonları (Fatura Başlık)
- eID (int) — PK
- eNo (varchar) — Fatura no
- eMekan (int) — Mağaza ID (FK→posMagaza)
- eFirma (int) — Firma/cari ID
- eTarihS (smalldatetime) — Fatura tarihi (sıralama?)
- eTarih (smalldatetime) — Fatura tarihi
- eGC (tinyint) — Giriş/Çıkış (1=satış, 2=alış?)
- eTip (tinyint) — Fatura tipi
- eDurum (tinyint) — Durum
- eY1-eY5 (decimal) — Yekün toplamları
- eT6 (decimal) — Genel toplam?
- yuv (decimal) — Yuvarlama
- eFirmaMkn (int) — ?
- eDvzID (tinyint) — Döviz ID
- eDvzKur (decimal) — Döviz kuru

## posMagaza Kolonları (60 kolon)
- mekanID (int) — PK
- mekanAd (varchar) — Mağaza adı
- mekanKod (varchar) — Mağaza kodu
- mekanTip (tinyint) — Tip
- mekanDurum (tinyint) — Durum (1=aktif?)
- mekanGrup (tinyint) — Grup
- mekanNo (tinyint) — No

## urn Kolonları (Ürün — 166 kolon, duplicate var)
- stkID (int) — PK
- stkKod (varchar) — Stok kodu (ISBN?)
- stkAd (varchar) — Ürün adı
- fiyatS (decimal) — Satış fiyatı
- fiyatA (decimal) — Alış fiyatı
- urnGrpID (int) — Ürün grup ID
- urnMrkID (smallint) — Marka ID
- urnKtgrID (tinyint) — Kategori ID (FK→urnKtgr)
- urnKtgrID1-7 (tinyint) — Alt kategori ID'leri
- urnDurum (tinyint) — Durum
- sahafAnaStokId (int) — Sahaf ana stok?

## Adlandırma Konvansiyonu
- e* = Evrak (eID, eNo, eTarih, eMekan)
- eh* = Evrak hareket/satır (ehStkID, ehTutar, ehAdet)
- stk* = Stok/Ürün (stkID, stkKod, stkAd)
- mekan* = Mağaza/Mekan (mekanID, mekanAd)
- urn* = Ürün ek bilgiler (urnKtgrID, urnMrkID)

## KRİTİK SORULAR (Fikri'ye sorulacak)
1. **Ciro raporu:** fatAyr.ehTutar mı, ehTutarKDV mi, ehTutarN mı kullanılıyor?
2. **Satış filtresi:** fat.eGC=1 satış mı? eTip değerleri neler?
3. **Tarih:** fat.eTarih mi eTarihS mi kullanılıyor?
4. **İade:** Negatif adet/tutar mı, ayrı kayıt mı?
5. **Kategori adları:** urnKtgr tablosundaki kolon adı ne?
6. **Marka adları:** urnMrk tablosundaki kolon adı ne?
7. **ISBN:** stkKod ISBN mi, yoksa urnBilgi'de mi?
