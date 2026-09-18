using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;

namespace ChatApp.Infrastructure.Repositories;

/// <summary>
/// 📚 LLD LESSON: In-Memory Conversation Repository
/// Notice how this uses the User repository? That's dependency composition.
/// The "fake" layers can depend on each other, just like real layers would.
/// </summary>
public class InMemoryConversationRepository : IConversationRepository
{
    private static readonly Dictionary<Guid, Conversation> _conversations = new();
    private readonly InMemoryUserRepository _userRepository;

    public InMemoryConversationRepository(InMemoryUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Conversation?> GetByIdAsync(Guid id)
    {
        _conversations.TryGetValue(id, out var conv);
        return Task.FromResult(conv);
    }

    public Task<List<Conversation>> GetByUserIdAsync(Guid userId)
    {
        var conversations = new List<Conversation>();
        foreach (var conv in _conversations.Values)
        {
            if (conv.Participants.Any(p => p.Id == userId))
            {
                conversations.Add(CloneConversation(conv));
            }
        }
        return Task.FromResult(conversations);
    }

    public Task<Conversation?> GetByParticipantsAsync(Guid userId1, Guid userId2)
    {
        foreach (var conv in _conversations.Values)
        {
            if (conv.Participants.Any(p => p.Id == userId1) &&
                conv.Participants.Any(p => p.Id == userId2))
            {
                return Task.FromResult<Conversation?>(CloneConversation(conv));
            }
        }
        return Task.FromResult<Conversation?>(null);
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        conversation.Id = Guid.NewGuid();
        conversation.CreatedAt = DateTime.UtcNow;
        
        var participants = new List<User>();
        foreach (var user in conversation.Participants)
        {
            var existingUser = await _userRepository.GetByIdAsync(user.Id) ??
                               await _userRepository.GetByPhoneAsync(user.PhoneNumber);
            if (existingUser != null)
            {
                participants.Add(existingUser);
            }
            else
            {
                var newUser = await _userRepository.CreateAsync(user);
                participants.Add(newUser);
            }
        }
        conversation.Participants = participants;
        _conversations[conversation.Id] = conversation;
        return conversation;
    }

    private Conversation CloneConversation(Conversation conv)
    {
        return new Conversation
        {
            Id = conv.Id,
            CreatedAt = conv.CreatedAt,
            Participants = new List<User>(conv.Participants),
            Messages = new List<Message>(conv.Messages)
        };
    }
}