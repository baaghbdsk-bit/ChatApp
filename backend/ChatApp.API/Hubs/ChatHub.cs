using System.Collections.Concurrent;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, Guid> Connections = new();
    private readonly IConversationRepository conversationRepository;
    private readonly IMessageRepository messageRepository;

    public ChatHub(IConversationRepository conversationRepository, IMessageRepository messageRepository)
    {
        this.conversationRepository = conversationRepository;
        this.messageRepository = messageRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromConnection();
        if (userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        Connections[Context.ConnectionId] = userId;
    await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Connections.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserIdFromConnection();
        var conversation = await conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || !conversation.Participants.Any(participant => participant.Id == userId))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var senderId = GetUserIdFromConnection();
        var conversation = await conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || !conversation.Participants.Any(participant => participant.Id == senderId))
        {
            return;
        }

        var message = await messageRepository.CreateAsync(new ChatApp.Core.Models.Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow
        });

        await Clients.Group(GetConversationGroupName(conversationId)).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            Type = message.Type.ToString(),
            SentAt = message.SentAt
        });
    }

    public async Task MarkAsRead(Guid conversationId)
    {
        await messageRepository.MarkAsReadAsync(conversationId, GetUserIdFromConnection());
    }

    private Guid GetUserIdFromConnection()
    {
        var rawUserId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        return Guid.TryParse(rawUserId, out var userId) ? userId : Guid.Empty;
    }

    private static string GetConversationGroupName(Guid conversationId) => $"conversation-{conversationId}";

    public static string GetUserGroupName(Guid userId) => $"user-{userId}";
}
