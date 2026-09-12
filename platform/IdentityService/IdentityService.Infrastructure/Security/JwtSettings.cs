namespace IdentityService.Infrastructure.Security;

public sealed class JwtSettings
{
    public string Secret { get; set; } = "CHANGE_ME_IN_REAL_ENVIRONMENT_MINIMUM_32_CHARS";
    public string Issuer { get; set; } = "RoyalGame.Identity";
    public string Audience { get; set; } = "RoyalGame.Client";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
