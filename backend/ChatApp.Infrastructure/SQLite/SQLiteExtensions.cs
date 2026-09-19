using ChatApp.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Infrastructure.SQLite;

public static class SQLiteExtensions
{
    public static IServiceCollection AddSQLiteRepositories(this IServiceCollection services, string databasePath)
    {
        services.AddScoped(sp => new ChatAppDbContext($"Data Source={databasePath}"));
        services.AddScoped<IUserRepository, SQLiteUserRepository>();
        services.AddScoped<IConversationRepository, SQLiteConversationRepository>();
        services.AddScoped<IMessageRepository, SQLiteMessageRepository>();
        return services;
    }
}
