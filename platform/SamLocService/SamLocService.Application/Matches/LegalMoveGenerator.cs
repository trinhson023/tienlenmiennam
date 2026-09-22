using SamLocService.Domain.Cards;
using SamLocService.Domain.Matches;
using SamLocService.Domain.Rules;

namespace SamLocService.Application.Matches;

public static class LegalMoveGenerator
{
    public static IReadOnlyList<Card>? FindLowestLegalPlay(SamMatch match, SamMatchPlayer player)
    {
        var hand = player.Hand.OrderBy(x => x, CardComparer.Instance).ToArray();
        Candidate? best = null;

        for (var size = 1; size <= hand.Length; size++)
        {
            foreach (var cards in Combinations(hand, size))
            {
                var combination = CombinationDetector.TryCreate(cards);
                if (combination is null) continue;
                if (!PlayValidation.Validate(combination, match.Center, hand.Length).IsValid) continue;

                var remaining = hand.Where(card => !cards.Contains(card)).ToArray();
                var leavesOnlyTwos = remaining.Length > 0 && remaining.All(card => card.Rank == Rank.Two);
                var candidate = new Candidate(cards, combination, leavesOnlyTwos ? 1 : 0);
                if (best is null || Compare(candidate, best) < 0) best = candidate;
            }
        }

        return best?.Cards;
    }

    private static int Compare(Candidate left, Candidate right)
    {
        var risk = left.EndgameRisk.CompareTo(right.EndgameRisk);
        if (risk != 0) return risk;

        var count = left.Combination.Count.CompareTo(right.Combination.Count);
        if (count != 0) return count;

        var strength = left.Combination.ComparisonStrength.CompareTo(right.Combination.ComparisonStrength);
        if (strength != 0) return strength;

        return string.CompareOrdinal(
            string.Join(',', left.Cards.Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(',', right.Cards.Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal)));
    }

    private static IEnumerable<IReadOnlyList<Card>> Combinations(Card[] source, int size)
    {
        var buffer = new Card[size];
        foreach (var value in Walk(0, 0)) yield return value;

        IEnumerable<IReadOnlyList<Card>> Walk(int sourceIndex, int bufferIndex)
        {
            if (bufferIndex == size)
            {
                yield return buffer.ToArray();
                yield break;
            }

            var remaining = size - bufferIndex;
            for (var i = sourceIndex; i <= source.Length - remaining; i++)
            {
                buffer[bufferIndex] = source[i];
                foreach (var nested in Walk(i + 1, bufferIndex + 1)) yield return nested;
            }
        }
    }

    private sealed record Candidate(IReadOnlyList<Card> Cards, Combination Combination, int EndgameRisk);
}
