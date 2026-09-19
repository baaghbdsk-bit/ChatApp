using ChatApp.API.Hubs;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Auth;
using ChatApp.Infrastructure.Repositories;
using ChatApp.Infrastructure.SQLite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DI: register framework services and application services.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Database selection: SQLite for local dev, in-memory for quick testing
// Set CHATAPP_USE_SQLITE=true to use SQLite, otherwise use in-memory
bool useSQLite = bool.TryParse(Environment.GetEnvironmentVariable("CHATAPP_USE_SQLITE") ?? "false", out var result) && result;

if (useSQLite)
{
    // SQLite configuration for local development
    string databasePath = Environment.GetEnvironmentVariable("CHATAPP_DATABASE_PATH") ?? "chatapp.db";
    builder.Services.AddSQLiteRepositories(databasePath);
}
else
{
    // In-memory repositories (original fake implementation)
    builder.Services.AddSingleton<InMemoryUserRepository>();
    builder.Services.AddSingleton<IUserRepository>(sp =>
        sp.GetRequiredService<InMemoryUserRepository>());

    builder.Services.AddSingleton<InMemoryConversationRepository>();
    builder.Services.AddSingleton<IConversationRepository>(sp =>
        sp.GetRequiredService<InMemoryConversationRepository>());

    builder.Services.AddSingleton<InMemoryMessageRepository>();
    builder.Services.AddSingleton<IMessageRepository>(sp =>
        sp.GetRequiredService<InMemoryMessageRepository>());
}

builder.Services.AddScoped<IAuthService, FakeOtpAuthService>();

// Angular runs on a different origin during local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (useSQLite)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<ChatAppDbContext>();
    database.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "ChatApp API",
    Timestamp = DateTime.UtcNow
}));

app.Run();
