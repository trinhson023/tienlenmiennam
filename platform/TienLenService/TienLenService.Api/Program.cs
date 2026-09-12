using TienLenService.Api.Hubs;
using TienLenService.Domain.Cards;
using TienLenService.Domain.Rules;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapHealthChecks("/health");
app.MapHub<TienLenHub>("/hubs/tienlen");
app.MapGet("/api/rules/smoke", () =>
{
    var pair = new[] { new Card(Rank.Seven, Suit.Spades), new Card(Rank.Seven, Suit.Hearts) };
    return Results.Ok(new { service = "TienLenService", detected = CombinationDetector.Detect(pair).ToString() });
});
app.Run();
