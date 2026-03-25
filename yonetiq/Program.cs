using Microsoft.EntityFrameworkCore;
using Blazored.Toast;
using YonetIQ.Components;
using YonetIQ.Data;
using YonetIQ.Data.Services;

DotEnv.LoadIfExists(FindFileInParents(Directory.GetCurrentDirectory(), ".env") ?? string.Empty);

var builder = WebApplication.CreateBuilder(args);

var envConn = Environment.GetEnvironmentVariable("YONET_CONN");
if (string.IsNullOrWhiteSpace(envConn))
{
    envConn = Environment.GetEnvironmentVariable("SQLCLI_CONN");
}

var connStr = !string.IsNullOrWhiteSpace(envConn)
    ? envConn
    : builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection veya YONET_CONN tanimli degil.");

builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredToast();

// EF Core yalnizca migration/sema yonetimi icin.
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connStr));

builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<QuerySetService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TaskCommentService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<DataSourceService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<KpiTargetService>();
builder.Services.AddScoped<TaskTemplateService>();
builder.Services.AddScoped<TimeTrackingService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<OkrService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ScheduledReportService>();
builder.Services.AddScoped<SemanticService>();
builder.Services.AddScoped<GlobalSearchService>();
builder.Services.AddHostedService<ScheduledReportWorker>();
builder.Services.AddHttpClient<TelegramService>();
builder.Services.AddHttpClient<NoteChatService>();

// AI Skill Altyapısı
builder.Services.AddSingleton<YonetIQ.Data.Services.AI.SkillRegistry>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.PromptEngine>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.AiMemoryService>();
builder.Services.AddHttpClient<YonetIQ.Data.Services.AI.AiProviderService>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.SkillExecutor>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.ContextBuilderService>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.AiOrchestrationService>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.AiQualityTestService>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.AiEvaluationService>();
builder.Services.AddScoped<YonetIQ.Data.Services.AI.ProactiveInsightService>();

// DataSource şifreleme anahtarı yoksa uyar ve otomatik oluştur (yalnızca geliştirme ortamı için güvenli)
var encKey = builder.Configuration["DataSourceEncryptionKey"];
if (string.IsNullOrWhiteSpace(encKey))
{
    var generated = YonetIQ.Data.Infrastructure.AesEncryption.GenerateKey();
    builder.Configuration["DataSourceEncryptionKey"] = generated;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Error.WriteLine("[WARN] 'DataSourceEncryptionKey' appsettings.json içinde tanımlı değil!");
    Console.Error.WriteLine($"[WARN] Geçici anahtar oluşturuldu: {generated}");
    Console.Error.WriteLine("[WARN] Bu anahtarı appsettings.json'a ekleyin, aksi hâlde uygulama yeniden başladığında şifreli bağlantılar açılamaz!");
    Console.ResetColor();
}

DapperTypeHandlers.Register();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAntiforgery();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Count > 0)
    {
        await db.Database.MigrateAsync();
    }

    await SeedData.SeedAsync(scope.ServiceProvider);
}

// Skill tanımlarını registry'ye kaydet
{
    var registry = app.Services.GetRequiredService<YonetIQ.Data.Services.AI.SkillRegistry>();
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.DailyBriefing);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.ChangeDetection);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Clarify);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Risk);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Summarize);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractActions);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.Structure);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.SuggestTags);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Explain);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.NlToSql);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.ExecSummary);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Summarize);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Risk);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.OkrSkills.Progress);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.OkrSkills.Action);
    // Faz 4: Yeni skill'ler (24.03.2026)
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Decompose);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Bottleneck);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Followup);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.TaskSkills.Prioritize);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.ExtractDecisions);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Preparation);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.Focus);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.KpiAnalysis);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Anomaly);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.NextQuestion);
    // Faz 4b: Strateji dokümanından kalan skill'ler (24.03.2026)
    registry.Register(YonetIQ.Data.AiSkills.Definitions.MeetingSkills.Unresolved);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.ExtractActions);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.NoteSkills.ToAgenda);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.WeeklyDigest);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ExecutiveSkills.RiskScan);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Bottleneck);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ApprovalSkills.Anomaly);
    registry.Register(YonetIQ.Data.AiSkills.Definitions.ReportSkills.Trends);
}

// Development ortamında AI QA test suite çalıştır
if (app.Environment.IsDevelopment() &&
    Environment.GetEnvironmentVariable("RUN_AI_QA") == "1")
{
    using var qaScope = app.Services.CreateScope();
    var qaService = qaScope.ServiceProvider.GetRequiredService<YonetIQ.Data.Services.AI.AiQualityTestService>();
    var qaReport = await qaService.RunFullTestSuiteAsync();
    Console.WriteLine(qaReport.ToSummary());
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// WP12.3 — Health check endpoint
app.MapGet("/health", async (AppDbContext db, YonetIQ.Data.Services.AI.SkillRegistry registry) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "ok",
            db = "connected",
            skills = registry.Count,
            time = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"DB bağlantı hatası: {ex.Message}");
    }
});

app.Run();

static string? FindFileInParents(string startDirectory, string fileName)
{
    var dir = new DirectoryInfo(startDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        dir = dir.Parent;
    }

    return null;
}
