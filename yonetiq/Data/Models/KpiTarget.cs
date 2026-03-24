namespace YonetIQ.Data.Models;

public class KpiTarget : BaseEntity
{
    public string  MetricKey   { get; set; } = string.Empty;
    public string  Label       { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public string  Operator    { get; set; } = "<=";   // <=, >=, =
    public string  PeriodType  { get; set; } = "AllTime";
    public bool    IsActive    { get; set; } = true;

    // Hesaplanan (DB'den gelmiyor, UI'da doldurulur)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal? ActualValue { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Status => (ActualValue, Operator) switch
    {
        (null, _)    => "unknown",
        ({ } a, "<=") => a <= TargetValue ? "success" : "danger",
        ({ } a, ">=") => a >= TargetValue ? "success" : "danger",
        ({ } a, "=")  => a == TargetValue ? "success" : "danger",
        _            => "unknown"
    };
}
