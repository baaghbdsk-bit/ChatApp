using ChatApp.API.Hubs;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Auth;
using ChatApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// DI: register framework services and application services.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// In-memory repositories are Singleton so their data survives across requests.
builder.Services.AddSingleton<InMemoryUserRepository>();
builder.Services.AddSingleton<IUserRepository>(sp =>
    sp.GetRequiredService<InMemoryUserRepository>());

builder.Services.AddSingleton<InMemoryConversationRepository>();
builder.Services.AddSingleton<IConversationRepository>(sp =>
    sp.GetRequiredService<InMemoryConversationRepository>());

builder.Services.AddSingleton<InMemoryMessageRepository>();
builder.Services.AddSingleton<IMessageRepository>(sp =>
    sp.GetRequiredService<InMemoryMessageRepository>());

builder.Services.AddSingleton<IAuthService, FakeOtpAuthService>();

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
