# YonetIQ — Unified Master Implementation Plan

**Tarih:** 25.03.2026 | **Versiyon:** 3.0 — Tüm fazlar birleştirildi
**Önceki belgeler:** MASTER_PLAN.md + LEARNING_SYSTEM.md → bu belgede birleştirildi

---

## İÇİNDEKİLER

- Bölüm 0: Git + Secrets (TAMAMLANDI)
- Bölüm 1: Kritik Borçlar (7 madde)
- Bölüm 2: Veritabanı Şeması (9 yeni tablo/genişletme)
- Bölüm 3: Yeni Servisler (7 servis)
- Bölüm 4: Meta-Skill Sistemi (AI kendi kendini geliştirme)
- Bölüm 5: Mevcut Servis Güncellemeleri (6 güncelleme)
- Bölüm 6: BKM Kitap Domain Zekası (3 skill + 20 semantic seed)
- Bölüm 7: DI Kayıtları
- Bölüm 8: Navigasyon
- Bölüm 9: Prompt Dosyaları Envanteri
- Bölüm 10: Tam Uygulama Sırası (49 adım, 7 faz)
- Bölüm 11: Sistem Nasıl Çalışacak (end-to-end akış)

---

## UYGULAMA SIRASI (49 adım, 7 faz)

### Faz 1 — Temel (1-8)
1. ~~git init + .gitignore + env var~~ ✅ TAMAMLANDI
2. Build uyarıları → clean build
3. BaseEntity.UpdatedAt
4. Schema detection cache
5. Session null safety
6. NotificationService memory leak
7. Sayfa yetki kontrolü
8. PromptEngine hot-reload

### Faz 2 — Veritabanı (9-17)
9. AiSkillDefinitions tablosu
10. AiPatternSignals tablosu
11. AiPatterns ek kolonlar
12. SemanticLearningCandidates
13. AiQueryLog
14. AiConversationContext
15. AiSkillTriggerLog
16. UserDashboardPreferences
17. Decision ek kolonlar

### Faz 3 — Yeni Servisler (18-26)
18. AiSkillRecord model
19. SkillPersistenceService
20. SkillRegistry.Disable + Count
21. LearningSignalService
22. SemanticEnricher
23. ConversationContextService
24. SkillTriggerEngine
25. SemanticDiscoveryService
26. ContinuousLearningService

### Faz 4 — Meta-Skill Sistemi (27-31)
27. MetaSkills.cs (3 skill: SkillAnalyzer, SkillWriter, PromptRefiner)
28. Meta prompt dosyaları (6 dosya)
29. MetaSkillService
30. SkillStudio.razor
31. SkillManagement.razor

### Faz 5 — Servis Güncellemeleri (32-38)
32. AiOrchestrationService (ConversationContext + LearningSignal + SemanticDiscovery)
33. ContextBuilderService (SemanticEnricher + conversation_history)
34. PromptEngine DB override
35. AiFeedbackWidget tüm sinyaller
36. QueryEditor SQL sinyal entegrasyonu
37. AiMemoryService.GetApprovedPatterns (ConfidenceScore sıralama)
38. Program.cs skill init (SkillPersistenceService.InitializeAsync)

### Faz 6 — BKM Domain + UI (39-44)
39. BookSkills.cs (StockAlert, TrendRadar, CampaignRoi)
40. BKM prompt dosyaları (6 dosya)
41. SemanticDefinitions seed (20 terim)
42. ProactiveInsight BKM ekleri
43. SemanticAdmin.razor
44. NavMenu güncelle

### Faz 7 — AI Bağlantıları (45-49)
45. SuggestedAction butonları (SkillResultCard)
46. Toplantı hazırlık otomatı (MeetingDetail 4 saat kuralı)
47. Görev sonrası AI (TaskForm risk bildirimi)
48. Not AI analizi (etiket + aksiyon)
49. Anomali bildirim zinciri (ScheduledReportWorker)

---

## ÖĞRENME SİSTEMİ SİNYAL TABLOSU

| Sinyal | Ağırlık | Tip |
|--------|---------|-----|
| Thumbs up | +3.0 | Explicit |
| Implicit accept | +1.0 | Otomatik |
| SQL accepted (değiştirmedi) | +1.5 | Otomatik |
| No re-query (48h) | +0.5 | Zaman bazlı |
| Thumbs down | -5.0 | Explicit |
| SQL correction | -2.0 | Otomatik + öğren |
| Decay (30 gün unused) | -0.3 | Otomatik |

**Eşikler:** ≥8.0 oto-onay | 4.0-7.9 admin onay | <4.0 gürültü

---

## AGENT EŞLEŞTİRME

| Faz | Agent | Dosya |
|-----|-------|-------|
| 1 | debt-fixer | .claude/agents/debt-fixer.md |
| 2 | learning-architect | .claude/agents/learning-architect.md |
| 3 | learning-architect | .claude/agents/learning-architect.md |
| 4 | ai-skill-developer | .claude/agents/ai-skill-developer.md |
| 5 | ai-gap-closer | .claude/agents/ai-gap-closer.md |
| 6 | domain-expert | .claude/agents/domain-expert.md |
| 7 | ai-gap-closer | .claude/agents/ai-gap-closer.md |

---

**Detaylı implementasyon kodları:** Kullanıcının verdiği orijinal belge (mesaj içinde tam kod snippetleri mevcut).
**Önceki belgeler:** MASTER_PLAN.md + LEARNING_SYSTEM.md bu belge tarafından kapsanıyor.
