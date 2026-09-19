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
        return await _context.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Conversation>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(u => u.Id == userId))
            .ToListAsync();
    }

    public async Task<Conversation?> GetByParticipantsAsync(Guid userId1, Guid userId2)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                c.Participants.Any(u => u.Id == userId1) &&
                c.Participants.Any(u => u.Id == userId2));
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        conversation.Id = Guid.NewGuid();
        conversation.CreatedAt = DateTime.UtcNow;
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }
}
