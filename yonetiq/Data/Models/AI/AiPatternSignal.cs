namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Öğrenme sinyali kaydı — thumbs up/down, SQL kabul/düzeltme, decay vb.
/// LearningSignalService bu sinyalleri kaydeder ve pattern skorunu hesaplar.
/// </summary>
public class AiPatternSignal
{
    public int Id { get; set; }
    public int PatternId { get; set; }

    /// <summary>ThumbsUp, ThumbsDown, SqlAccepted, SqlCorrection, ImplicitAccept, NoRequery, Decay</summary>
    public string SignalType { get; set; } = string.Empty;

    /// <summary>Sinyal ağırlığı: +3.0 (thumbs up) ile -5.0 (thumbs down) arası</summary>
    public decimal Weight { get; set; }

    public int? UserId { get; set; }
    public int? SourceInteractionId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
