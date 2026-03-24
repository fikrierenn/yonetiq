using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using YonetIQ.Data.Services.AI;

namespace YonetIQ.Tests.Unit.AI;

public class PromptEngineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PromptEngine _engine;

    public PromptEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"yonetiq_test_{Guid.NewGuid():N}");
        var promptDir = Path.Combine(_tempDir, "Data", "AiSkills", "Prompts");
        Directory.CreateDirectory(promptDir);

        // Test prompt dosyaları oluştur
        File.WriteAllText(Path.Combine(promptDir, "test.system.md"),
            "---version: 3---\nSen bir AI asistansın. {{role}} olarak çalış.");
        File.WriteAllText(Path.Combine(promptDir, "noversion.md"),
            "Bu bir versiyonsuz prompt.");

        var env = new TestWebHostEnvironment(_tempDir);
        _engine = new PromptEngine(env, NullLogger<PromptEngine>.Instance);
    }

    [Fact]
    public async Task LoadPromptAsync_ExistingFile_ReturnsContent()
    {
        var content = await _engine.LoadPromptAsync("test.system.md");
        Assert.Contains("AI asistansın", content);
    }

    [Fact]
    public async Task LoadPromptAsync_ExistingFile_StripsVersionHeader()
    {
        var content = await _engine.LoadPromptAsync("test.system.md");
        Assert.DoesNotContain("---version:", content);
    }

    [Fact]
    public async Task LoadPromptAsync_ExistingFile_ParsesVersion()
    {
        await _engine.LoadPromptAsync("test.system.md");
        Assert.Equal(3, _engine.GetPromptVersion("test.system.md"));
    }

    [Fact]
    public async Task LoadPromptAsync_NoVersionHeader_VersionIsZero()
    {
        await _engine.LoadPromptAsync("noversion.md");
        Assert.Equal(0, _engine.GetPromptVersion("noversion.md"));
    }

    [Fact]
    public async Task LoadPromptAsync_NonExistingFile_ReturnsEmpty()
    {
        var content = await _engine.LoadPromptAsync("nonexistent.md");
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void RenderTemplate_ReplacesVariables()
    {
        var template = "Merhaba {{name}}, {{role}} olarak görev yap.";
        var vars = new Dictionary<string, string>
        {
            ["name"] = "YonetIQ",
            ["role"] = "asistan"
        };
        var result = _engine.RenderTemplate(template, vars);
        Assert.Equal("Merhaba YonetIQ, asistan olarak görev yap.", result);
    }

    [Fact]
    public void RenderTemplate_UnknownVariable_LeavesAsIs()
    {
        var template = "Merhaba {{name}}, {{unknown}} burada.";
        var vars = new Dictionary<string, string> { ["name"] = "Test" };
        var result = _engine.RenderTemplate(template, vars);
        Assert.Equal("Merhaba Test, {{unknown}} burada.", result);
    }

    [Fact]
    public void RenderTemplate_EmptyTemplate_ReturnsEmpty()
    {
        var result = _engine.RenderTemplate("", new Dictionary<string, string>());
        Assert.Equal(string.Empty, result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private class TestWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "YonetIQ.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Testing";
    }
}
