using System.Security.Claims;
using IdentityService.Application.Auth;
using IdentityService.Application.Common;

namespace IdentityService.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, IdentityApplicationService service, CancellationToken ct) =>
            Map(await service.RegisterAsync(request, ct)));
        group.MapPost("/login", async (LoginRequest request, IdentityApplicationService service, CancellationToken ct) =>
            Map(await service.LoginAsync(request, ct)));
        group.MapPost("/refresh", async (RefreshRequest request, IdentityApplicationService service, CancellationToken ct) =>
            Map(await service.RefreshAsync(request, ct)));
        group.MapPost("/logout", async (LogoutRequest request, IdentityApplicationService service, CancellationToken ct) =>
            Map(await service.LogoutAsync(request, ct)));
        group.MapGet("/me", async (ClaimsPrincipal principal, IdentityApplicationService service, CancellationToken ct) =>
        {
            var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(rawId, out var userId)
                ? Map(await service.GetProfileAsync(userId, ct))
                : Results.Unauthorized();
        }).RequireAuthorization();

        return endpoints;
    }

    private static IResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        var status = result.ErrorCode switch
        {
            "username_taken" => StatusCodes.Status409Conflict,
            "invalid_credentials" or "invalid_refresh_token" => StatusCodes.Status401Unauthorized,
            "user_not_found" => StatusCodes.Status404NotFound,
            "identity_not_ready" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(new { error = result.ErrorCode, message = result.ErrorMessage }, statusCode: status);
    }
}
