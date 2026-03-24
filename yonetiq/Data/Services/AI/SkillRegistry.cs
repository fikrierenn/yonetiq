using System.Collections.Concurrent;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Tüm AI skill tanımlarını bellekte tutan singleton registry.
/// Uygulama başlangıcında skill'ler Register ile kaydedilir, runtime'da Resolve ile çözülür.
/// </summary>
public class SkillRegistry(ILogger<SkillRegistry> logger)
{
    private readonly ConcurrentDictionary<string, SkillDefinition> _skills = new();

    /// <summary>
    /// Yeni bir skill tanımını registry'ye ekler. Aynı ID varsa üzerine yazar.
    /// </summary>
    public void Register(SkillDefinition skill)
    {
        _skills[skill.Id] = skill;
        logger.LogInformation("Skill registered: {SkillId} ({SkillName})", skill.Id, skill.Name);
    }

    /// <summary>
    /// Verilen ID ile skill tanımını çözer. Bulunamazsa null döner.
    /// </summary>
    public SkillDefinition? Resolve(string skillId)
    {
        _skills.TryGetValue(skillId, out var skill);
        return skill;
    }

    /// <summary>
    /// Belirli bir modüle ait skill'leri döndürür.
    /// Opsiyonel olarak TriggerMode ile filtreleme yapılabilir.
    /// </summary>
    public List<SkillDefinition> ResolveByModule(string module, TriggerMode? mode = null)
    {
        var query = _skills.Values
            .Where(s => s.Module.Equals(module, StringComparison.OrdinalIgnoreCase)
                        || s.Module.Equals("global", StringComparison.OrdinalIgnoreCase));

        if (mode.HasValue)
            query = query.Where(s => s.TriggerMode == mode.Value);

        return query.ToList();
    }

    /// <summary>
    /// Kayıtlı tüm skill tanımlarını döndürür.
    /// </summary>
    public IReadOnlyList<SkillDefinition> ListAll()
    {
        return _skills.Values.ToList().AsReadOnly();
    }
}
