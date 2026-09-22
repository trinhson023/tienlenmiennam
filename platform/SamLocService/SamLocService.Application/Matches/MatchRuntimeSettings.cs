namespace SamLocService.Application.Matches;

public sealed record MatchRuntimeSettings(
    int DeclarationSeconds = 5,
    int TurnSeconds = 60,
    int BotMinDelayMs = 800,
    int BotMaxDelayMs = 1400);
