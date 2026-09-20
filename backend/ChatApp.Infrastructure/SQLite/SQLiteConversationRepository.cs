using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.SQLite;

public class SQLiteConversationRepository : IConversationRepository
{
    private readonly ChatAppDbContext _context;

    public SQLiteConversationRepository(ChatAppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id);
        return await IncludeMessagesAsync(conversation);
    }

    public async Task<List<Conversation>> GetByUserIdAsync(Guid userId)
    {
        var conversations = await _context.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(u => u.Id == userId))
            .ToListAsync();
        foreach (var conversation in conversations)
        {
            await IncludeMessagesAsync(conversation);
        }
        return conversations;
    }

    public async Task<Conversation?> GetByParticipantsAsync(Guid userId1, Guid userId2)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.Participants.Any(u => u.Id == userId1) &&
                c.Participants.Any(u => u.Id == userId2));
        return await IncludeMessagesAsync(conversation);
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        conversation.Id = Guid.NewGuid();
        conversation.CreatedAt = DateTime.UtcNow;
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    private async Task<Conversation?> IncludeMessagesAsync(Conversation? conversation)
    {
        if (conversation != null)
        {
            conversation.Messages = await _context.Messages
                .Where(message => message.ConversationId == conversation.Id)
                .OrderBy(message => message.SentAt)
                .ToListAsync();
        }

        return conversation;
    }
}
