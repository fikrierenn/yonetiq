namespace YonetIQ.Data.Models;

public class TaskActivity : BaseEntity
{
    public int     TaskItemId  { get; set; }
    public int     UserId      { get; set; }
    public string  UserName    { get; set; } = string.Empty;
    public string  ActionType  { get; set; } = string.Empty;
    public string? OldValue    { get; set; }
    public string? NewValue    { get; set; }
    public string? Note        { get; set; }
}
