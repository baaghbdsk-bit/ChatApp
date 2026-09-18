namespace ChatApp.Core.Models;

/// <summary>
/// 📚 LLD LESSON: OTP as a Short-Lived Entity
/// ------------------------------------------
/// This is a "temporal" entity — it only matters for a few minutes, then expires.
///
/// WHY store OTP in data store (not just send & forget)?
/// → We need to VERIFY it when the user submits. Without storing, we can't compare.
/// → Alternative: Stateless OTP using HMAC/TOTP (like Google Authenticator).
///   But that's more complex and unnecessary for our learning project.
///
/// SECURITY NOTES (real-world):
/// → Hash the OTP code (don't store plaintext) — same reason you hash passwords.
/// → Rate limit: max 3 verification attempts per OTP.
/// → Cooldown: max 1 OTP per phone per 60 seconds (prevent spam).
/// → We skip these for learning, but they're critical in production.
/// </summary>
public class OtpRecord
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}
