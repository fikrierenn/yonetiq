using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

public class TimeEntry : BaseEntity
{
    public int      TaskItemId  { get; set; }
    public int      UserId      { get; set; }
    public string   UserName    { get; set; } = string.Empty;
    public DateTime StartedAt   { get; set; }
    public DateTime? EndedAt    { get; set; }
    public int?     DurationMin { get; set; }
    public string?  Note        { get; set; }

    [NotMapped]
    public bool IsRunning => EndedAt is null;

    [NotMapped]
    public string DurationDisplay => DurationMin.HasValue
        ? $"{DurationMin / 60}s {DurationMin % 60}dk"
        : "Devam ediyor...";
}
