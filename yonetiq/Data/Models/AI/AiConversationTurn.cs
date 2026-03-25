namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Çok turlu sohbetteki tek bir tur (kullanıcı girdisi + AI çıktısı).
/// ConversationContextService bu kayıtları yönetir.
/// </summary>
public class AiConversationTurn
{
    public int Id { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int TurnIndex { get; set; }
    public string? SkillId { get; set; }
    public string UserInput { get; set; } = string.Empty;
    public string? AiOutput { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
