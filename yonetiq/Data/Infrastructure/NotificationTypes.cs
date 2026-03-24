namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Uygulama içi bildirim tip sabitleri.
/// Notifications.Type kolonu için kullanılır — enum değil, plain string.
/// </summary>
public static class NotificationTypes
{
    /// <summary>Kullanıcıya yeni bir görev atandı.</summary>
    public const string TaskAssigned = "TaskAssigned";

    /// <summary>Kullanıcıya yeni bir mesaj gönderildi.</summary>
    public const string NewMessage = "NewMessage";

    /// <summary>Kişisel not hatırlatması tetiklendi.</summary>
    public const string NoteReminder = "NoteReminder";
}
