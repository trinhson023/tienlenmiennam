using IdentityService.Domain.Users;

namespace IdentityService.Application.Abstractions;

public sealed record AccessTokenDescriptor(string Token, DateTimeOffset ExpiresAtUtc);
public sealed record RefreshTokenDescriptor(string Token, string TokenHash, DateTimeOffset ExpiresAtUtc);

public interface ITokenService
{
    AccessTokenDescriptor CreateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    RefreshTokenDescriptor CreateRefreshToken();
    string HashRefreshToken(string rawToken);
}
