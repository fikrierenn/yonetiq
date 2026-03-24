using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

public class ApprovalRequest : BaseEntity
{
    public string  EntityType      { get; set; } = "Custom";
    public int?    EntityId        { get; set; }
    public string  Title           { get; set; } = string.Empty;
    public string? Description     { get; set; }
    public int     RequestedBy     { get; set; }
    public string  RequestedByName { get; set; } = string.Empty;
    public string  Status          { get; set; } = "Pending";   // Pending, Approved, Rejected, Cancelled

    [NotMapped]
    public List<ApprovalStep> Steps { get; set; } = [];

    [NotMapped]
    public string StatusLabel => Status switch
    {
        "Approved"  => "Onaylandı",
        "Rejected"  => "Reddedildi",
        "Cancelled" => "İptal",
        _           => "Bekliyor"
    };

    [NotMapped]
    public string StatusClass => Status switch
    {
        "Approved"  => "yi-badge-success",
        "Rejected"  => "yi-badge-danger",
        "Cancelled" => "yi-badge-light",
        _           => "yi-badge-warning"
    };
}

public class ApprovalStep : BaseEntity
{
    public int     ApprovalRequestId { get; set; }
    public int     StepOrder         { get; set; }
    public int     ApproverId        { get; set; }
    public string  ApproverName      { get; set; } = string.Empty;
    public string  Status            { get; set; } = "Pending";
    public string? Comment           { get; set; }
    public DateTime? ActionAt        { get; set; }

    [NotMapped]
    public string StatusLabel => Status switch
    {
        "Approved" => "Onaylandı",
        "Rejected" => "Reddedildi",
        _ => "Bekliyor"
    };
}
