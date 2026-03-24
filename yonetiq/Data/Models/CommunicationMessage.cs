using System;
using System.Collections.Generic;

namespace YonetIQ.Data.Models;

/// <summary>
/// Şirket içi yazışma sistemindeki her bir mesajı veya sistem bildirimini temsil eden modeldir.
/// </summary>
public class CommunicationMessage
{
    /// <summary>
    /// Mesajın benzersiz kimliği.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Mesajı gönderen kullanıcının ID'si. Eğer sistem gönderdiyse null olabilir.
    /// </summary>
    public int? SenderUserId { get; set; }

    /// <summary>
    /// Mesajın alıcısı olan kullanıcının ID'si (Özel mesaj için).
    /// </summary>
    public int? ReceiverUserId { get; set; }

    /// <summary>
    /// Mesajın alıcısı olan departmanın TanimId'si (Grup mesajı için).
    /// </summary>
    public int? ReceiverDepartmentId { get; set; }

    /// <summary>
    /// Mesajın içeriği.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Mesajın tipi (Kullanıcı Mesajı, Sistem Bildirimi, Görev Hatırlatıcısı vb.).
    /// </summary>
    public MessageType Type { get; set; } = MessageType.UserMessage;

    /// <summary>
    /// Mesajın oluşturulma tarihi.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Mesajın okunduğuna dair bilgi. (Grup mesajlarında farklı mantık gerekebilir)
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Mesaja iliştirilen nesnenin tipi (Rapor, Görev, Toplantı vb.).
    /// </summary>
    public string? AttachmentType { get; set; }

    /// <summary>
    /// Mesaja iliştirilen nesnenın ID'si.
    /// </summary>
    public int? AttachmentId { get; set; }
}

/// <summary>
/// İletişim mesajının türünü belirler.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Kullanıcılar arası manuel yazışma.
    /// </summary>
    UserMessage = 1,

    /// <summary>
    /// Sistem tarafından otomatik üretilen bilgilendirme.
    /// </summary>
    SystemNotification = 2,

    /// <summary>
    /// Otomatik görev hatırlatması.
    /// </summary>
    TaskReminder = 3,

    /// <summary>
    /// Toplantı daveti veya değişikliği.
    /// </summary>
    MeetingInfo = 4
}
