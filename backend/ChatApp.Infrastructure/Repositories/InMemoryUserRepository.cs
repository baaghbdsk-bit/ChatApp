using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;

namespace ChatApp.Infrastructure.Repositories;

/// <summary>
/// 📚 HLD LESSON: In-Memory Repository (Fake Data Layer)
/// ──────────────────────────────────────────────────────
/// This is a "Test Double" — specifically a "Fake."
/// It implements the SAME interface as the real DB repository will,
/// but uses a simple List<T> instead of SQL Server.
///
/// WHY start with fakes?
/// 1. SPEED: No DB setup, no connection strings, no migrations.
/// 2. FOCUS: We can build and test the API layer first.
/// 3. PROVING THE ARCHITECTURE: If our interfaces are well-designed,
///    swapping fakes for real repos should be a one-line DI change.
///
/// ⚠️ LIMITATIONS (good to understand):
/// - Data is lost on app restart (no persistence).
/// - No real transactions or concurrency handling.
/// - ConcurrentDictionary gives thread-safety but NOT atomicity.
/// - No query optimization (we scan everything in memory).
/// These are ALL things a real database handles for you.
///
/// 📚 PATTERN: Repository Pattern
/// - Abstracts data access behind a clean interface.
/// - Controller says "give me user by phone" — doesn't know if it's
///   coming from SQL, Redis, a file, or thin air. That's the power.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    // 📚 ConcurrentDictionary: Thread-safe dictionary.
    // In a web app, multiple requests hit simultaneously (different threads).
    // A normal Dictionary would corrupt data under concurrent access.
    private static readonly Dictionary<Guid, User> _users = new();
    
    // Seed some fake users so we have data to work with immediately
    static InMemoryUserRepository()
    {
        var user1 = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PhoneNumber = "+1234567890",
            DisplayName = "Alice",
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        var user2 = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PhoneNumber = "+0987654321",
            DisplayName = "Bob",
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddMinutes(-45),
            CreatedAt = DateTime.UtcNow.AddDays(-25)
        };
        var user3 = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PhoneNumber = "+1122334455",
            DisplayName = "Charlie",
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _users[user1.Id] = user1;
        _users[user2.Id] = user2;
        _users[user3.Id] = user3;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByPhoneAsync(string phoneNumber)
    {
        var user = _users.Values.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<List<User>> SearchByPhoneAsync(string phoneNumberPartial)
    {
        var results = _users.Values
            .Where(u => u.PhoneNumber.Contains(phoneNumberPartial))
            .ToList();
        return Task.FromResult(results);
    }
}
