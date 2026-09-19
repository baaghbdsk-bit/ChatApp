using ChatApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.SQLite;

/// <summary>
/// 📚 SQLite Database Context
/// ─────────────────────────────
/// This is the EF Core DbContext that connects to SQLite.
/// </summary>
public class ChatAppDbContext : DbContext
{
    private readonly string _connectionString;

    public ChatAppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Conversation> Conversations { get; set; } = default!;
    public DbSet<Message> Messages { get; set; } = default!;
    public DbSet<OtpRecord> OtpRecords { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.ProfilePicUrl).HasMaxLength(500);
            entity.Property(u => u.IsOnline).HasDefaultValue(false);
            entity.Property(u => u.LastSeen).HasDefaultValueSql("datetime('now')");
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.HasIndex(u => u.PhoneNumber).IsUnique();
        });

        // Conversation entity
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Message entity - simple configuration without navigation properties
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedNever();
            entity.Property(m => m.SentAt).HasDefaultValueSql("datetime('now')");
            entity.Property(m => m.ConversationId).IsRequired();
            entity.Property(m => m.SenderId).IsRequired();
        });

        // OtpRecord entity
        modelBuilder.Entity<OtpRecord>(entity =>
        {
            entity.ToTable("OtpRecords");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).ValueGeneratedNever();
            entity.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Code).IsRequired().HasMaxLength(6);
            entity.Property(o => o.ExpiresAt).IsRequired();
            entity.Property(o => o.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.HasIndex(o => o.PhoneNumber);
            entity.HasIndex(o => o.Code);
        });
    }
}
