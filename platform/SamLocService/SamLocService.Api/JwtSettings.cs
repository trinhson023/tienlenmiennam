namespace SamLocService.Api;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "RoyalGame.Identity";
    public string Audience { get; init; } = "RoyalGame.Client";
    public string Secret { get; init; } = "CHANGE_ME_IN_REAL_ENVIRONMENT_MINIMUM_32_CHARS";
}
