using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

public class Objective : BaseEntity
{
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int?    OwnerId     { get; set; }
    public string  OwnerName   { get; set; } = string.Empty;
    public string  Level       { get; set; } = "Company";    // Company, Department, Personal
    public string  Period      { get; set; } = "Q1";         // Q1-Q4, H1, H2, Annual
    public int     Year        { get; set; } = 2026;
    public string  Status      { get; set; } = "Active";
    public decimal Progress    { get; set; }                  // 0-100

    [NotMapped]
    public List<KeyResult> KeyResults { get; set; } = [];

    [NotMapped]
    public string LevelLabel => Level switch { "Department" => "Departman", "Personal" => "Kişisel", _ => "Şirket" };

    [NotMapped]
    public string ProgressClass => Progress switch { >= 75 => "success", >= 40 => "warning", _ => "danger" };
}

public class KeyResult : BaseEntity
{
    public int     ObjectiveId  { get; set; }
    public string  Title        { get; set; } = string.Empty;
    public string  MetricType   { get; set; } = "Number";   // Number, Percentage, Boolean
    public decimal StartValue   { get; set; }
    public decimal TargetValue  { get; set; }
    public decimal CurrentValue { get; set; }
    public string? Unit         { get; set; }
    public int?    OwnerId      { get; set; }
    public string  OwnerName    { get; set; } = string.Empty;
    public int?    LinkedTaskId { get; set; }
    public decimal Progress     { get; set; }   // 0-100 hesaplanan

    [NotMapped]
    public string ProgressClass => Progress switch { >= 75 => "success", >= 40 => "warning", _ => "danger" };

    [NotMapped]
    public string CurrentDisplay => MetricType == "Boolean"
        ? (CurrentValue > 0 ? "Tamamlandı" : "Devam ediyor")
        : $"{CurrentValue:N0}{(Unit != null ? " " + Unit : "")} / {TargetValue:N0}{(Unit != null ? " " + Unit : "")}";
}
