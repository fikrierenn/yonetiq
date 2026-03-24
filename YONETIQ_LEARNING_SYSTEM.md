# YonetIQ — DB Skill Sistemi + Güçlü Öğrenme Mimarisi

**Tarih:** 25.03.2026
**Önceki belge:** YONETIQ_MASTER_PLAN.md
**Bu belge:** Skill kalıcılığı + multi-sinyal öğrenme sistemi
**Uygulama sırası:** MASTER_PLAN tamamlandıktan sonra bu belge

---

## MİMARİ KARAR: NEDEN DB'DE SKILL?

Şu an skill'ler `Definitions/*.cs` statik class'larında. Sorunlar:
- Skill değiştirmek → deploy gerekiyor
- Temperature, max token, prompt dosyası → kod değişikliği
- Skill kapat/aç → kod değişikliği
- A/B test → imkansız

DB'de skill → admin panelinden temperature'ı 0.2'den 0.3'e çekebilirsin. Deploy yok.

---

## MİMARİ KARAR: ÖĞRENME STRATEJİSİ

Şu an: thumbs up → admin onaylar → few-shot örneği olur. Bu çalışmaz — admin darboğaz.

**Doğru sistem: çok sinyalli güven puanı**

| Sinyal | Ağırlık | Otomasyon |
|--------|---------|-----------|
| Thumbs up (explicit) | +3.0 | otomatik |
| Kullandı, dokunmadı | +1.0 | implicit, otomatik |
| SQL düzeltmedi (<5% diff) | +1.5 | implicit, otomatik |
| Aynı soruyu tekrar sormadı | +0.5 | implicit, zaman bazlı |
| Thumbs down | -5.0 | otomatik, pattern sil |
| SQL büyük değişiklik | -2.0 | correction → öğren |
| Kimse kullanmadı (30 gün) | -0.3 | decay, otomatik |

**Güven Eşikleri:**
- ≥ 8.0 puan → OTO ONAY (admin yok)
- 4.0 – 7.9 → Admin onayı bekle
- < 4.0 → Gürültü, işleme alma

---

## BÖLÜM A — VERİTABANI ŞEMASI

### A.1 — AiSkillDefinitions Tablosu
### A.2 — AiPatternSignals Tablosu (YENİ)
### A.3 — AiPatterns Tablosu Genişletme
### A.4 — SemanticLearningCandidates Tablosu (YENİ)
### A.5 — AiQueryLog Tablosu (YENİ)

## BÖLÜM B — SKILL KALICILIK SİSTEMİ
- B.1: SkillPersistenceService
- B.2: AiSkillRecord Model
- B.3: SkillRegistry Güncelleme (Disable metodu)
- B.4: Program.cs Güncelleme

## BÖLÜM C — GÜÇLÜ ÖĞRENME SİSTEMİ
- C.1: LearningSignalService

## BÖLÜM D — GÜÇLENDİRİLMİŞ SEMANTİK KATMAN
- D.1: SemanticDiscoveryService
- D.2: SemanticService.SearchAsync Güçlendirme
- D.3: AiMemoryService.GetApprovedPatternsAsync Güncelleme

## BÖLÜM E — QUERY SİNYAL ENTEGRASYONU
- E.1: AiOrchestrationService Güncelleme
- E.2: AiQueryLog Kaydetme

## BÖLÜM F — ADMIN UI GÜNCELLEMELERİ
- F.1: Skill Yönetim Sayfası (/admin/skill-yonetim)
- F.2: Semantic Learning Admin Sayfası (/admin/semantik-ogrenme)

## BÖLÜM G — PROMPT OVERRIDE ENTEGRASYONU

## BÖLÜM H — DI KAYITLARI

## BÖLÜM I — NAVİGASYON GÜNCELLEMESİ

---

## UYGULAMA SIRASI (15 adım)

1. DB şema değişiklikleri — Bölüm A (InfrastructureSeed)
2. AiSkillRecord model — B.2
3. SkillPersistenceService — B.1
4. SkillRegistry Disable metodu — B.3
5. Program.cs skill init bloğu — B.4
6. LearningSignalService — C.1
7. AiMemoryService.GetApprovedPatternsAsync güncelle — D.3
8. SemanticDiscoveryService — D.1
9. SemanticService.SearchAsync güncelle — D.2
10. AiOrchestrationService constructor güncelle — E.1
11. AiQueryLog kaydetme — E.2
12. PromptEngine DB override — G
13. DI kayıtları — H
14. Admin sayfaları — F.1, F.2
15. NavMenu güncelle — I

---

## TAM ÖĞRENME DÖNGÜSÜ

```
Kullanıcı soru sorar
    ↓
NlToSql çalışır → AiQueryLog kaydedilir
    ↓
Çıktı gösterilir
    ↓ ← SemanticDiscovery: yeni terim keşfi
    ├── Thumbs up  → +3.0 puan
    ├── Kullandı (sessiz) → +1.0 puan
    ├── SQL çalıştırdı değiştirmedi → +1.5 puan
    ├── SQL düzeltti → -2.0 + correction pattern
    └── Thumbs down → -5.0 puan
         ↓
    Pattern güven puanı yeniden hesaplanır
         ↓
    ≥ 8.0 → OTO ONAY → few-shot örnek
    4.0-7.9 → Admin bekle
    < 4.0 → işlem yok
         ↓
    30 günde bir decay → kullanılmayan kaybolur
```

**Detaylı implementasyon kodları için kaynak belge:** kullanıcıdan alınan orijinal doküman (bu dosyanın tam versiyonu).
