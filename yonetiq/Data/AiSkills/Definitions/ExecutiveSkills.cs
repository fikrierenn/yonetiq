using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class ExecutiveSkills
{
    public static SkillDefinition DailyBriefing => new()
    {
        Id = "exec.daily_briefing",
        Name = "Günlük Briefing",
        Description = "Kişiselleştirilmiş günlük yönetici özeti: geciken görevler, bekleyen kararlar, bugünün programı",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-sunrise",
        RequiredEntities = ["TaskItem", "Decision", "Meeting", "ApprovalRequest"],
        RequiredContext = ["overdue_tasks", "pending_decisions", "today_meetings", "pending_approvals"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "exec.daily_briefing.system.md",
        UserPromptFile = "exec.daily_briefing.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı briefing'i faydalı buldu (rating >= 2)",
        DependencyServices = ["TaskService", "MeetingService", "ApprovalService", "SessionService"]
    };

    public static SkillDefinition ChangeDetection => new()
    {
        Id = "exec.change_detection",
        Name = "Değişim Tespiti",
        Description = "Son 24 saatte sistemdeki önemli değişiklikleri özetler",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-arrow-repeat",
        RequiredEntities = ["TaskItem", "Decision"],
        RequiredContext = ["new_tasks_24h", "completed_tasks_24h", "new_decisions_24h"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "exec.change_detection.system.md",
        UserPromptFile = "exec.change_detection.user.md",
        MaxOutputLength = 800,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Değişiklik tespiti doğru ve eksiksiz",
        DependencyServices = ["TaskService", "MeetingService"]
    };

    public static SkillDefinition Focus => new()
    {
        Id = "exec.focus",
        Name = "Odak Önerisi",
        Description = "Bugün en yüksek etkili 3 iş önerisi sunar",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-bullseye",
        RequiredEntities = ["TaskItem", "Meeting", "ApprovalRequest"],
        RequiredContext = ["overdue_tasks", "today_meetings", "pending_approvals"],
        OutputType = SkillOutputType.Suggestion,
        Temperature = 0.3f,
        SystemPromptFile = "exec.focus.system.md",
        UserPromptFile = "exec.focus.user.md",
        MaxOutputLength = 800,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı önerilen odak noktalarından en az birini uyguladı",
        DependencyServices = ["TaskService", "MeetingService", "ApprovalService"]
    };

    public static SkillDefinition KpiAnalysis => new()
    {
        Id = "exec.kpi_analysis",
        Name = "KPI Yorumu",
        Description = "KPI kartlarının trend ve anomali açıklamasını yapar",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Reactive,
        Module = "dashboard",
        Icon = "bi-graph-up-arrow",
        RequiredEntities = [],
        RequiredContext = ["kpi_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "exec.kpi_analysis.system.md",
        UserPromptFile = "exec.kpi_analysis.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "KPI yorumu veriye dayalı ve doğrulanabilir",
        DependencyServices = ["KpiTargetService", "AnalyticsService"]
    };

    public static SkillDefinition WeeklyDigest => new()
    {
        Id = "exec.weekly_digest",
        Name = "Haftalık Özet",
        Description = "Son 7 günün operasyonel özetini yönetici seviyesinde hazırlar",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-calendar-week",
        RequiredEntities = ["TaskItem", "Meeting"],
        RequiredContext = ["overdue_tasks", "completed_tasks", "today_meetings"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "exec.weekly_digest.system.md",
        UserPromptFile = "exec.weekly_digest.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Haftalık özet yönetici tarafından faydalı bulundu",
        DependencyServices = ["TaskService", "MeetingService", "OkrService"]
    };

    public static SkillDefinition RiskScan => new()
    {
        Id = "exec.risk_scan",
        Name = "Risk Taraması",
        Description = "Tüm modüllerdeki verileri tarayarak potansiyel riskleri belirler",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Proactive,
        Module = "dashboard",
        Icon = "bi-shield-exclamation",
        RequiredEntities = [],
        RequiredContext = ["overdue_tasks", "pending_decisions", "pending_approvals"],
        OptionalContext = ["kpi_data", "assignee_workload"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "exec.risk_scan.system.md",
        UserPromptFile = "exec.risk_scan.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen riskler gerçek verilerle doğrulanabilir",
        DependencyServices = ["TaskService", "MeetingService", "ApprovalService", "OkrService"]
    };
}
