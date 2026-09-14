using TienLenService.Domain.Cards;
using TienLenService.Domain.Matches;
using TienLenService.Domain.Rules;

namespace TienLenService.Application.Matches;

public static class LegalMoveGenerator
{
    private static readonly Card ThreeOfSpades = new(Rank.Three, Suit.Spades);

    public static IReadOnlyList<Card>? FindLowestLegalPlay(TienLenMatch match, MatchPlayer player)
    {
        var hand = player.Hand.OrderBy(x => x, CardComparer.Instance).ToArray();
        var openingThreeRequired = match.IsOpeningPlay && hand.Contains(ThreeOfSpades);

        for (var size = 1; size <= hand.Length; size++)
        {
            IReadOnlyList<Card>? best = null;
            foreach (var candidate in Combinations(hand, size))
            {
                var validation = PlayValidator.Validate(candidate, match.Center, openingThreeRequired, hand.Length);
                if (!validation.IsValid) continue;
                if (best is null || CompareCandidate(candidate, best) < 0) best = candidate;
            }
            if (best is not null) return best;
        }
        return null;
    }

    private static int CompareCandidate(IReadOnlyList<Card> left, IReadOnlyList<Card> right)
    {
        var highest = CardComparer.Instance.Compare(left.MaxBy(x => x.Strength), right.MaxBy(x => x.Strength));
        if (highest != 0) return highest;
        return left.Sum(x => x.Strength).CompareTo(right.Sum(x => x.Strength));
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
}
