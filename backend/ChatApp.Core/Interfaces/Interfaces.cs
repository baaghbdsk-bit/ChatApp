using ChatApp.Core.DTOs;
using ChatApp.Core.Models;

namespace ChatApp.Core.Interfaces;

/// <summary>
/// 📚 HLD LESSON: Dependency Inversion Principle (DIP) — the "D" in SOLID
/// ──────────────────────────────────────────────────────────────────────
/// WHY interfaces in Core (not in Infrastructure)?
///
/// Think of it as a CONTRACT. Core says "I need something that can do X"
/// and Infrastructure says "I can do X using SQL Server / in-memory / Redis."
///
/// The DIRECTION of dependency matters:
///   ❌ Wrong:  API → Core → Infrastructure (Core depends on DB)
///   ✅ Right:  API → Core ← Infrastructure (both depend on Core's contracts)
///
/// This means we can:
/// 1. Swap fake data for real DB WITHOUT changing Core or API code.
/// 2. Unit test with mock implementations.
/// 3. Run without a database (exactly what we're doing now!).
///
///          ┌─────────┐
///          │   API   │ ← Calls interfaces
///          └────┬────┘
///               │ depends on
///          ┌────▼────┐
///          │  Core   │ ← Defines interfaces (contracts)
///          └────▲────┘
///               │ implements
///      ┌────────┴────────┐
///      │ Infrastructure  │ ← Provides real implementations
///      └─────────────────┘
///
/// This is also called "Clean Architecture" or "Onion Architecture."
/// </summary>

public interface IAuthService
{
    /// <summary>Generate and "send" an OTP to the phone number.</summary>
    Task<bool> SendOtpAsync(string phoneNumber);
    
    /// <summary>Verify the OTP and return a JWT token + user info if valid.</summary>
    Task<AuthResponse?> VerifyOtpAsync(string phoneNumber, string code);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByPhoneAsync(string phoneNumber);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<List<User>> SearchByPhoneAsync(string phoneNumberPartial);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<List<Conversation>> GetByUserIdAsync(Guid userId);
    Task<Conversation?> GetByParticipantsAsync(Guid userId1, Guid userId2);
    Task<Conversation> CreateAsync(Conversation conversation);
}

public interface IMessageRepository
{
    /// <summary>
    /// Get messages with cursor-based pagination.
    /// 
    /// 📚 LLD LESSON: Cursor vs Offset Pagination
    /// ─────────────────────────────────────────────
    /// OFFSET (SKIP/TAKE): "Give me page 5 (skip 40, take 10)"
    ///   → Problem: If 5 new messages arrive, page 5 shifts. You see duplicates!
    ///   → Also slow on large tables (DB must count through all skipped rows).
    ///
    /// CURSOR (KEYSET): "Give me 10 messages BEFORE this timestamp"  
    ///   → Stable: New messages don't affect which old messages you see.
    ///   → Fast: DB uses index to jump directly to the cursor position.
    ///   → WhatsApp, Twitter, Discord all use cursor pagination.
    ///
    /// The "before" parameter is the cursor — the SentAt of the last message
    /// the client already has. We return messages older than that.
    /// </summary>
    Task<List<Message>> GetByConversationIdAsync(Guid conversationId, DateTime? before = null, int take = 50);
    
    Task<Message> CreateAsync(Message message);
    Task MarkAsReadAsync(Guid conversationId, Guid userId);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
}
