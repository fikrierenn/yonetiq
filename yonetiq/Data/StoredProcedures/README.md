# YonetIQ - Stored Procedures

Bu klasor, `InfrastructureSeed.cs` dosyasindaki stored procedure tanimlarinin referans kopyalarini icerir.

> **Not:** Runtime'da SP olusturma islemi hala `InfrastructureSeed.cs` uzerinden `CREATE OR ALTER PROCEDURE` ile yapilmaktadir. Bu `.sql` dosyalari versiyon kontrol, kod inceleme, manuel deployment ve dokumantasyon amaclidir.

## SP Listesi

| # | SP Adi | Servis | Aciklama |
|---|--------|--------|----------|
| 1 | `sp_Lookup_ListByGroup` | LookupService | Belirtilen gruba ait aktif lookup degerlerini listeler |
| 2 | `sp_CalendarSpecialDay_List` | CalendarService | Ozel gunleri tarih araligina ve aktiflik durumuna gore listeler |
| 3 | `sp_CalendarSpecialDay_ListAsEvents` | CalendarService | Ozel gunleri takvim event formatinda dondurur |
| 4 | `sp_Calendar_ListAsEvents` | CalendarService | Toplanti ve gorevleri birlestirerek takvim event listesi olusturur |
| 5 | `sp_CalendarSpecialDay_Save` | CalendarService | Ozel gun kaydeder veya gunceller (Upsert) |
| 6 | `sp_CalendarSpecialDay_Delete` | CalendarService | Belirtilen ozel gunu siler |

## Kaynak Metot Eslesmesi

| SP Adi | InfrastructureSeed.cs Metodu |
|--------|------------------------------|
| `sp_Lookup_ListByGroup` | `PrepareLookupsTableAsync` |
| `sp_CalendarSpecialDay_List` | `PrepareCalendarSpecialDayInfrastructureAsync` |
| `sp_CalendarSpecialDay_ListAsEvents` | `PrepareCalendarSpecialDayInfrastructureAsync` |
| `sp_Calendar_ListAsEvents` | `PrepareCalendarSpecialDayInfrastructureAsync` |
| `sp_CalendarSpecialDay_Save` | `PrepareCalendarSpecialDayInfrastructureAsync` |
| `sp_CalendarSpecialDay_Delete` | `PrepareCalendarSpecialDayInfrastructureAsync` |

## Kullanim

Bu dosyalar dogrudan SQL Server Management Studio veya `sqlcmd` ile calistirabilir:

```bash
sqlcmd -S <server> -d <database> -i sp_Calendar_ListAsEvents.sql
```

Veya toplu olarak:

```bash
for f in *.sql; do sqlcmd -S <server> -d <database> -i "$f"; done
```
