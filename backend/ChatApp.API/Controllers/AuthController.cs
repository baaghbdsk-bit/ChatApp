using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

/// <summary>
/// 📚 HLD LESSON: REST API Design
/// ────────────────────────────────
/// This is the "Controller Layer" — the entry point for HTTP requests.
///
/// WHAT IS AN ENDPOINT?
/// An endpoint is a URL + HTTP method combination:
///   POST /api/auth/send-otp
///   POST /api/auth/verify-otp
///
/// The REST pattern: Resources → Endpoints
/// - User resource: /api/users
/// - Conversation resource: /api/conversations
/// - Message resource: /api/conversations/{id}/messages
///
/// 📚 LLD LESSON: Controller Responsibilities
/// Controllers should ONLY:
/// 1. Validate incoming request (DTO binding, model state)
/// 2. Call service layer (business logic)
/// 3. Map response DTOs to HTTP responses
/// 
/// NO business logic in controllers — that goes in Services/Handlers.
/// This keeps controllers thin and testable.
/// </summary>
[ApiController]
[Route("api/[controller]")] // Route: /api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Request OTP code for phone number verification
    /// </summary>
    /// <param name="request">Phone number to send OTP to</param>
    /// <returns>200 OK if OTP sent (or already exists)</returns>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { error = "Phone number is required" });
        }

        // Call service
        var success = await _authService.SendOtpAsync(request.PhoneNumber);

        if (!success)
        {
            return BadRequest(new { error = "Invalid phone number" });
        }

        // Return success (don't reveal if user exists or not — security)
        return Ok(new { message = "OTP sent successfully (logged to console)" });
    }

    /// <summary>
    /// Verify OTP and get authentication token
    /// </summary>
    /// <param name="request">Phone number + OTP code</param>
    /// <returns>AuthResponse with JWT token and user info</returns>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { error = "Phone number and OTP code are required" });
        }

        // Call service
        var authResponse = await _authService.VerifyOtpAsync(request.PhoneNumber, request.Code);

        if (authResponse == null)
        {
            return BadRequest(new { error = "Invalid or expired OTP" });
        }

        return Ok(authResponse);
    }
}
