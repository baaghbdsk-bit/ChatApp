using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

/// <summary>
/// 📚 REST API: Users Resource
/// Endpoints for user profile management.
/// </summary>
[ApiController]
[Route("api/[controller]")] // Route: /api/users
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get current user's profile (from JWT token)
    /// </summary>
    /// <returns>User profile info</returns>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // In real app, get user ID from JWT token claims
        // For now, we'll use a placeholder
        var userId = GetUserIdFromRequest();
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var dto = new UserDto(
            Id: user.Id,
            PhoneNumber: user.PhoneNumber,
            DisplayName: user.DisplayName,
            ProfilePicUrl: user.ProfilePicUrl,
            LastSeen: user.LastSeen,
            IsOnline: user.IsOnline
        );

        return Ok(dto);
    }

    /// <summary>
    /// Update user's display name
    /// </summary>
    /// <param name="request">New display name</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromRequest();
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.DisplayName = request.DisplayName;
        await _userRepository.UpdateAsync(user);

        var dto = new UserDto(
            Id: user.Id,
            PhoneNumber: user.PhoneNumber,
            DisplayName: user.DisplayName,
            ProfilePicUrl: user.ProfilePicUrl,
            LastSeen: user.LastSeen,
            IsOnline: user.IsOnline
        );

        return Ok(dto);
    }

    /// <summary>
    /// Search for users by phone number (partial match)
    /// </summary>
    /// <param name="phone">Phone number to search for</param>
    /// <returns>List of matching users</returns>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(new { error = "Phone query parameter is required" });
        }

        var users = await _userRepository.SearchByPhoneAsync(phone);
        
        var dtos = users.Select(u => new UserDto(
            Id: u.Id,
            PhoneNumber: u.PhoneNumber,
            DisplayName: u.DisplayName,
            ProfilePicUrl: u.ProfilePicUrl,
            LastSeen: u.LastSeen,
            IsOnline: u.IsOnline
        )).ToList();

        return Ok(dtos);
    }

    // 📚 HLD LESSON: In production, extract this to a middleware/service
    // that reads from JWT token claims
    private Guid GetUserIdFromRequest()
    {
        // This is a placeholder — in real app, extract from JWT
        // For testing, we'll use a default user
        return Guid.Parse("11111111-1111-1111-1111-111111111111"); // Alice
    }
}
