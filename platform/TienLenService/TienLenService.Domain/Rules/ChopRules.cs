using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Rules;

public static class ChopRules
{
    public static bool CanChop(IReadOnlyCollection<Card>? center, IReadOnlyCollection<Card>? challenger)
    {
        if (center is null || challenger is null || center.Count == 0 || challenger.Count == 0) return false;

        var centerCombination = CombinationDetector.TryCreate(center);
        var challengerCombination = CombinationDetector.TryCreate(challenger);
        if (centerCombination is null || challengerCombination is null) return false;

        var centerAllTwos = center.All(card => card.Rank == Rank.Two);
        var challengerType = challengerCombination.Type;

        if (center.Count == 1 && center.First().Rank == Rank.Two)
            return challengerType is CombinationType.ThreeConsecutivePairs or CombinationType.FourOfAKind or CombinationType.FourConsecutivePairs;

        if (center.Count == 2 && centerAllTwos)
            return challengerType is CombinationType.FourOfAKind or CombinationType.FourConsecutivePairs;

        if (centerCombination.Type == CombinationType.ThreeConsecutivePairs)
            return challengerType is CombinationType.FourOfAKind or CombinationType.FourConsecutivePairs;

        if (centerCombination.Type == CombinationType.FourOfAKind)
            return challengerType == CombinationType.FourConsecutivePairs;

        return false;
    }
}
