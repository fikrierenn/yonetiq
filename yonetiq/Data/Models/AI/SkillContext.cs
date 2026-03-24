namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Bir skill'in çalıştırılması için toplanan bağlam verisi.
/// ContextBuilderService tarafından doldurulur.
/// </summary>
public class SkillContext
{
    /// <summary>Kullanıcı bilgileri</summary>
    public required SessionInfo User { get; set; }

    /// <summary>İlgili entity verileri (key: entity tipi, value: serialized data)</summary>
    public Dictionary<string, object> Entities { get; set; } = new();

    /// <summary>Ek bağlam verileri (key: context adı, value: data)</summary>
    public Dictionary<string, object> ContextData { get; set; } = new();

    /// <summary>Kullanıcı giriş metni (CommandBar'dan veya inline input'tan)</summary>
    public string? UserInput { get; set; }

    /// <summary>Önceki ilgili etkileşimler (varsa)</summary>
    public List<AiInteraction> PreviousInteractions { get; set; } = [];

    /// <summary>Toplanan bağlamın tahmini token sayısı</summary>
    public int EstimatedTokenCount { get; set; }

    /// <summary>Bağlam açıklaması (explainability için)</summary>
    public string ContextSummary { get; set; } = string.Empty;
}
