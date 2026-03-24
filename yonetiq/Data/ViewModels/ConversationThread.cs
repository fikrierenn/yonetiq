using System;
using System.Collections.Generic;
using System.Linq;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.ViewModels;

public class ConversationThread
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-chat-dots";
    
    public DateTime LastMessageAt => Messages.Any() ? Messages.Max(m => m.CreatedAt) : DateTime.MinValue;
    public int UnreadCount => Messages.Count(m => !m.IsRead && m.SenderUserId != _ownerUserId);
    
    public List<CommunicationMessage> Messages { get; set; } = new();

    public string? ContextAttachmentType { get; set; }
    public int? ContextAttachmentId { get; set; }
    public int? ContextDepartmentId { get; set; }
    public int? ContextOtherUserId { get; set; }

    private readonly int _ownerUserId;

    public ConversationThread(int ownerUserId)
    {
        _ownerUserId = ownerUserId;
    }
}
