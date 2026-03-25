using System.Collections.Concurrent;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Prompt şablonlarını diskten veya embedded resource'lardan yükler ve değişken yerleştirmesi yapar.
/// Yüklenen şablonlar bellekte önbelleğe alınır. Prompt dosyaları isteğe bağlı versiyon başlığı içerebilir.
/// </summary>
public class PromptEngine : IDisposable
{
    private readonly IWebHostEnvironment env;
    private readonly ILogger<PromptEngine> logger;
    private readonly ConcurrentDictionary<string, (int Version, string Content)> _cache = new();
    private readonly FileSystemWatcher? _watcher;

    public PromptEngine(IWebHostEnvironment env, ILogger<PromptEngine> logger)
    {
        this.env = env;
        this.logger = logger;

        // Hot-reload: prompt dosyaları değiştiğinde cache'i temizle
        var promptDir = Path.Combine(env.ContentRootPath, "Data", "AiSkills", "Prompts");
        if (Directory.Exists(promptDir))
        {
            _watcher = new FileSystemWatcher(promptDir, "*.md")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnPromptFileChanged;
            _watcher.Created += OnPromptFileChanged;
            _watcher.Renamed += (_, e) => OnPromptFileChanged(null, e);
        }
    }

    private void OnPromptFileChanged(object? sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        if (_cache.TryRemove(fileName, out _))
            logger.LogInformation("Prompt cache invalidated: {FileName}", fileName);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Belirtilen dosya adı ile prompt şablonunu yükler.
    /// Önce Data/AiSkills/Prompts/{fileName} disk yolunu dener, bulamazsa embedded resource'a düşer.
    /// Sonuçlar bellekte önbelleğe alınır. Versiyon başlığı varsa ayrıştırılır ve içerikten çıkarılır.
    /// </summary>
    public async Task<string> LoadPromptAsync(string fileName)
    {
        // Development'ta cache bypass — prompt düzenlerken anında yansısın
        if (env.IsDevelopment())
        {
            var devPath = Path.Combine(env.ContentRootPath, "Data", "AiSkills", "Prompts", fileName);
            if (File.Exists(devPath))
            {
                var raw = await File.ReadAllTextAsync(devPath);
                var (version, content) = ParseVersionHeader(raw);
                _cache[fileName] = (version, content);
                return content;
            }
        }

        if (_cache.TryGetValue(fileName, out var cached))
            return cached.Content;

        // 1. Disk yolu: Data/AiSkills/Prompts/{fileName}
        var diskPath = Path.Combine(env.ContentRootPath, "Data", "AiSkills", "Prompts", fileName);
        if (File.Exists(diskPath))
        {
            var raw = await File.ReadAllTextAsync(diskPath);
            var (version, content) = ParseVersionHeader(raw);
            _cache[fileName] = (version, content);
            if (version > 0)
                logger.LogDebug("Loaded prompt {FileName} v{Version}", fileName, version);
            else
                logger.LogDebug("Prompt loaded from disk: {FileName}", fileName);
            return content;
        }

        // 2. Embedded resource fallback
        var assembly = typeof(PromptEngine).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is not null)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var raw = await reader.ReadToEndAsync();
                var (version, content) = ParseVersionHeader(raw);
                _cache[fileName] = (version, content);
                if (version > 0)
                    logger.LogDebug("Loaded prompt {FileName} v{Version} from embedded resource", fileName, version);
                else
                    logger.LogDebug("Prompt loaded from embedded resource: {FileName}", fileName);
                return content;
            }
        }

        logger.LogWarning("Prompt file not found: {FileName}. Returning empty string.", fileName);
        return string.Empty;
    }

    /// <summary>
    /// System prompt yükler ve evrensel kuralları (_system_rules.md) başına ekler.
    /// Tüm skill'lerin system prompt'larında tutarlı format/kalite kuralları sağlar.
    /// </summary>
    public async Task<string> LoadSystemPromptAsync(string fileName)
    {
        var content = await LoadPromptAsync(fileName);
        if (string.IsNullOrWhiteSpace(content)) return content;

        // Evrensel kuralları yükle ve başa ekle
        var rules = await LoadPromptAsync("_system_rules.md");
        if (!string.IsNullOrWhiteSpace(rules))
            return $"{rules}\n\n---\n\n{content}";

        return content;
    }

    /// <summary>
    /// Belirtilen prompt dosyasının versiyon numarasını döner. Dosya yüklenmemişse -1 döner.
    /// </summary>
    public int GetPromptVersion(string fileName)
    {
        return _cache.TryGetValue(fileName, out var cached) ? cached.Version : -1;
    }

    /// <summary>Prompt version header'ını parse eder. Format: ---version: N---</summary>
    private static (int Version, string Content) ParseVersionHeader(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return (0, rawContent);

        var firstLine = rawContent.Split('\n', 2)[0].Trim();
        if (firstLine.StartsWith("---version:") && firstLine.EndsWith("---"))
        {
            var versionStr = firstLine.Replace("---version:", "").Replace("---", "").Trim();
            if (int.TryParse(versionStr, out var version))
            {
                var content = rawContent.Split('\n', 2).Length > 1 ? rawContent.Split('\n', 2)[1] : string.Empty;
                return (version, content.TrimStart());
            }
        }
        return (0, rawContent); // No version header = version 0
    }

    /// <summary>
    /// Şablon içindeki {{variable}} ifadelerini verilen değişkenlerle değiştirir.
    /// </summary>
    public string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value ?? string.Empty);
        }

        return result;
    }
}
