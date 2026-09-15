using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Rules;

public enum PlayValidationCode
{
    Valid = 0,
    InvalidCombination,
    OpeningThreeOfSpadesRequired,
    DoesNotMatchCenter,
    DoesNotBeatCenter
}

public sealed record PlayValidationResult(PlayValidationCode Code, string Message, Combination? Combination, bool IsChop)
{
    public bool IsValid => Code == PlayValidationCode.Valid;

    public static PlayValidationResult Valid(Combination combination, bool isChop = false) =>
        new(PlayValidationCode.Valid, string.Empty, combination, isChop);

    public static PlayValidationResult Invalid(PlayValidationCode code, string message, Combination? combination = null) =>
        new(code, message, combination, false);
}

public static class PlayValidator
{
    private static readonly Card ThreeOfSpades = new(Rank.Three, Suit.Spades);

    public static PlayValidationResult Validate(
        IReadOnlyCollection<Card>? proposed,
        IReadOnlyCollection<Card>? center,
        bool openingThreeOfSpadesRequired,
        int totalCardsInHand)
    {
        var combination = CombinationDetector.TryCreate(proposed);
        if (combination is null)
            return PlayValidationResult.Invalid(PlayValidationCode.InvalidCombination, "Invalid Combination");

        if (openingThreeOfSpadesRequired && !combination.Cards.Contains(ThreeOfSpades))
            return PlayValidationResult.Invalid(PlayValidationCode.OpeningThreeOfSpadesRequired, "First Play Must Include 3♠", combination);

        // Southern Tiến Lên is a shedding game: a legal 2 (heo) may be the final play.
        // "Thối heo" belongs to settlement/scoring rules for twos left unplayed when
        // somebody else finishes; it must not make an otherwise legal final play invalid.
        _ = totalCardsInHand;

        if (center is null || center.Count == 0) return PlayValidationResult.Valid(combination);

        if (ChopRules.CanChop(center, combination.Cards)) return PlayValidationResult.Valid(combination, true);

        var centerCombination = CombinationDetector.TryCreate(center);
        if (centerCombination is null || centerCombination.Type != combination.Type || centerCombination.Count != combination.Count)
            return PlayValidationResult.Invalid(PlayValidationCode.DoesNotMatchCenter, "Does Not Match Center", combination);

        if (CardComparer.CompareHighest(combination.Cards, centerCombination.Cards) <= 0)
            return PlayValidationResult.Invalid(PlayValidationCode.DoesNotBeatCenter, "Does Not Beat Center", combination);

        return PlayValidationResult.Valid(combination);
    }
}
