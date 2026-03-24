namespace YonetIQ.Data.Models;

public class TaskComment : BaseEntity
{
    public int    TaskItemId { get; set; }
    public int    UserId     { get; set; }
    public string UserName   { get; set; } = string.Empty;
    public string Content    { get; set; } = string.Empty;
}
