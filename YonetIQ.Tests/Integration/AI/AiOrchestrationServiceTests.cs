using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using YonetIQ.Data.Models.AI;
using YonetIQ.Data.Services.AI;

namespace YonetIQ.Tests.Integration.AI;

public class AiOrchestrationServiceTests
{
    private static SkillRegistry CreateRegistryWithSkills()
    {
        var registry = new SkillRegistry(NullLogger<SkillRegistry>.Instance);
        registry.Register(new SkillDefinition
        {
            Id = "task.clarify",
            Name = "Görev Netleştirme",
            Category = SkillCategory.Task,
            Module = "task",
            SystemPromptFile = "task.clarify.system.md",
            UserPromptFile = "task.clarify.user.md"
        });
        registry.Register(new SkillDefinition
        {
            Id = "meeting.summarize",
            Name = "Toplantı Özeti",
            Category = SkillCategory.Meeting,
            Module = "meeting",
            SystemPromptFile = "meeting.summarize.system.md",
            UserPromptFile = "meeting.summarize.user.md"
        });
        return registry;
    }

    [Fact]
    public void SkillRegistry_ResolveByModule_ReturnsCorrectSkills()
    {
        var registry = CreateRegistryWithSkills();

        var taskSkills = registry.ResolveByModule("task");
        Assert.Single(taskSkills);
        Assert.Equal("task.clarify", taskSkills[0].Id);

        var meetingSkills = registry.ResolveByModule("meeting");
        Assert.Single(meetingSkills);
        Assert.Equal("meeting.summarize", meetingSkills[0].Id);
    }

    [Fact]
    public void AiRequest_CanBeCreatedWithSkillId()
    {
        var request = new AiRequest
        {
            SkillId = "task.clarify",
            UserId = 1,
            Module = "task",
            Parameters = new Dictionary<string, object> { ["taskId"] = 42 }
        };

        Assert.Equal("task.clarify", request.SkillId);
        Assert.Equal(1, request.UserId);
        Assert.Equal(42, request.Parameters["taskId"]);
    }

    [Fact]
    public void AiRequest_CanBeCreatedWithUserInput()
    {
        var request = new AiRequest
        {
            UserInput = "Bu görevi netleştir",
            UserId = 1,
            Module = "task"
        };

        Assert.Null(request.SkillId);
        Assert.Equal("Bu görevi netleştir", request.UserInput);
    }

    [Fact]
    public void AiResponse_Failure_HasCorrectFields()
    {
        var response = AiResponse.Failure("Skill bulunamadı", "task.unknown");

        Assert.False(response.IsSuccess);
        Assert.Contains("bulunamadı", response.ErrorMessage);
    }

    [Fact]
    public void SkillDefinition_AllNewSkillsHavePromptFiles()
    {
        // Verify all 33 registered skills have system+user prompt files
        var allSkills = new SkillDefinition[]
        {
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Clarify,
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Risk,
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Decompose,
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Bottleneck,
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Followup,
            YonetIQ.Data.AiSkills.Definitions.TaskSkills.Prioritize,
            YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Summarize,
            YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractActions,
            YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractDecisions,
            YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Preparation,
            YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Unresolved,
            YonetIQ.Data.AiSkills.Definitions.NoteSkills.Structure,
            YonetIQ.Data.AiSkills.Definitions.NoteSkills.SuggestTags,
            YonetIQ.Data.AiSkills.Definitions.NoteSkills.ExtractActions,
            YonetIQ.Data.AiSkills.Definitions.NoteSkills.ToAgenda,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.DailyBriefing,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.ChangeDetection,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.Focus,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.KpiAnalysis,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.WeeklyDigest,
            YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.RiskScan,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.Explain,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.NlToSql,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.ExecSummary,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.Anomaly,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.NextQuestion,
            YonetIQ.Data.AiSkills.Definitions.ReportSkills.Trends,
            YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Summarize,
            YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Risk,
            YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Bottleneck,
            YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Anomaly,
            YonetIQ.Data.AiSkills.Definitions.OkrSkills.Progress,
            YonetIQ.Data.AiSkills.Definitions.OkrSkills.Action,
        };

        foreach (var skill in allSkills)
        {
            Assert.False(string.IsNullOrWhiteSpace(skill.SystemPromptFile),
                $"Skill {skill.Id} has no SystemPromptFile");
            Assert.False(string.IsNullOrWhiteSpace(skill.UserPromptFile),
                $"Skill {skill.Id} has no UserPromptFile");
            Assert.False(string.IsNullOrWhiteSpace(skill.Id),
                $"Skill has no Id");
            Assert.False(string.IsNullOrWhiteSpace(skill.Name),
                $"Skill {skill.Id} has no Name");
            Assert.True(skill.MaxOutputLength > 0,
                $"Skill {skill.Id} has invalid MaxOutputLength");
        }
    }

    [Fact]
    public void AllSkillIds_AreUnique()
    {
        var registry = new SkillRegistry(NullLogger<SkillRegistry>.Instance);

        // Register all skills just like Program.cs does
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.DailyBriefing);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.ChangeDetection);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.Focus);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.KpiAnalysis);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Clarify);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Risk);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Decompose);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Bottleneck);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Followup);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Prioritize);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Summarize);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractActions);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractDecisions);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Preparation);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.Structure);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.SuggestTags);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Explain);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.NlToSql);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.ExecSummary);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Anomaly);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.NextQuestion);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Summarize);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Risk);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.OkrSkills.Progress);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.OkrSkills.Action);
        // Faz 4b skill'leri
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Unresolved);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.ExtractActions);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.ToAgenda);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.WeeklyDigest);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.RiskScan);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Bottleneck);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Anomaly);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Trends);

        var all = registry.ListAll();
        Assert.Equal(33, all.Count);

        var uniqueIds = all.Select(s => s.Id).Distinct().Count();
        Assert.Equal(33, uniqueIds);
    }

    [Fact]
    public void AllSkills_HaveValidModules()
    {
        var validModules = new HashSet<string> { "task", "meeting", "dashboard", "note", "query", "approval", "okr", "global" };
        var registry = new SkillRegistry(NullLogger<SkillRegistry>.Instance);

        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Decompose);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Bottleneck);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Preparation);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.Focus);
        registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Anomaly);

        foreach (var skill in registry.ListAll())
        {
            Assert.Contains(skill.Module, validModules);
        }
    }
}
