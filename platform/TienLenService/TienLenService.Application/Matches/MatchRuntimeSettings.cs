namespace TienLenService.Application.Matches;

public sealed record MatchRuntimeSettings(
    int TurnSeconds = 60,
    int BotMinDelayMs = 800,
    int BotMaxDelayMs = 1400);
