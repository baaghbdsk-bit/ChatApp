namespace ChatApp.Core.Models;

/// <summary>
/// 📚 LLD LESSON: Entity Design
/// ------------------------------------------
/// This is an "Entity" — it has a unique identity (Id) that persists over time.
/// Even if a user changes their display name, they're still the SAME user.
/// This is different from a "Value Object" (like an Address) where identity
/// doesn't matter — two addresses with the same fields ARE the same.
///
/// WHY Guid for Id?
/// - Globally unique — no collisions even across distributed systems.
/// - Can be generated client-side (no DB roundtrip needed).
/// - Better for microservices (each service can generate its own IDs).
/// - Contrast with int/auto-increment: simpler, but creates DB dependency
///   and leaks info (user count, creation order).
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePicUrl { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; }
}
