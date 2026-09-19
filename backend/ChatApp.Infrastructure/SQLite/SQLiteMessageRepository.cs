using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.SQLite;

public class SQLiteMessageRepository : IMessageRepository
{
    private readonly ChatAppDbContext _context;

    public SQLiteMessageRepository(ChatAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Message>> GetByConversationIdAsync(Guid conversationId, DateTime? before = null, int take = 50)
    {
        IQueryable<Message> query = _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt);

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        return await query
            .Take(take)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        message.Id = Guid.NewGuid();
        message.SentAt = DateTime.UtcNow;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid userId)
    {
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId 
                && m.SenderId != userId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.ReadAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ConversationId == conversationId 
                && m.SenderId != userId 
                && m.ReadAt == null);
    }
}
