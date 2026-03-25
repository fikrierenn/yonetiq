using System.Text;
using System.Text.Json;
using YonetIQ.Data.Models;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Skill çalıştırılmadan önce gerekli bağlam verisini toplar.
/// İlgili servisleri çağırarak SkillContext nesnesini oluşturur.
/// </summary>
public class ContextBuilderService(
    TaskService taskSvc,
    MeetingService meetingSvc,
    NoteService noteSvc,
    QueryService querySvc,
    QuerySetService querySetSvc,
    UserService userSvc,
    SessionService sessionSvc,
    LookupService lookupSvc,
    ApprovalService approvalSvc,
    OkrService okrSvc,
    KpiTargetService kpiTargetSvc,
    SemanticService semanticSvc,
    ILogger<ContextBuilderService> logger)
{
    // İleride kullanılacak servisler — CS9113 suppress
    private readonly QuerySetService _querySetSvc = querySetSvc;
    private readonly LookupService _lookupSvc = lookupSvc;
    /// <summary>
    /// Skill tanımı ve istek parametrelerine göre bağlam verisini toplar.
    /// Her skill'in RequiredEntities ve Module bilgisine göre ilgili servislerden veri çeker.
    /// </summary>
    public async Task<SkillContext> BuildContextAsync(SkillDefinition skill, AiRequest request)
    {
        var activeUser = sessionSvc.ActiveUser;
        var context = new SkillContext
        {
            User = activeUser,
            UserInput = request.UserInput,
            ContextSummary = string.Empty
        };

        // Null safety: oturum yoksa boş context dön
        if (activeUser is null || !sessionSvc.IsLoggedIn)
        {
            logger.LogWarning("ContextBuilder: ActiveUser null veya login değil");
            return context;
        }

        var summaryParts = new List<string>();

        try
        {
            // Modül bazlı bağlam toplama
            switch (skill.Module.ToLowerInvariant())
            {
                case "task":
                    await BuildTaskContextAsync(request, context, summaryParts);
                    break;
                case "meeting":
                    await BuildMeetingContextAsync(request, context, summaryParts);
                    break;
                case "dashboard":
                case "executive":
                    await BuildDashboardContextAsync(request, context, summaryParts);
                    break;
                case "note":
                    await BuildNoteContextAsync(request, context, summaryParts);
                    break;
                case "query":
                    await BuildQueryContextAsync(request, context, summaryParts);
                    break;
                case "approval":
                    await BuildApprovalContextAsync(request, context, summaryParts);
                    break;
                case "okr":
                    await BuildOkrContextAsync(request, context, summaryParts);
                    break;
            }

            // RequiredEntities'ten ek veri toplama
            foreach (var entity in skill.RequiredEntities)
            {
                if (context.Entities.ContainsKey(entity))
                    continue; // Zaten modül bazlı adımda toplandı

                await LoadEntityAsync(entity, request, context, summaryParts);
            }

            // Bağlam özetini oluştur (explainability)
            context.ContextSummary = summaryParts.Count > 0
                ? string.Join("; ", summaryParts)
                : $"Modül: {skill.Module}, Kullanıcı: {context.User.FullName}";

            // Permission-aware: rol bazlı bağlam filtresi
            ApplyPermissionFilter(context);

            // Tahmini token sayısını hesapla (basit: karakter/4)
            var totalChars = context.Entities.Values
                .Sum(v => (v is string s ? s : JsonSerializer.Serialize(v)).Length);
            totalChars += context.ContextData.Values
                .Sum(v => (v is string s ? s : JsonSerializer.Serialize(v)).Length);
            context.EstimatedTokenCount = totalChars / 4;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building context for skill: {SkillId}", skill.Id);
            context.ContextSummary = $"Bağlam toplama sırasında hata: {ex.Message}";
        }

        return context;
    }

    private async Task BuildTaskContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        // Belirli bir görev ID'si verilmişse o görevi yükle
        if (request.Parameters.TryGetValue("taskId", out var taskIdObj) &&
            int.TryParse(taskIdObj.ToString(), out var taskId))
        {
            var taskResult = await taskSvc.GetAsync(taskId);
            if (taskResult is { IsSuccess: true, Data: not null })
            {
                context.Entities["TaskItem"] = taskResult.Data;
                summary.Add($"Görev: {taskResult.Data.Title}");
            }
        }

        // Kullanıcının bekleyen görevlerini bağlama ekle
        var pendingResult = await taskSvc.ListPendingTasksAsync(5);
        if (pendingResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["pendingTasks"] = pendingResult.Data;
            summary.Add($"Bekleyen görev sayısı: {pendingResult.Data.Count}");
        }

        // Atanan kişi bilgilerini ekle
        var assigneesResult = await taskSvc.GetAssigneesAsync();
        if (assigneesResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["assignees"] = assigneesResult.Data;
        }

        // Geciken görev sayısı ve detayları (bottleneck, followup, prioritize skill'leri için)
        var overdueCountResult = await taskSvc.GetOverdueTaskCountAsync();
        if (overdueCountResult is { IsSuccess: true, Data: var overdueCount })
        {
            context.ContextData["overdueTasks"] = overdueCount.ToString();
            summary.Add($"Geciken görev: {overdueCount}");
        }

        // Tüm görevlerin listesi (prioritize, bottleneck skill'leri için)
        var allTasksResult = await taskSvc.ListAsync();
        if (allTasksResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["totalTasks"] = allTasksResult.Data.Count.ToString();

            // Kişi bazlı iş yükü özeti (bottleneck skill'i için)
            var workload = allTasksResult.Data
                .Where(t => !string.IsNullOrEmpty(t.AssigneeName))
                .GroupBy(t => t.AssigneeName)
                .Select(g =>
                {
                    var overdue = g.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Today);
                    return $"{g.Key}: {g.Count()} görev ({overdue} geciken)";
                })
                .ToList();
            context.ContextData["assigneeWorkload"] = string.Join("\n", workload);
        }
    }

    private async Task BuildMeetingContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        // Belirli bir toplantı ID'si verilmişse o toplantıyı yükle
        if (request.Parameters.TryGetValue("meetingId", out var meetingIdObj) &&
            int.TryParse(meetingIdObj.ToString(), out var meetingId))
        {
            var meetingResult = await meetingSvc.GetByIdAsync(meetingId);
            if (meetingResult is { IsSuccess: true, Data: not null })
            {
                context.Entities["Meeting"] = meetingResult.Data;
                summary.Add($"Toplantı: {meetingResult.Data.Title}");

                // Toplantı kararlarını yükle
                var decisionsResult = await meetingSvc.ListDecisionsAsync(meetingId);
                if (decisionsResult is { IsSuccess: true, Data: not null })
                {
                    context.ContextData["decisions"] = decisionsResult.Data;
                    summary.Add($"Karar sayısı: {decisionsResult.Data.Count}");
                }

                // Katılımcı bilgisi (extract_decisions, preparation skill'leri için)
                if (!string.IsNullOrWhiteSpace(meetingResult.Data.Participants))
                {
                    context.ContextData["participants"] = meetingResult.Data.Participants;
                }
            }
        }
    }

    private async Task BuildDashboardContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        int pendingCount = 0;

        // Bekleyen görev sayısı
        var pendingCountResult = await taskSvc.GetPendingTaskCountAsync();
        if (pendingCountResult is { IsSuccess: true, Data: var pc })
        {
            pendingCount = pc;
            context.ContextData["pendingTaskCount"] = pc;
            context.ContextData["totalActiveTasks"] = pc.ToString();
            summary.Add($"Bekleyen görevler: {pc}");
        }

        // Tamamlanma oranı
        var actionRateResult = await taskSvc.GetActionRateAsync();
        if (actionRateResult is { IsSuccess: true, Data: var actionRate })
        {
            context.ContextData["actionRate"] = actionRate;
            summary.Add($"Aksiyon oranı: %{actionRate}");
        }

        // Geciken görev sayısı
        var overdueResult = await taskSvc.GetOverdueTaskCountAsync();
        context.ContextData["overdueTasks"] = overdueResult is { IsSuccess: true }
            ? overdueResult.Data.ToString()
            : "0";
        if (overdueResult is { IsSuccess: true, Data: > 0 })
            summary.Add($"Geciken görevler: {overdueResult.Data}");

        // Son 24 saatte tamamlanan
        var completedResult = await taskSvc.GetCompletedLast24hCountAsync();
        context.ContextData["completedYesterday"] = completedResult is { IsSuccess: true }
            ? completedResult.Data.ToString()
            : "0";

        // Bugün son tarihli görevler
        var dueTodayResult = await taskSvc.GetDueTodayCountAsync();
        context.ContextData["dueTodayTasks"] = dueTodayResult is { IsSuccess: true }
            ? dueTodayResult.Data.ToString()
            : "0";

        // Bekleyen görevler (özet)
        var pendingTasksResult = await taskSvc.ListPendingTasksAsync(5);
        if (pendingTasksResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["pendingTasks"] = pendingTasksResult.Data;
        }

        // Bekleyen karar sayısı
        var pendingDecisionsResult = await meetingSvc.GetPendingDecisionCountAsync();
        if (pendingDecisionsResult is { IsSuccess: true, Data: var pdc })
        {
            context.ContextData["pendingDecisions"] = pdc.ToString();
            summary.Add($"Bekleyen kararlar: {pdc}");
        }

        // Bugünkü toplantılar
        var todayMeetingsResult = await meetingSvc.GetTodayMeetingsAsync();
        if (todayMeetingsResult is { IsSuccess: true, Data: not null } && todayMeetingsResult.Data.Count > 0)
        {
            var lines = todayMeetingsResult.Data
                .Select(m => $"{m.MeetingDate:HH:mm} - {m.Title}");
            context.ContextData["todayMeetings"] = string.Join("\n", lines);
            summary.Add($"Bugünkü toplantı: {todayMeetingsResult.Data.Count}");
        }
        else
        {
            context.ContextData["todayMeetings"] = "Bugün planlanmış toplantı yok.";
        }

        // Bekleyen onay sayısı
        var approvalResult = await approvalSvc.GetPendingApprovalCountAsync(request.UserId);
        context.ContextData["pendingApprovals"] = approvalResult is { IsSuccess: true }
            ? approvalResult.Data.ToString()
            : "0";
        if (approvalResult is { IsSuccess: true, Data: > 0 })
            summary.Add($"Bekleyen onaylar: {approvalResult.Data}");

        // KPI verileri (kpi_analysis skill'i için)
        var kpiResult = await kpiTargetSvc.ListAsync();
        if (kpiResult is { IsSuccess: true, Data: not null } && kpiResult.Data.Count > 0)
        {
            var kpiLines = kpiResult.Data
                .Select(k => $"{k.Label}: Hedef={k.TargetValue}, Güncel={k.ActualValue?.ToString() ?? "?"}, Durum={k.Status}");
            context.ContextData["kpiData"] = string.Join("\n", kpiLines);
            summary.Add($"KPI sayısı: {kpiResult.Data.Count}");
        }

        // Fallback'ler — henüz set edilmediyse varsayılan ata
        context.ContextData.TryAdd("totalActiveTasks", pendingCount.ToString());
        context.ContextData.TryAdd("pendingDecisions", "0");
    }

    private async Task BuildNoteContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        if (request.Parameters.TryGetValue("noteId", out var noteIdObj) &&
            int.TryParse(noteIdObj.ToString(), out var noteId))
        {
            var noteResult = await noteSvc.GetAsync(noteId);
            if (noteResult is { IsSuccess: true, Data: not null })
            {
                context.Entities["PersonalNote"] = noteResult.Data;
                summary.Add($"Not: {noteResult.Data.Title}");
            }
        }
    }

    private async Task BuildQueryContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        if (request.Parameters.TryGetValue("queryId", out var queryIdObj) &&
            int.TryParse(queryIdObj.ToString(), out var queryId))
        {
            var queryResult = await querySvc.GetQueryAsync(queryId);
            if (queryResult is { IsSuccess: true, Data: not null })
            {
                context.Entities["QueryRecord"] = queryResult.Data;
                summary.Add($"Sorgu: {queryResult.Data.Name}");
            }
        }

        // Sorgu sonuç verisi (query.explain, query.anomaly, query.trends skill'leri için)
        if (request.Parameters.TryGetValue("queryResultData", out var resultDataObj) && resultDataObj is string resultData)
        {
            context.ContextData["dataSummary"] = resultData;
            context.ContextData["query_result_data"] = resultData;
        }
        else if (context.Entities.TryGetValue("QueryRecord", out var qrObj) &&
                 qrObj is Models.QueryRecord qr && !string.IsNullOrWhiteSpace(qr.SqlContent))
        {
            // Sorguyu çalıştırıp özet veri ekle (max 15 satır)
            try
            {
                var result = await querySvc.ExecuteAsync(qr.SqlContent);
                if (result is { IsSuccess: true, RowCount: > 0 })
                {
                    context.ContextData["rowCount"] = result.RowCount.ToString();
                    context.ContextData["columns"] = string.Join(", ", result.Columns);

                    var sampleRows = result.Rows.Take(15)
                        .Select(row => string.Join(" | ", result.Columns.Select(c =>
                            $"{c}={row.GetValueOrDefault(c)}")));
                    context.ContextData["dataSummary"] = string.Join("\n", sampleRows);
                    context.ContextData["query_result_data"] = context.ContextData["dataSummary"];
                    summary.Add($"Sorgu sonucu: {result.RowCount} satır");
                }
            }
            catch { /* Sorgu çalıştırma başarısız — context'siz devam */ }
        }

        // Şema bilgisini ekle
        var schemaResult = await querySvc.GetTableSchemaAsync();
        if (schemaResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["schema"] = schemaResult.Data;
            summary.Add("Veritabanı şeması eklendi");
        }

        // Semantic tanımları ekle (NL→SQL zenginleştirme)
        var semanticResult = await semanticSvc.ListAsync();
        if (semanticResult is { IsSuccess: true, Data: not null })
        {
            var semanticContext = semanticResult.Data
                .Select(s => s.TermType switch
                {
                    "table" => $"Tablo: {s.TableName} → {s.BusinessName} ({s.Description}){(s.Aliases != null ? $" [Eşanlamlılar: {s.Aliases}]" : "")}",
                    "metric" => $"Metrik: {s.BusinessName} → {s.SqlExpression} ({s.Description})",
                    "column" => $"Kolon: {s.TableName}.{s.ColumnName} → {s.BusinessName}",
                    _ => null
                })
                .Where(x => x != null);
            context.ContextData["semanticDefinitions"] = string.Join("\n", semanticContext);
            summary.Add($"Semantic tanım: {semanticResult.Data.Count}");
        }
    }

    private async Task BuildApprovalContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        var pendingResult = await approvalSvc.GetPendingForUserAsync(request.UserId);
        if (pendingResult is { IsSuccess: true, Data: not null })
        {
            context.ContextData["pendingApprovals"] = pendingResult.Data;
            summary.Add($"Bekleyen onay: {pendingResult.Data.Count}");
        }
    }

    private async Task BuildOkrContextAsync(AiRequest request, SkillContext context, List<string> summary)
    {
        var objectives = await okrSvc.ListAsync(year: DateTime.Now.Year);
        if (objectives is { IsSuccess: true, Data: not null })
        {
            context.ContextData["objectives"] = objectives.Data;
            summary.Add($"OKR hedef sayısı: {objectives.Data.Count}");
        }
    }

    /// <summary>
    /// Kullanıcı rolüne göre bağlam verisini filtreler / zenginleştirir.
    /// Genel Müdür tüm veriyi görür; diğer roller kısıtlı bağlam alır.
    /// </summary>
    private static void ApplyPermissionFilter(SkillContext context)
    {
        var role = context.User?.Role ?? "";

        // Admin (Genel Müdür) tüm veriyi görür — filtre yok
        if (role.Contains("Genel Müdür", StringComparison.OrdinalIgnoreCase))
            return;

        // Rol bilgisini bağlama ekle (AI farkındalığı için)
        context.ContextData["userRole"] = role;
        context.ContextData["permissionNote"] = role switch
        {
            var r when r.Contains("Direktör", StringComparison.OrdinalIgnoreCase)
                => "Kullanıcı direktör seviyesinde — departman verilerine erişimi var.",
            var r when r.Contains("Müdür", StringComparison.OrdinalIgnoreCase)
                => "Kullanıcı müdür seviyesinde — kendi departman verilerine erişimi var.",
            _ => "Kullanıcı standart seviyede — sadece kendi verilerine erişimi var."
        };

        context.ContextSummary += $" | Rol: {role}";
    }

    /// <summary>
    /// Tekil entity yükleme — RequiredEntities listesindeki eksik entity'ler için çağrılır.
    /// </summary>
    private async Task LoadEntityAsync(string entityType, AiRequest request, SkillContext context, List<string> summary)
    {
        switch (entityType)
        {
            case "User":
                if (request.Parameters.TryGetValue("userId", out var userIdObj) &&
                    int.TryParse(userIdObj.ToString(), out var userId))
                {
                    var userResult = await userSvc.GetByIdAsync(userId);
                    if (userResult is { IsSuccess: true, Data: not null })
                    {
                        context.Entities["User"] = userResult.Data;
                        summary.Add($"Kullanıcı: {userResult.Data.FullName}");
                    }
                }
                break;

            case "TaskItem":
                // Modül bazlı adımda yüklenmediyse tekil yükleme
                if (request.Parameters.TryGetValue("taskId", out var tIdObj) &&
                    int.TryParse(tIdObj.ToString(), out var tId))
                {
                    var taskResult = await taskSvc.GetAsync(tId);
                    if (taskResult is { IsSuccess: true, Data: not null })
                    {
                        context.Entities["TaskItem"] = taskResult.Data;
                        summary.Add($"Görev: {taskResult.Data.Title}");
                    }
                }
                break;

            case "Meeting":
                if (request.Parameters.TryGetValue("meetingId", out var mIdObj) &&
                    int.TryParse(mIdObj.ToString(), out var mId))
                {
                    var meetingResult = await meetingSvc.GetByIdAsync(mId);
                    if (meetingResult is { IsSuccess: true, Data: not null })
                    {
                        context.Entities["Meeting"] = meetingResult.Data;
                        summary.Add($"Toplantı: {meetingResult.Data.Title}");
                    }
                }
                break;

            default:
                logger.LogDebug("Unknown entity type in RequiredEntities: {EntityType}", entityType);
                break;
        }
    }
}
