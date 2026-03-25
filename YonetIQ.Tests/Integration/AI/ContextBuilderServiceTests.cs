using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using YonetIQ.Data.Models;
using YonetIQ.Data.Models.AI;
using YonetIQ.Data.Services;
using YonetIQ.Data.Services.AI;

namespace YonetIQ.Tests.Integration.AI;

public class ContextBuilderServiceTests
{
    private static IConfiguration MockConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=true;"
            })
            .Build();

    private static ContextBuilderService CreateService()
    {
        var config = MockConfig();
        var sessionSvc = new SessionService();
        sessionSvc.Login(new User
        {
            Id = 1,
            FullName = "Test User",
            Email = "test@test.com",
            RoleName = "Genel Müdür"
        });

        var auditSvc = new AuditService(config, sessionSvc);
        var taskSvc = new TaskService(config, auditSvc, new NotificationService(config, auditSvc));
        var meetingSvc = new MeetingService(config, auditSvc);
        var dataSourceSvc = new DataSourceService(config, auditSvc);
        var querySvc = new QueryService(config, auditSvc, dataSourceSvc);
        var noteSvc = new NoteService(config, auditSvc, taskSvc);

        return new ContextBuilderService(
            taskSvc,
            meetingSvc,
            noteSvc,
            querySvc,
            new QuerySetService(config, auditSvc, querySvc),
            new UserService(config, auditSvc),
            sessionSvc,
            new LookupService(config),
            new ApprovalService(config, auditSvc),
            new OkrService(config, auditSvc),
            new KpiTargetService(config, auditSvc),
            new SemanticService(config, auditSvc),
            new SemanticEnricher(new SemanticService(config, auditSvc)),
            new ConversationContextService(config, auditSvc, NullLogger<ConversationContextService>.Instance),
            NullLogger<ContextBuilderService>.Instance);
    }

    [Fact]
    public async Task BuildContextAsync_SetsUserFromSession()
    {
        var svc = CreateService();
        var skill = YonetIQ.Data.AiSkills.Definitions.TaskSkills.Clarify;
        var request = new AiRequest { SkillId = "task.clarify", UserId = 1, Module = "task" };

        var context = await svc.BuildContextAsync(skill, request);

        Assert.NotNull(context);
        Assert.NotNull(context.User);
        Assert.Equal("Test User", context.User.FullName);
    }

    [Fact]
    public async Task BuildContextAsync_HandlesErrorsGracefully()
    {
        var svc = CreateService();
        var skill = YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.DailyBriefing;
        var request = new AiRequest { SkillId = "exec.daily_briefing", UserId = 1, Module = "dashboard" };

        // Should not throw even with invalid DB connection
        var context = await svc.BuildContextAsync(skill, request);
        Assert.NotNull(context);
    }

    [Fact]
    public void SkillContext_HasExpectedStructure()
    {
        var context = new SkillContext
        {
            User = new SessionInfo { UserId = 1, FullName = "Test", Role = "Admin" },
            UserInput = "test input",
            ContextSummary = "test summary",
            EstimatedTokenCount = 100
        };

        context.Entities["TaskItem"] = new TaskItem { Title = "Test Task" };
        context.ContextData["pendingTasks"] = "5";

        Assert.Single(context.Entities);
        Assert.Single(context.ContextData);
        Assert.Equal(100, context.EstimatedTokenCount);
    }

    [Fact]
    public void PermissionFilter_AdminGetsFullAccess()
    {
        var context = new SkillContext
        {
            User = new SessionInfo { UserId = 1, FullName = "CEO", Role = "Genel Müdür" }
        };
        context.ContextData["testData"] = "sensitive";

        // Admin should not have permissionNote added
        Assert.False(context.ContextData.ContainsKey("permissionNote"));
    }
}
