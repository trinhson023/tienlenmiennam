namespace SamLocService.Domain.Matches;

public sealed record SamMatchActionResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static SamMatchActionResult Success() => new(true);
    public static SamMatchActionResult Failure(string code, string message) => new(false, code, message);
}
