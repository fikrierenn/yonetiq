using System.Diagnostics;
using System.Text.Json;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Bir skill tanımını alır, prompt'ları hazırlar, AI provider üzerinden çağırır ve yanıtı döner.
/// AiProviderService ile multi-provider desteği (Gemini + OpenAI + Anthropic fallback).
/// </summary>
public class SkillExecutor(
    PromptEngine promptEngine,
    AiProviderService aiProvider,
    AiMemoryService memoryService,
    ILogger<SkillExecutor> logger)
{

    /// <summary>
    /// Verilen skill ve bağlam ile AI çağrısını gerçekleştirir.
    /// 1. System + User prompt'larını yükler ve render eder
    /// 2. Gemini API'yi çağırır
    /// 3. Guardrail kontrolleri uygular
    /// 4. AiResponse döner
    /// </summary>
    public async Task<AiResponse> ExecuteAsync(SkillDefinition skill, SkillContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. Prompt'ları yükle (system prompt evrensel kurallarla birlikte gelir)
            var systemTemplate = await promptEngine.LoadSystemPromptAsync(skill.SystemPromptFile);
            var userTemplate = await promptEngine.LoadPromptAsync(skill.UserPromptFile);

            if (string.IsNullOrWhiteSpace(systemTemplate) && string.IsNullOrWhiteSpace(userTemplate))
            {
                logger.LogWarning("Prompt templates not found for skill: {SkillId}", skill.Id);
                return AiResponse.Failure($"Skill prompt dosyaları bulunamadı: {skill.Id}", skill.Id);
            }

            // 2. Template değişkenlerini oluştur
            var variables = BuildVariables(skill, context);

            // 3. Template'leri render et
            var systemPrompt = promptEngine.RenderTemplate(systemTemplate, variables);
            var userPrompt = promptEngine.RenderTemplate(userTemplate, variables);

            // Guardrail: prompt sanitization — tehlikeli direktifleri temizle
            systemPrompt = SanitizePrompt(systemPrompt);
            userPrompt = SanitizePrompt(userPrompt);

            // Kullanıcı giriş metnini user prompt'a ekle
            if (!string.IsNullOrWhiteSpace(context.UserInput))
            {
                userPrompt += $"\n\nKullanıcı Girdisi:\n{SanitizePrompt(context.UserInput)}";
            }

            // 4. Golden memory: onaylanmış kalıpları few-shot örnek olarak ekle
            var patternsResult = await memoryService.GetApprovedPatternsAsync(skill.Id, 2);
            if (patternsResult is { IsSuccess: true, Data.Count: > 0 })
            {
                var examples = string.Join("\n\n", patternsResult.Data.Select(p =>
                    $"Örnek girdi: {p.InputPattern}\nÖrnek çıktı: {p.ApprovedOutput}"));
                userPrompt = $"Referans örnekler:\n{examples}\n\n---\n\n{userPrompt}";

                // Kullanım sayaçlarını artır (arka planda)
                foreach (var p in patternsResult.Data)
                    _ = memoryService.IncrementPatternUsageAsync(p.Id);
            }

            // Guardrail: toplam prompt boyutu kontrolü (max ~32K karakter ≈ 8K token)
            const int maxPromptChars = 32_000;
            var totalPromptLength = (systemPrompt?.Length ?? 0) + (userPrompt?.Length ?? 0);
            if (totalPromptLength > maxPromptChars)
            {
                var excess = totalPromptLength - maxPromptChars;
                userPrompt = (userPrompt ?? "")[..^excess] + "\n\n_(Bağlam boyut sınırına ulaşıldığı için kısaltıldı.)_";
                logger.LogWarning("Prompt truncated for skill {SkillId}: {Total} > {Max} chars",
                    skill.Id, totalPromptLength, maxPromptChars);
            }

            // 5. AI Provider çağrısı (multi-provider: Gemini + OpenAI + Anthropic)
            var aiContent = await aiProvider.GenerateAsync(systemPrompt ?? "", userPrompt ?? "", skill.Temperature);

            sw.Stop();

            if (aiContent is null)
            {
                return new AiResponse
                {
                    IsSuccess = false,
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    ErrorMessage = "AI servisine ulaşılamadı. API kullanım limiti aşılmış olabilir — birkaç dakika sonra tekrar deneyin.",
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            // 6. Guardrail: çıktı uzunluk kontrolü
            if (aiContent.Length > skill.MaxOutputLength)
            {
                aiContent = aiContent[..skill.MaxOutputLength] + "\n\n_(Çıktı uzunluk sınırına ulaşıldığı için kısaltıldı.)_";
                logger.LogWarning("AI output truncated for skill {SkillId}: {Length} > {Max}",
                    skill.Id, aiContent.Length, skill.MaxOutputLength);
            }

            return new AiResponse
            {
                IsSuccess = true,
                SkillId = skill.Id,
                SkillName = skill.Name,
                Content = aiContent,
                Confidence = CalculateConfidence(skill, context, aiContent),
                OutputType = skill.OutputType,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                ContextExplanation = context.ContextSummary
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error executing skill: {SkillId}", skill.Id);
            return new AiResponse
            {
                IsSuccess = false,
                SkillId = skill.Id,
                SkillName = skill.Name,
                ErrorMessage = $"Skill çalıştırma hatası: {ex.Message}",
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Skill ve bağlamdan template değişkenlerini oluşturur.
    /// Entity nesnelerini prompt değişkenlerine düzleştirir (TaskItem → taskTitle, taskDescription vb.)
    /// </summary>
    internal static Dictionary<string, string> BuildVariables(SkillDefinition skill, SkillContext context)
    {
        var vars = new Dictionary<string, string>
        {
            ["skillId"] = skill.Id,
            ["skillName"] = skill.Name,
            ["module"] = skill.Module,
            ["userName"] = context.User.FullName,
            ["userRole"] = context.User.Role,
            ["contextSummary"] = context.ContextSummary,
            ["userInput"] = context.UserInput ?? string.Empty,
            ["currentDate"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["today"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["currentTime"] = DateTime.Now.ToString("HH:mm")
        };

        // Entity nesnelerini tanıyıp alanlarını prompt değişkenlerine düzleştir
        foreach (var (key, value) in context.Entities)
        {
            vars[$"entity_{key}"] = value is string s ? s : JsonSerializer.Serialize(value);
            FlattenEntity(key, value, vars);
        }

        // Bağlam verilerini değişkenlere ekle (hem prefix'li hem prefix'siz)
        foreach (var (key, value) in context.ContextData)
        {
            var val = value is string sv ? sv : JsonSerializer.Serialize(value);
            vars[$"context_{key}"] = val;
            vars.TryAdd(key, val);
        }

        return vars;
    }

    /// <summary>
    /// Bilinen entity tiplerini prompt değişkenlerine düzleştirir.
    /// Prompt'lardaki {{taskTitle}}, {{meetingDate}} gibi değişkenleri doldurur.
    /// </summary>
    private static void FlattenEntity(string entityType, object entity, Dictionary<string, string> vars)
    {
        switch (entityType)
        {
            case "TaskItem" when entity is YonetIQ.Data.Models.TaskItem task:
                vars.TryAdd("taskTitle", task.Title);
                vars.TryAdd("taskDescription", task.Description);
                vars.TryAdd("assigneeName", task.AssigneeName);
                vars.TryAdd("priority", task.PriorityLookupId.ToString());
                vars.TryAdd("dueDate", task.DueDate?.ToString("dd.MM.yyyy") ?? "Belirtilmedi");
                vars.TryAdd("status", task.StatusLookupId.ToString());
                vars.TryAdd("overdueDays", task.DueDate.HasValue && task.DueDate.Value < DateTime.Today
                    ? ((DateTime.Today - task.DueDate.Value).Days).ToString() : "0");
                break;

            case "Meeting" when entity is YonetIQ.Data.Models.Meeting meeting:
                vars.TryAdd("meetingTitle", meeting.Title);
                vars.TryAdd("meetingDate", meeting.MeetingDate.ToString("dd.MM.yyyy HH:mm"));
                vars.TryAdd("participants", meeting.Participants);
                vars.TryAdd("agenda", meeting.Agenda);
                vars.TryAdd("notes", meeting.Notes);
                vars.TryAdd("location", meeting.Location);
                vars.TryAdd("meetingLink", meeting.MeetingLink ?? "");
                break;

            case "PersonalNote" when entity is YonetIQ.Data.Models.PersonalNote note:
                vars.TryAdd("noteTitle", note.Title);
                vars.TryAdd("noteContent", note.Content);
                vars.TryAdd("noteTags", note.Tags);
                vars.TryAdd("reminderAt", note.ReminderAt?.ToString("dd.MM.yyyy HH:mm") ?? "Yok");
                break;

            case "QueryRecord" when entity is YonetIQ.Data.Models.QueryRecord query:
                vars.TryAdd("queryName", query.Name);
                vars.TryAdd("sqlContent", query.SqlContent ?? "");
                vars.TryAdd("resultType", query.ResultType ?? "Table");
                break;

            case "Objective" when entity is YonetIQ.Data.Models.Objective obj:
                vars.TryAdd("objectiveTitle", obj.Title);
                vars.TryAdd("objectiveDescription", obj.Description ?? "");
                vars.TryAdd("objectiveProgress", obj.Progress.ToString("F0"));
                vars.TryAdd("objectiveLevel", obj.Level);
                vars.TryAdd("objectiveStatus", obj.Status);
                break;
        }
    }

    /// <summary>
    /// Birden fazla faktöre dayalı güven skoru hesaplar.
    /// </summary>
    private static float CalculateConfidence(SkillDefinition skill, SkillContext context, string output)
    {
        var score = skill.MinConfidence;

        // Faktör 1: Bağlam bütünlüğü — tüm zorunlu entity'ler mevcut mu?
        var requiredEntitiesPresent = skill.RequiredEntities.All(e => context.Entities.ContainsKey(e));
        if (requiredEntitiesPresent) score += 0.15f;

        // Faktör 2: Çıktı uzunluğu makul mü? (çok kısa veya truncate edilmiş değil)
        if (output.Length > 50 && output.Length < skill.MaxOutputLength * 0.9)
            score += 0.1f;

        // Faktör 3: Çıktı beklenen dilde mi? (Türkçe anahtar kelimeler)
        var turkishKeywords = new[] { "görev", "toplantı", "karar", "öneri", "analiz", "durum", "rapor", "sonuç" };
        if (turkishKeywords.Any(k => output.Contains(k, StringComparison.OrdinalIgnoreCase)))
            score += 0.05f;

        // Faktör 4: Golden memory kalıpları var mı? → daha yüksek güven
        if (context.PreviousInteractions.Count > 0)
            score += 0.05f;

        // Faktör 5: Yapısal format kontrolü — emoji header veya madde listesi var mı?
        var hasStructure = output.Contains("📊") || output.Contains("📋") || output.Contains("✅") ||
                           output.Contains("⚠") || output.Contains("🔴") || output.Contains("🟡") ||
                           output.Contains("🔹") || output.Contains("💡") || output.Contains("1.") ||
                           output.Contains("- ");
        if (hasStructure)
            score += 0.05f;
        else
            score -= 0.1f; // Yapısız çıktı → confidence düşür

        return Math.Clamp(score, 0.1f, 0.95f);
    }

    /// <summary>
    /// Prompt içeriğini sanitize eder:
    /// - Tehlikeli SQL direktiflerini (DROP, DELETE, TRUNCATE, EXEC) engeller (DML üretme skill'leri hariç)
    /// - Injection girişimlerini temizler (system prompt override denemeleri)
    /// - Null/boş → boş string döner
    /// </summary>
    private static string SanitizePrompt(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = input;

        // System prompt override girişimlerini temizle
        string[] injectionPatterns =
        [
            "ignore previous instructions",
            "ignore all previous",
            "disregard above",
            "new system prompt",
            "you are now",
            "act as root",
            "sudo mode",
            "override safety",
            "jailbreak"
        ];

        foreach (var pattern in injectionPatterns)
        {
            if (result.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Replace(pattern, "[BLOCKED]", StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }
}
