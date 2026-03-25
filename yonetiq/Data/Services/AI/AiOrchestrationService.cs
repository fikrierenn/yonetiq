using System.Diagnostics;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Tüm AI isteklerinin ana giriş noktası.
/// İstekleri skill'lere yönlendirir, bağlam toplar, çalıştırır ve etkileşimi kaydeder.
/// </summary>
public class AiOrchestrationService(
    SkillRegistry skillRegistry,
    ContextBuilderService contextBuilder,
    SkillExecutor skillExecutor,
    AiMemoryService memoryService,
    ConversationContextService conversationSvc,
    SemanticDiscoveryService semanticDiscoverySvc,
    ILogger<AiOrchestrationService> logger)
{
    /// <summary>
    /// Gelen AI isteğini işler:
    /// 1. Skill çözümleme (doğrudan ID veya otomatik yönlendirme)
    /// 2. Bağlam toplama
    /// 3. Skill çalıştırma
    /// 4. Etkileşim kaydı
    /// </summary>
    public async Task<AiResponse> ProcessRequestAsync(AiRequest request)
    {
        var sw = Stopwatch.StartNew();

        // 0. Auth: kullanıcı doğrulaması
        if (request.UserId <= 0)
        {
            logger.LogWarning("AI request rejected: invalid UserId={UserId}", request.UserId);
            return AiResponse.Failure("Geçersiz kullanıcı. Lütfen giriş yapın.", "AuthError");
        }

        // 1. Skill çözümleme
        SkillDefinition? skill = null;

        if (!string.IsNullOrWhiteSpace(request.SkillId))
        {
            skill = skillRegistry.Resolve(request.SkillId);
            if (skill is null)
            {
                logger.LogWarning("Skill not found: {SkillId}", request.SkillId);
                return AiResponse.Failure($"Skill bulunamadı: {request.SkillId}", request.SkillId);
            }
        }
        else
        {
            // Otomatik yönlendirme
            skill = RouteToSkill(request);
            if (skill is null)
            {
                logger.LogInformation("No matching skill found for input: {Input}", request.UserInput);
                return AiResponse.Failure(
                    "Girdinize uygun bir AI yeteneği bulunamadı. Lütfen daha belirgin bir ifade kullanın veya bir skill seçin.");
            }
        }

        logger.LogInformation("Processing AI request: Skill={SkillId}, Module={Module}, UserId={UserId}",
            skill.Id, request.Module, request.UserId);

        // 2. Bağlam toplama
        var context = await contextBuilder.BuildContextAsync(skill, request);

        // Token sınırı kontrolü
        if (context.EstimatedTokenCount > skill.MaxTokenInput)
        {
            logger.LogWarning("Context too large for skill {SkillId}: {Tokens} > {Max}",
                skill.Id, context.EstimatedTokenCount, skill.MaxTokenInput);
            // Devam et ama uyar — Gemini kendi sınırını uygulayacaktır
        }

        // Önceki etkileşimleri ekle
        var recentResult = await memoryService.GetRecentInteractionsAsync(request.UserId, skill.Id, 3);
        if (recentResult is { IsSuccess: true, Data: not null })
        {
            context.PreviousInteractions = recentResult.Data;
        }

        // 3. Skill çalıştırma
        var response = await skillExecutor.ExecuteAsync(skill, context);

        sw.Stop();
        response.LatencyMs = (int)sw.ElapsedMilliseconds;

        // 4. Etkileşim kaydı
        var interaction = new AiInteraction
        {
            UserId = request.UserId,
            SkillId = skill.Id,
            Module = request.Module,
            InputSummary = TruncateForSummary(request.UserInput ?? skill.Name, 200),
            OutputSummary = TruncateForSummary(response.Content, 500),
            ConfidenceScore = (decimal)response.Confidence,
            LatencyMs = response.LatencyMs,
            TokenCount = context.EstimatedTokenCount,
            IsSuccess = response.IsSuccess,
            CreatedAt = DateTime.UtcNow
        };

        var saveResult = await memoryService.SaveInteractionAsync(interaction);
        if (saveResult is { IsSuccess: true, Data: var interactionId })
        {
            response.InteractionId = interactionId;
        }

        // WP5: Sohbet bağlamını kaydet
        var sessionKey = request.Parameters?.GetValueOrDefault("sessionKey")?.ToString()
            ?? $"user_{request.UserId}";
        _ = conversationSvc.SaveTurnAsync(
            sessionKey, request.UserId,
            request.UserInput ?? skill.Name,
            response.Content, skill.Id);

        // WP5: Yeni semantik terim keşfi (arka plan, hata yutulur)
        if (!string.IsNullOrWhiteSpace(request.UserInput))
        {
            _ = semanticDiscoverySvc.DiscoverFromInputAsync(request.UserInput, skill.Id);
        }

        return response;
    }

    /// <summary>
    /// Belirli bir modül için kullanılabilir skill'leri döner (sadece reaktif olanlar).
    /// </summary>
    public List<SkillDefinition> GetAvailableSkills(string module)
    {
        // Tüm skill'leri göster (Reactive + Proactive + Hybrid) — sidebar'dan tetiklenebilir
        return skillRegistry.ResolveByModule(module);
    }

    /// <summary>
    /// Kullanıcı geri bildirimini kaydeder.
    /// </summary>
    public async Task SubmitFeedbackAsync(AiFeedback feedback)
    {
        await memoryService.SaveFeedbackAsync(feedback);
    }

    /// <summary>
    /// Basit keyword eşleştirmesi ile skill yönlendirmesi yapar (Faz 1).
    /// Kullanıcının serbest metin girdisini skill tanımlarıyla eşleştirir.
    /// </summary>
    private SkillDefinition? RouteToSkill(AiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
            return null;

        var input = request.UserInput.ToLowerInvariant();

        // "global" modülünden geliyorsa tüm skill'leri ara
        var candidates = request.Module == "global"
            ? skillRegistry.ListAll().ToList()
            : skillRegistry.ResolveByModule(request.Module);

        // Modül bazlı arama sonuç vermezse tüm skill'lere düş
        if (candidates.Count == 0)
            candidates = skillRegistry.ListAll().ToList();

        if (candidates.Count == 0)
            return null;

        // Keyword eşleştirme: skill Name ve Description'daki kelimeleri input ile karşılaştır
        SkillDefinition? bestMatch = null;
        var bestScore = 0;

        foreach (var skill in candidates)
        {
            var score = 0;
            var keywords = GetKeywords(skill);

            foreach (var keyword in keywords)
            {
                if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    score++;
            }

            // Skill ID'sindeki modül kısmı da eşleşirse bonus puan
            if (!string.IsNullOrWhiteSpace(skill.Module) &&
                input.Contains(skill.Module, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = skill;
            }
        }

        // Minimum 1 keyword eşleşmesi gerekir
        return bestScore > 0 ? bestMatch : null;
    }

    /// <summary>
    /// Skill tanımından arama anahtar kelimelerini çıkarır.
    /// Türkçe eşanlamlılar ve yaygın ifadeler dahil edilir.
    /// </summary>
    private static List<string> GetKeywords(SkillDefinition skill)
    {
        var keywords = new List<string>();

        // Skill adından kelimeler
        foreach (var word in skill.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length > 2)
                keywords.Add(word.ToLowerInvariant());
        }

        // Açıklamadan kelimeler
        if (!string.IsNullOrWhiteSpace(skill.Description))
        {
            foreach (var word in skill.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length > 3)
                    keywords.Add(word.ToLowerInvariant());
            }
        }

        // Skill ID'sinden kelimeler
        foreach (var part in skill.Id.Split('.'))
        {
            if (part.Length > 2)
                keywords.Add(part.ToLowerInvariant());
        }

        // Skill bazlı Türkçe eşanlamlılar
        var turkishKeywords = skill.Id switch
        {
            "exec.daily_briefing" => new[] { "bugün", "öncelik", "özet", "briefing", "günlük", "ne yapmalı", "odak", "durum" },
            "exec.change_detection" => new[] { "değişiklik", "değişim", "son", "neler oldu", "güncelleme", "yeni" },
            "task.clarify" => new[] { "görev", "netleştir", "açıkla", "tanım", "kriter" },
            "task.risk" => new[] { "görev", "risk", "gecik", "tıkan", "sorun", "problem" },
            "meeting.summarize" => new[] { "toplantı", "özet", "özetle", "not", "toplantıyı" },
            "meeting.extract_actions" => new[] { "toplantı", "aksiyon", "karar", "görev çıkar", "yapılacak" },
            "note.structure" => new[] { "not", "yapılandır", "düzenle", "organize", "madde" },
            "note.suggest_tags" => new[] { "not", "etiket", "tag", "öneri", "kategori" },
            "query.explain" => new[] { "sorgu", "sonuç", "açıkla", "rapor", "analiz", "yorum" },
            "query.nl_to_sql" => new[] { "sql", "sorgu", "yaz", "oluştur", "veri", "tablo" },
            "query.exec_summary" => new[] { "yönetici", "özet", "rapor", "executive", "summary" },
            "approval.summarize" => new[] { "onay", "bekleyen", "özet", "onaylar" },
            "approval.risk" => new[] { "onay", "risk", "sıra dışı", "anomali" },
            "okr.progress" => new[] { "hedef", "okr", "ilerleme", "progress", "durum" },
            "okr.action" => new[] { "hedef", "okr", "aksiyon", "öneri", "geride" },
            _ => Array.Empty<string>()
        };
        keywords.AddRange(turkishKeywords);

        return keywords.Distinct().ToList();
    }

    private static string TruncateForSummary(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
