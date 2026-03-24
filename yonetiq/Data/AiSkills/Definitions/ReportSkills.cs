using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class ReportSkills
{
    public static SkillDefinition Explain => new()
    {
        Id = "query.explain",
        Name = "Sorgu Sonucu Açıklama",
        Description = "Sorgu sonuçlarını analiz ederek iş birimine yönelik anlaşılır açıklama üretir",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-chat-left-text",
        RequiredEntities = ["QueryRecord"],
        RequiredContext = ["query_result_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "query.explain.system.md",
        UserPromptFile = "query.explain.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Açıklama iş birimi tarafından anlaşılır bulundu",
        DependencyServices = ["QueryService"]
    };

    public static SkillDefinition NlToSql => new()
    {
        Id = "query.nl_to_sql",
        Name = "Doğal Dil → SQL",
        Description = "Türkçe doğal dil ifadesini SQL sorgusuna dönüştürür",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-translate",
        RequiredEntities = [],
        RequiredContext = ["db_schema"],
        RequiresUserInput = true,
        OutputType = SkillOutputType.StructuredData,
        Temperature = 0.1f,
        SystemPromptFile = "query.nl_to_sql.system.md",
        UserPromptFile = "query.nl_to_sql.user.md",
        MaxOutputLength = 2000,
        ProhibitedActions = ["dml_üretme"],
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Üretilen SQL çalıştırılabilir ve doğru sonuç döner",
        DependencyServices = ["QueryService"]
    };

    public static SkillDefinition ExecSummary => new()
    {
        Id = "query.exec_summary",
        Name = "Yönetici Özeti",
        Description = "Sorgu sonuçlarından yöneticiye sunulabilir kısa özet üretir",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-file-earmark-text",
        RequiredEntities = ["QueryRecord"],
        RequiredContext = ["query_result_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "query.exec_summary.system.md",
        UserPromptFile = "query.exec_summary.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Özet yönetici tarafından yeterli bulundu",
        DependencyServices = ["QueryService"]
    };

    public static SkillDefinition Anomaly => new()
    {
        Id = "query.anomaly",
        Name = "Anomali Tespiti",
        Description = "Sorgu sonuçlarındaki normal dışı değerleri ve beklenmeyen kalıpları saptar",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-exclamation-diamond",
        RequiredEntities = ["QueryRecord"],
        RequiredContext = ["query_result_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "query.anomaly.system.md",
        UserPromptFile = "query.anomaly.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen anomali gerçek verilerle doğrulanabilir",
        DependencyServices = ["QueryService"]
    };

    public static SkillDefinition NextQuestion => new()
    {
        Id = "query.next_question",
        Name = "Sonraki Soru Önerisi",
        Description = "Mevcut sorgu sonuçlarına bakarak mantıklı takip soruları önerir",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-question-circle",
        RequiredEntities = ["QueryRecord"],
        RequiredContext = ["query_result_data"],
        OutputType = SkillOutputType.Suggestion,
        Temperature = 0.5f,
        SystemPromptFile = "query.next_question.system.md",
        UserPromptFile = "query.next_question.user.md",
        MaxOutputLength = 800,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı önerilen sorulardan en az birini çalıştırdı",
        DependencyServices = ["QueryService"]
    };

    public static SkillDefinition Trends => new()
    {
        Id = "query.trends",
        Name = "Trend Tespiti",
        Description = "Sorgu sonuçlarındaki zaman bazlı trendleri ve dönemsel değişimleri tespit eder",
        Category = SkillCategory.Query,
        TriggerMode = TriggerMode.Reactive,
        Module = "query",
        Icon = "bi-graph-up",
        RequiredEntities = ["QueryRecord"],
        RequiredContext = ["query_result_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "query.trends.system.md",
        UserPromptFile = "query.trends.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen trend gerçek veriyle doğrulanabilir",
        DependencyServices = ["QueryService"]
    };
}
