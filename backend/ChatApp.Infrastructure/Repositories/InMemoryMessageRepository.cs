using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;

namespace ChatApp.Infrastructure.Repositories;

/// <summary>
/// 📚 LLD LESSON: Cursor-Based Pagination Implementation
/// ─────────────────────────────────────────────────────
/// This is where we implement the cursor-based pagination design
/// we discussed in the interface. No offset/skip — we filter by
/// the SentAt timestamp.
///
/// 📚 PERFORMANCE TIP (real-world):
/// In SQL, you'd create an index on (ConversationId, SentAt DESC)
/// so this query runs in O(log N) instead of O(N).
/// </summary>
public class InMemoryMessageRepository : IMessageRepository
{
    private static readonly Dictionary<Guid, Message> _messages = new();
    private readonly InMemoryConversationRepository _conversationRepo;

    public InMemoryMessageRepository(InMemoryConversationRepository conversationRepo)
    {
        _conversationRepo = conversationRepo;
    }

    public async Task<List<Message>> GetByConversationIdAsync(Guid conversationId, DateTime? before = null, int take = 50)
    {
        var conversation = await _conversationRepo.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            return new List<Message>();
        }

        // Messages are owned by this repository, so reads must use the same store
        // that CreateAsync writes to.
        var messages = _messages.Values
            .Where(m => m.ConversationId == conversationId);
            
        // Apply cursor-based pagination (before timestamp)
        if (before.HasValue)
        {
            messages = messages.Where(m => m.SentAt < before.Value);
        }
        
        // Sort by newest first, take limit
        return messages
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .OrderBy(m => m.SentAt)  // Re-sort chronologically
            .ToList();
    }

    public Task<Message> CreateAsync(Message message)
    {
        message.Id = Guid.NewGuid();
        message.SentAt = DateTime.UtcNow;
        _messages[message.Id] = message;
        return Task.FromResult(message);
    }

    public Task MarkAsReadAsync(Guid conversationId, Guid userId)
    {
        // Find unread messages from OTHER participants in this conversation
        var messagesToUpdate = _messages.Values
            .Where(m => m.ConversationId == conversationId &&
                        m.ReadAt == null &&
                        m.SenderId != userId)  // Don't mark our own messages as read
            .ToList();

        foreach (var msg in messagesToUpdate)
        {
            msg.ReadAt = DateTime.UtcNow;
            _messages[msg.Id] = msg;
        }
        return Task.CompletedTask;
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        // Count messages in this conversation where:
        // 1. The current user is NOT the sender (other participant's messages)
        // 2. The message has NOT been read yet
        var conversation = await _conversationRepo.GetByIdAsync(conversationId);
        if (conversation == null) return 0;

        return _messages.Values.Count(m =>
            m.ConversationId == conversationId &&
            m.SenderId != userId &&
            m.ReadAt == null);
    }
}
