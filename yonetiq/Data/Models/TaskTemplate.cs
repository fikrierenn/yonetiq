using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

public class TaskTemplate : BaseEntity
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category    { get; set; }
    public bool    IsActive    { get; set; } = true;
    public int     CreatedBy   { get; set; }

    [NotMapped]
    public List<TaskTemplateItem> Items { get; set; } = [];
}

public class TaskTemplateItem : BaseEntity
{
    public int     TaskTemplateId { get; set; }
    public string  Title          { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public string? Priority       { get; set; }
    public int?    DueDays        { get; set; }
    public int     OrderIndex     { get; set; }
}
