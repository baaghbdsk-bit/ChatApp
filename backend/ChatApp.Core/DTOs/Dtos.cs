namespace ChatApp.Core.DTOs;

/// <summary>
/// 📚 LLD LESSON: DTOs vs Entities — WHY separate classes?
/// ------------------------------------------
/// NEVER expose your database entities directly in API responses. Why?
///
/// 1. SECURITY: Your User entity has fields you don't want to expose
///    (e.g., OTP records, internal IDs, password hashes in other apps).
///
/// 2. DECOUPLING: If you change your DB schema (rename a column),
///    your API contract doesn't break. DTO stays the same.
///
/// 3. SHAPING: API consumers need different data than what's in the DB.
///    Example: ConversationDto includes "LastMessage" — but that's not a
///    column on the Conversation table, it's computed from the Messages table.
///
/// 4. PERFORMANCE: You control exactly which fields travel over the network.
///    No accidental "SELECT *" of massive data.
///
/// The pattern: Entity (DB) → DTO (API) → ViewModel (Frontend)
/// Each layer has its own representation of the data.
/// </summary>

// ─── Auth DTOs ───────────────────────────────────────────

public record SendOtpRequest(string PhoneNumber);

public record VerifyOtpRequest(string PhoneNumber, string Code);

public record AuthResponse(string Token, UserDto User);

// ─── User DTOs ───────────────────────────────────────────

public record UserDto(
    Guid Id,
    string PhoneNumber,
    string DisplayName,
    string? ProfilePicUrl,
    DateTime LastSeen,
    bool IsOnline
);

public record UpdateProfileRequest(string DisplayName);

// ─── Conversation DTOs ──────────────────────────────────

public record ConversationDto(
    Guid Id,
    UserDto OtherParticipant,
    MessageDto? LastMessage,
    int UnreadCount,
    DateTime CreatedAt
);

public record CreateConversationRequest(string PhoneNumber);

// ─── Message DTOs ───────────────────────────────────────

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    string Type,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt
);

public record SendMessageRequest(string Content);

/// <summary>
/// 📚 LLD LESSON: C# Records
/// ------------------------------------------
/// We use "record" instead of "class" for DTOs because:
/// - Records are IMMUTABLE by default (once created, can't be changed).
/// - Records have VALUE equality (two DTOs with same data ARE equal).
/// - Records have built-in ToString() that shows all properties.
/// - Perfect for DTOs — they're just data containers, no behavior.
///
/// Classes would work too, but records express intent: "this is pure data."
/// </summary>
