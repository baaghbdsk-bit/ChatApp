namespace ChatApp.Core.Models;

/// <summary>
/// 📚 LLD LESSON: Conversation as a "Join Entity"
/// ------------------------------------------
/// A Conversation connects two Users. This is a classic Many-to-Many relationship:
///   - One User can have MANY Conversations  
///   - One Conversation has MANY Users (2 for 1:1 chat)
///
/// WHY a separate Conversation entity (instead of just "UserA sends to UserB")?
/// 1. Groups: If we add group chat later, this model already supports it.
/// 2. Metadata: We can store conversation-level data (created date, last message).
/// 3. Message ownership: Messages belong to a Conversation, not directly to users.
///    This is cleaner than storing both SenderId AND ReceiverId on every message.
///
/// 📚 HLD LESSON: This is the "Aggregate Root" pattern from DDD.
/// When you load a Conversation, you load its Messages — they're a unit.
/// You never have an orphan Message without a Conversation.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Navigation property — the Users in this conversation.
    /// For 1:1 chat this will always have exactly 2 participants.
    /// </summary>
    public List<User> Participants { get; set; } = new();
    
    /// <summary>
    /// Navigation property — all messages in this conversation.
    /// In a real DB, you'd NEVER load all messages at once (use pagination).
    /// But for our in-memory fake data, this is fine.
    /// </summary>
    public List<Message> Messages { get; set; } = new();
}
