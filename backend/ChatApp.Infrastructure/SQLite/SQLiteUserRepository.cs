using ChatApp.Core.Interfaces;
using ChatApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.SQLite;

public class SQLiteUserRepository : IUserRepository
{
    private readonly ChatAppDbContext _context;

    public SQLiteUserRepository(ChatAppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByPhoneAsync(string phoneNumber)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> SearchByPhoneAsync(string phoneNumberPartial)
    {
        return await _context.Users
            .Where(u => u.PhoneNumber.Contains(phoneNumberPartial))
            .ToListAsync();
    }
}
