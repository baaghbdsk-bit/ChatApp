using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Hubs;

/// <summary>
/// 📚 LLD LESSON: SignalR Hub for Real-Time Chat
/// ─────────────────────────────────────────────────
/// This is the "Hub" class — SignalR's server-side entry point
/// for WebSocket connections.
///
/// WHAT IS A HUB?
/// - A hub is a high-level pipeline that handles:
///   1. Client connections/disconnections (Hub OnConnected/OnDisconnected)
///   2. Method calls from clients (SendMessage, MarkAsRead, etc.)
///   3. Broadcasting messages to connected clients
///
/// 📚 HLD LESSON: Real-Time vs REST
/// - REST: Client polls server for updates (inefficient)
/// - SignalR: Server pushes updates to clients (real-time)
///   → Like WhatsApp: messages appear instantly, no refresh needed
///
/// HOW IT WORKS:
/// 1. Client connects to /hubs/chat endpoint (WebSocket)
/// 2. Server stores connectionId → userId mapping
/// 3. When user sends message, Hub broadcasts to other participants
/// 4. All connected clients receive real-time updates
/// </summary>
public class ChatHub : Hub
{
    // 📚 LLD LESSON: In-memory store for connections
    // In production, use Redis backplane for multi-server scale
    private static readonly Dictionary<string, Guid> _connections = new();
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IUserRepository _userRepository;

    public ChatHub(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IUserRepository userRepository)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Get user ID from connection
        var userId = GetUserIdFromConnection();
        
        // Store connection mapping
        _connections[Context.ConnectionId] = userId;
        
        // Notify user about existing conversations
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove connection mapping
        _connections.Remove(Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client calls this to send a message
    /// </summary>
    /// <param name="conversationId">Conversation to send in</param>
    /// <param name="content">Message content</param>
    public async Task SendMessage(Guid conversationId, string content)
    {
        // Get sender info
        var senderId = GetUserIdFromConnection();
        
        // Validate conversation exists and sender is participant
        var conversation = await _conversationRepo.GetByIdAsync(conversationId);
        if (conversation == null || 
            !conversation.Participants.Any(p => p.Id == senderId))
        {
            return; // Or throw exception
        }

        // Create message
        var message = new ChatApp.Core.Models.Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        // Persist message (in real app, this would be async)
        await _messageRepo.CreateAsync(message);

        // Get other participants
        var otherParticipants = conversation.Participants
            .Where(p => p.Id != senderId)
            .ToList();

        // Broadcast to each participant's group
        foreach (var participant in otherParticipants)
        {
            // Create a SignalR group per conversation
            var group = GetConversationGroupName(conversationId);
            await Clients.Group(group).SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                Type = message.Type.ToString(),
                SentAt = message.SentAt
            });
        }
    }

    /// <summary>
    /// Client calls this to mark messages as read
    /// </summary>
    /// <param name="conversationId">Conversation to mark as read</param>
    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = GetUserIdFromConnection();
        await _messageRepo.MarkAsReadAsync(conversationId, userId);

        // Notify sender that their messages were read
        var conversation = await _conversationRepo.GetByIdAsync(conversationId);
        if (conversation == null) return;

        // Find messages sent by this user that weren't read
        var myMessages = conversation.Messages
            .Where(m => m.SenderId == userId && m.ReadAt == null)
            .ToList();

        foreach (var participant in conversation.Participants.Where(p => p.Id != userId))
        {
            await Clients.User(participant.Id.ToString()).SendAsync("MessagesRead", new
            {
                ConversationId = conversationId,
                ReadAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Client calls this to indicate typing (optional feature)
    /// </summary>
    /// <param name="conversationId">Conversation where user is typing</param>
    public async Task Typing(Guid conversationId)
    {
        var userId = GetUserIdFromConnection();
        
        // Broadcast to other participants
        var otherParticipants = await GetOtherParticipants(conversationId, userId);
        foreach (var participant in otherParticipants)
        {
            await Clients.User(participant.Id.ToString()).SendAsync("UserTyping", new
            {
                UserId = userId,
                ConversationId = conversationId
            });
        }
    }

    // Helper methods

    private Guid GetUserIdFromConnection()
    {
        // In production, extract from JWT token claims
        // For now, use a default user (Alice)
        return Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    private string GetConversationGroupName(Guid conversationId)
    {
        return $"conversation-{conversationId}";
    }

    private async Task<List<ChatApp.Core.Models.User>> GetOtherParticipants(Guid conversationId, Guid currentUserId)
    {
        var conversation = await _conversationRepo.GetByIdAsync(conversationId);
        return conversation?.Participants.Where(p => p.Id != currentUserId).ToList() ?? new List<ChatApp.Core.Models.User>();
    }
}
