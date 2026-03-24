using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class ApprovalSkills
{
    public static SkillDefinition Summarize => new()
    {
        Id = "approval.summarize",
        Name = "Onay Özeti",
        Description = "Bekleyen onay taleplerini özetler ve öncelik sırasına koyar",
        Category = SkillCategory.Approval,
        TriggerMode = TriggerMode.Proactive,
        Module = "approval",
        Icon = "bi-clipboard-check",
        RequiredEntities = ["ApprovalRequest"],
        RequiredContext = ["pending_approvals", "requester_info"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "approval.summarize.system.md",
        UserPromptFile = "approval.summarize.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Özet bekleyen onayları eksiksiz kapsar",
        DependencyServices = ["ApprovalService"]
    };

    public static SkillDefinition Risk => new()
    {
        Id = "approval.risk",
        Name = "Onay Risk Tespiti",
        Description = "Sıra dışı veya yüksek riskli onay taleplerini tespit eder",
        Category = SkillCategory.Approval,
        TriggerMode = TriggerMode.Reactive,
        Module = "approval",
        Icon = "bi-shield-exclamation",
        RequiredEntities = ["ApprovalRequest"],
        RequiredContext = ["approval_history", "requester_info"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "approval.risk.system.md",
        UserPromptFile = "approval.risk.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen riskler doğrulanabilir verilerle desteklendi",
        DependencyServices = ["ApprovalService"]
    };

    public static SkillDefinition Bottleneck => new()
    {
        Id = "approval.bottleneck",
        Name = "Darboğaz Tespiti",
        Description = "Onay süreçlerindeki tıkanıklıkları ve gecikmeleri tespit eder",
        Category = SkillCategory.Approval,
        TriggerMode = TriggerMode.Proactive,
        Module = "approval",
        Icon = "bi-hourglass-split",
        RequiredEntities = [],
        RequiredContext = ["pending_approvals"],
        OptionalContext = ["approval_history"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "approval.bottleneck.system.md",
        UserPromptFile = "approval.bottleneck.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Darboğaz tespiti gerçek gecikme verileriyle doğrulanabilir",
        DependencyServices = ["ApprovalService"]
    };

    public static SkillDefinition Anomaly => new()
    {
        Id = "approval.anomaly",
        Name = "Sıra Dışı Tespit",
        Description = "Normal kalıplardan sapan onay isteklerini tespit eder",
        Category = SkillCategory.Approval,
        TriggerMode = TriggerMode.Proactive,
        Module = "approval",
        Icon = "bi-exclamation-diamond",
        RequiredEntities = [],
        RequiredContext = ["pending_approvals"],
        OptionalContext = ["approval_history"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "approval.anomaly.system.md",
        UserPromptFile = "approval.anomaly.user.md",
        MaxOutputLength = 800,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Anomali tespiti normal kalıpla karşılaştırılarak doğrulandı",
        DependencyServices = ["ApprovalService"]
    };
}
