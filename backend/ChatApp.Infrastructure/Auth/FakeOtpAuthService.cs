using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;
using ChatApp.Infrastructure.Repositories;

namespace ChatApp.Infrastructure.Auth;

/// <summary>
/// 📚 LLD LESSON: Fake OTP Service (No Real SMS)
/// ────────────────────────────────────────────────
/// This service implements the IAuthService interface.
///
/// REAL-WORLD IMPLEMENTATION WOULD USE:
/// - Twilio, Amazon SNS, or Firebase Auth to send real SMS
/// - These services give you a phone number + API key
/// - You'd call their SDK to send the OTP
///
/// WHY FAKE HERE?
/// 1. LEARNING FOCUS: We want to learn architecture, not API keys & billing.
/// 2. NO COST: Real SMS services cost money (even with free tier credits).
/// 3. NO SETUP: No need to configure external services.
///
/// HOW IT WORKS:
/// 1. User enters phone number → API generates 6-digit code
/// 2. Code is stored in DB with 10-minute expiry
/// 3. Code is LOGGED to console (NOT sent via SMS)
/// 4. User enters code → API validates & returns JWT token
///
/// This is called a "Fake" or "Mock" implementation — it follows the
/// same contract as the real thing, but without external dependencies.
/// </summary>
public class FakeOtpAuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private static readonly Dictionary<string, OtpRecord> _otpStore = new();

    public FakeOtpAuthService(IUserRepository userRepository, IMessageRepository messageRepository)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
    }

    public async Task<bool> SendOtpAsync(string phoneNumber)
    {
        // Demo-mode validation: allow any non-empty dummy phone number.
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 3)
        {
            return false;
        }

        // Check if user exists, create if not
        var user = await _userRepository.GetByPhoneAsync(phoneNumber);
        if (user == null)
        {
            user = await _userRepository.CreateAsync(new User
            {
                PhoneNumber = phoneNumber,
                DisplayName = phoneNumber,
                IsOnline = false,
                LastSeen = DateTime.UtcNow
            });
        }

        // Demo mode: generate a placeholder OTP, but any code will be accepted later.
        var code = "DEMO";

        // Store OTP with 10-minute expiry
        _otpStore[phoneNumber] = new OtpRecord
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        Console.WriteLine($"========================================");
        Console.WriteLine($"Demo OTP for {phoneNumber}: {code}");
        Console.WriteLine($"Any OTP value is accepted in demo mode");
        Console.WriteLine($"========================================");

        return true;
    }

    public async Task<AuthResponse?> VerifyOtpAsync(string phoneNumber, string code)
    {
        // Demo mode: accept any non-empty phone and OTP value for learning/testing.
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        // Check if OTP exists and not expired
        if (!_otpStore.TryGetValue(phoneNumber, out var otpRecord))
        {
            _otpStore[phoneNumber] = new OtpRecord
            {
                Id = Guid.NewGuid(),
                PhoneNumber = phoneNumber,
                Code = "DEMO",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            otpRecord = _otpStore[phoneNumber];
        }

        // Check expiry
        if (otpRecord.ExpiresAt < DateTime.UtcNow)
        {
            _otpStore.Remove(phoneNumber);
            return null;
        }

        // Demo mode: do not validate the code against the stored value.
        otpRecord.IsUsed = true;
        _otpStore[phoneNumber] = otpRecord;

        // Get user (should exist - created in SendOtpAsync)
        var user = await _userRepository.GetByPhoneAsync(phoneNumber);
        if (user == null)
        {
            return null;
        }

        // Update user's last seen and online status
        user.LastSeen = DateTime.UtcNow;
        user.IsOnline = true;
        await _userRepository.UpdateAsync(user);

        // Generate JWT token (simplified for learning — real JWT would use signing key)
        var token = GenerateJwtToken(user);

        // Return auth response
        return new AuthResponse(token, MapToUserDto(user));
    }

    private string GenerateJwtToken(User user)
    {
        // 📚 LLD LESSON: In production, use proper JWT signing!
        // This is just a placeholder for learning. Real implementation:
        // 1. Use JwtSecurityTokenHandler
        // 2. Create claims (user id, role, etc.)
        // 3. Sign with secret key from config
        // 4. Set expiration (e.g., 1 hour)
        
        // For now, we'll just return a placeholder string
        // that includes user info (still base64-encoded to look like JWT)
        var payload = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                $"{{\"userId\":\"{user.Id}\",\"phone\":\"{user.PhoneNumber}\",\"exp\":{(DateTime.UtcNow.AddHours(1).Ticks / 10000000L - 62135596800L)}}}"
            )
        );
        
        return $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{payload}.fake_signature";
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
}
