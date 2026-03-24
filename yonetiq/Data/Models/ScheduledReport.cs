using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

public class ScheduledReport : BaseEntity
{
    public int     QueryRecordId  { get; set; }
    public string  Title          { get; set; } = string.Empty;
    public string  Channel        { get; set; } = "Email";      // Email | Telegram
    public string  Recipient      { get; set; } = string.Empty;
    public string  FrequencyType  { get; set; } = "Daily";      // Daily | Weekly | Monthly
    public TimeOnly RunTime       { get; set; } = new(8, 0);
    public int?    DayOfWeek      { get; set; }
    public int?    DayOfMonth     { get; set; }
    public bool    IsActive       { get; set; } = true;
    public DateTime? LastRunAt   { get; set; }
    public DateTime? NextRunAt   { get; set; }
    public int     CreatedBy      { get; set; }

    // JOIN ile doldurulur, EF görmez
    [NotMapped]
    public string? QueryName      { get; set; }
}
