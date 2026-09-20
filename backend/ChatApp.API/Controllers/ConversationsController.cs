using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;
using ChatApp.API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Controllers;

/// <summary>
/// REST API: Conversations Resource
/// Endpoints for managing chat conversations.
/// </summary>
[ApiController]
[Route("api/[controller]")] // Route: /api/conversations
public class ConversationsController : ControllerBase
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IUserRepository _userRepository;
    private readonly IHubContext<ChatHub> _chatHub;

    public ConversationsController(
        IConversationRepository conversationRepo, 
        IMessageRepository messageRepo,
        IUserRepository userRepository,
        IHubContext<ChatHub> chatHub)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _userRepository = userRepository;
        _chatHub = chatHub;
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    /// <returns>List of conversations with last message preview</returns>
    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetUserIdFromRequest();
        
        var conversations = await _conversationRepo.GetByUserIdAsync(userId);
        
        var dtos = new List<ConversationDto>();
        
        foreach (var conv in conversations)
        {
            var otherParticipant = conv.Participants.FirstOrDefault(p => p.Id != userId);
            if (otherParticipant == null) continue;

            var lastMessage = conv.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            var unreadCount = await _messageRepo.GetUnreadCountAsync(conv.Id, userId);

            var dto = new ConversationDto(
                Id: conv.Id,
                OtherParticipant: MapToUserDto(otherParticipant),
                LastMessage: lastMessage != null ? new MessageDto(
                    Id: lastMessage.Id,
                    ConversationId: lastMessage.ConversationId,
                    SenderId: lastMessage.SenderId,
                    Content: lastMessage.Content,
                    Type: lastMessage.Type.ToString(),
                    SentAt: lastMessage.SentAt,
                    DeliveredAt: lastMessage.DeliveredAt,
                    ReadAt: lastMessage.ReadAt
                ) : null,
                UnreadCount: unreadCount,
                CreatedAt: conv.CreatedAt
            );

            dtos.Add(dto);
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Create a new 1:1 conversation with another user (by phone)
    /// </summary>
    /// <param name="request">Phone number of the other user</param>
    /// <returns>The new conversation</returns>
    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var userId = GetUserIdFromRequest();
        var targetUser = await _userRepository.GetByPhoneAsync(request.PhoneNumber);
        
        if (targetUser == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var existingConv = await _conversationRepo.GetByParticipantsAsync(userId, targetUser.Id);
        if (existingConv != null)
        {
            return Ok(MapToConversationDto(existingConv, userId));
        }

        var newConv = new Conversation
        {
            Participants = new List<User> 
            { 
                await _userRepository.GetByIdAsync(userId),
                targetUser 
            }
        };
        
        var created = await _conversationRepo.CreateAsync(newConv);
        await _chatHub.Clients
            .Group(ChatHub.GetUserGroupName(targetUser.Id))
            .SendAsync("ConversationCreated", MapToConversationDto(created, targetUser.Id));
        return Ok(MapToConversationDto(created, userId));
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            PhoneNumber: user.PhoneNumber,
            DisplayName: user.DisplayName,
            ProfilePicUrl: user.ProfilePicUrl,
            LastSeen: user.LastSeen,
            IsOnline: user.IsOnline
        );
    }

    private ConversationDto MapToConversationDto(Conversation conv, Guid userId)
    {
        var otherParticipant = conv.Participants.FirstOrDefault(p => p.Id != userId);
        var lastMessage = conv.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
        
        return new ConversationDto(
            Id: conv.Id,
            OtherParticipant: MapToUserDto(otherParticipant),
            LastMessage: lastMessage != null ? new MessageDto(
                Id: lastMessage.Id,
                ConversationId: lastMessage.ConversationId,
                SenderId: lastMessage.SenderId,
                Content: lastMessage.Content,
                Type: lastMessage.Type.ToString(),
                SentAt: lastMessage.SentAt,
                DeliveredAt: lastMessage.DeliveredAt,
                ReadAt: lastMessage.ReadAt
            ) : null,
            UnreadCount: 0,
            CreatedAt: conv.CreatedAt
        );
    }

    private Guid GetUserIdFromRequest()
    {
        return Request.Headers.TryGetValue("X-User-Id", out var rawUserId) &&
               Guid.TryParse(rawUserId, out var userId)
            ? userId
            : Guid.Empty;
    }
}
