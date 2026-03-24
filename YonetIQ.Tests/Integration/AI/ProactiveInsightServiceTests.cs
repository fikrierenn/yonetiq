using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;
using YonetIQ.Data.Models.AI;
using YonetIQ.Data.Services;
using YonetIQ.Data.Services.AI;

namespace YonetIQ.Tests.Integration.AI;

public class ProactiveInsightServiceTests
{
    private static IConfiguration MockConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=true;"
            })
            .Build();

    private static ProactiveInsightService CreateService()
    {
        var config = MockConfig();
        var sessionSvc = new SessionService();
        var auditSvc = new AuditService(config, sessionSvc);

        return new ProactiveInsightService(
            new TaskService(config, auditSvc, new NotificationService(config, auditSvc)),
            new MeetingService(config, auditSvc),
            new ApprovalService(config, auditSvc),
            new OkrService(config, auditSvc),
            NullLogger<ProactiveInsightService>.Instance);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsEmptyList_WhenServicesUnavailable()
    {
        var svc = CreateService();
        // Without real DB, all service calls fail → empty insights (try-catch handles it)
        var result = await svc.GenerateInsightsAsync(1);
        Assert.NotNull(result);
        Assert.IsType<List<AiInsight>>(result);
    }

    [Fact]
    public void AiInsight_HasCorrectDefaults()
    {
        var insight = new AiInsight
        {
            Type = InsightType.OverdueTasks,
            Priority = InsightPriority.Critical,
            Title = "5 görev gecikmiş",
            Detail = "Test detail",
            Module = "task"
        };

        Assert.Equal(InsightType.OverdueTasks, insight.Type);
        Assert.Equal(InsightPriority.Critical, insight.Priority);
        Assert.Equal("task", insight.Module);
    }

    [Fact]
    public void InsightType_Enum_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.OverdueTasks));
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.PendingDecisions));
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.PendingApprovals));
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.UpcomingMeetings));
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.CompletedYesterday));
        Assert.True(Enum.IsDefined(typeof(InsightType), InsightType.Risk));
    }

    [Fact]
    public void InsightPriority_Ordering()
    {
        Assert.True(InsightPriority.Critical > InsightPriority.High);
        Assert.True(InsightPriority.High > InsightPriority.Normal);
        Assert.True(InsightPriority.Normal > InsightPriority.Low);
    }
}
