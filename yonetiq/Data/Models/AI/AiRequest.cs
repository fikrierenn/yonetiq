namespace YonetIQ.Data.Models.AI;

/// <summary>
/// AI orchestration'a gelen istek modeli.
/// </summary>
public class AiRequest
{
    /// <summary>Doğrudan çağrılacak skill ID (opsiyonel — buton tıklamasıyla gelen isteklerde dolu)</summary>
    public string? SkillId { get; set; }

    /// <summary>Serbest metin giriş (CommandBar'dan gelen isteklerde dolu)</summary>
    public string? UserInput { get; set; }

    /// <summary>İsteği yapan kullanıcı ID'si</summary>
    public int UserId { get; set; }

    /// <summary>Kullanıcının aktif olduğu modül (ör: "task", "meeting", "dashboard")</summary>
    public string Module { get; set; } = "global";

    /// <summary>Bağlam parametreleri — skill'e göre farklı entity ID'leri, filtreler vb.</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}
