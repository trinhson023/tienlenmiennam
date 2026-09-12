using LobbyService.Domain.Rooms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health");
app.MapGet("/api/games", () => Results.Ok(Enum.GetNames<GameType>()));
app.Run();
