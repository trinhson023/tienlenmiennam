using TienLenService.Domain.Rules;

namespace TienLenService.Domain.Matches;

public enum MatchActionError
{
    None = 0,
    MatchCompleted,
    PlayerNotFound,
    NotYourTurn,
    InvalidSelection,
    CardNotOwned,
    CannotPassOpenRound,
    InvalidPlay
}

public sealed record MatchActionResult(
    bool IsSuccess,
    MatchActionError Error,
    string? Message,
    PlayValidationCode? ValidationCode,
    bool IsChop,
    bool PlayerFinished,
    bool TrickReset,
    bool MatchCompleted)
{
    public static MatchActionResult Success(bool isChop = false, bool playerFinished = false, bool trickReset = false, bool matchCompleted = false) =>
        new(true, MatchActionError.None, null, null, isChop, playerFinished, trickReset, matchCompleted);

    public static MatchActionResult Failure(MatchActionError error, string message, PlayValidationCode? validationCode = null) =>
        new(false, error, message, validationCode, false, false, false, false);
}
