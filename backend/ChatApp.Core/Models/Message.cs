namespace ChatApp.Core.Models;

/// <summary>
/// 📚 LLD LESSON: Message Entity Design
/// ------------------------------------------
/// KEY DESIGN DECISIONS:
///
/// 1. WHY ConversationId (not ReceiverId)?
///    → Messages belong to a Conversation, not directly to a receiver.
///    → This scales to group chats — one message, many receivers.
///    → The Conversation already knows who the participants are.
///
/// 2. WHY separate SentAt, DeliveredAt, ReadAt?
///    → These represent the message lifecycle (WhatsApp's single/double/blue ticks).
///    → SentAt: when message was created (never null).
///    → DeliveredAt: when the OTHER user's device received it (nullable — not delivered yet).
///    → ReadAt: when the other user opened the chat (nullable — not read yet).
///    → This is a State Machine: Sent → Delivered → Read (one-way transitions).
///
/// 3. WHY MessageType enum?
///    → Extensibility. Right now it's just Text, but later:
///      Image, Video, Audio, Document, Location, Contact.
///    → The frontend renders differently based on type.
///    → Much cleaner than having nullable ImageUrl, VideoUrl, etc. fields.
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// 📚 LLD LESSON: Enums for Fixed Categories
/// Only use enums when the set of values is small, stable, and known at compile time.
/// For things that change often (like "tags"), use a separate table/list instead.
/// </summary>
public enum MessageType
{
    Text = 0,
    // Future: Image = 1, Video = 2, Audio = 3, Document = 4
}
