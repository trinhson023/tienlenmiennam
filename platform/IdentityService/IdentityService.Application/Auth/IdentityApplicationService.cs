using System.Text.RegularExpressions;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Common;
using IdentityService.Domain.Authentication;
using IdentityService.Domain.Users;

namespace IdentityService.Application.Auth;

public sealed class IdentityApplicationService(
    IIdentityRepository repository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private static readonly Regex UsernamePattern = new("^[a-zA-Z0-9_.-]{3,32}$", RegexOptions.Compiled);

    public async Task<ServiceResult<AuthSession>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var normalized = username.ToLowerInvariant();

        if (!UsernamePattern.IsMatch(username))
            return ServiceResult<AuthSession>.Failure("invalid_username", "Username phải dài 3-32 ký tự và chỉ chứa chữ, số, _, -, .");
        if (displayName.Length is < 1 or > 50)
            return ServiceResult<AuthSession>.Failure("invalid_display_name", "Tên hiển thị phải dài 1-50 ký tự.");
        if (password.Length is < 8 or > 128)
            return ServiceResult<AuthSession>.Failure("invalid_password", "Mật khẩu phải dài ít nhất 8 ký tự.");
        if (await repository.UsernameExistsAsync(normalized, cancellationToken))
            return ServiceResult<AuthSession>.Failure("username_taken", "Username đã tồn tại.");

        var playerRole = await repository.GetRoleAsync("player", cancellationToken);
        if (playerRole is null)
            return ServiceResult<AuthSession>.Failure("identity_not_ready", "Identity seed chưa sẵn sàng.");

        var user = new User(Guid.NewGuid(), username, normalized, displayName, passwordHasher.Hash(password));
        user.AssignRole(playerRole.Id);
        await repository.AddUserAsync(user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthSession>.Success(await IssueSessionAsync(user, cancellationToken));
    }

    public async Task<ServiceResult<AuthSession>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalized = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        var user = await repository.GetUserByUsernameAsync(normalized, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            return ServiceResult<AuthSession>.Failure("invalid_credentials", "Sai username hoặc mật khẩu.");

        user.MarkLoggedIn();
        var session = await IssueSessionAsync(user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<AuthSession>.Success(session);
    }

    public async Task<ServiceResult<AuthSession>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ServiceResult<AuthSession>.Failure("invalid_refresh_token", "Refresh token không hợp lệ.");

        var now = DateTimeOffset.UtcNow;
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var currentToken = await repository.GetRefreshTokenAsync(hash, cancellationToken);
        if (currentToken is null || !currentToken.IsActive(now))
            return ServiceResult<AuthSession>.Failure("invalid_refresh_token", "Refresh token đã hết hạn hoặc bị thu hồi.");

        var user = await repository.GetUserByIdAsync(currentToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult<AuthSession>.Failure("invalid_refresh_token", "Tài khoản không còn hoạt động.");

        var replacement = tokenService.CreateRefreshToken();
        currentToken.Revoke(now, replacement.TokenHash);
        var token = new RefreshToken(Guid.NewGuid(), user.Id, replacement.TokenHash, now, replacement.ExpiresAtUtc);
        user.AddRefreshToken(token);
        await repository.AddRefreshTokenAsync(token, cancellationToken);
        var auth = await repository.GetAuthorizationAsync(user.Id, cancellationToken);
        var access = tokenService.CreateAccessToken(user, auth.Roles, auth.Permissions);
        await repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthSession>.Success(new AuthSession(
            access.Token, access.ExpiresAtUtc, replacement.Token, replacement.ExpiresAtUtc,
            new UserProfile(user.Id, user.Username, user.DisplayName, auth.Roles, auth.Permissions)));
    }

    public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return ServiceResult<bool>.Success(true);
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var token = await repository.GetRefreshTokenAsync(hash, cancellationToken);
        if (token is not null && token.IsActive(DateTimeOffset.UtcNow))
        {
            token.Revoke(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(cancellationToken);
        }
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<UserProfile>> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult<UserProfile>.Failure("user_not_found", "Không tìm thấy tài khoản.");
        var auth = await repository.GetAuthorizationAsync(user.Id, cancellationToken);
        return ServiceResult<UserProfile>.Success(new UserProfile(user.Id, user.Username, user.DisplayName, auth.Roles, auth.Permissions));
    }

    private async Task<AuthSession> IssueSessionAsync(User user, CancellationToken cancellationToken)
    {
        var auth = await repository.GetAuthorizationAsync(user.Id, cancellationToken);
        var access = tokenService.CreateAccessToken(user, auth.Roles, auth.Permissions);
        var refresh = tokenService.CreateRefreshToken();
        var token = new RefreshToken(Guid.NewGuid(), user.Id, refresh.TokenHash, DateTimeOffset.UtcNow, refresh.ExpiresAtUtc);
        user.AddRefreshToken(token);
        await repository.AddRefreshTokenAsync(token, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new AuthSession(
            access.Token, access.ExpiresAtUtc, refresh.Token, refresh.ExpiresAtUtc,
            new UserProfile(user.Id, user.Username, user.DisplayName, auth.Roles, auth.Permissions));
    }
}
