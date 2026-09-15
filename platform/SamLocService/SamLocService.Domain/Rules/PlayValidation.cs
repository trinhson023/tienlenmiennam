using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public sealed record PlayValidationResult(bool IsValid, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static PlayValidationResult Success() => new(true);
    public static PlayValidationResult Failure(string code, string message) => new(false, code, message);
}

public static class PlayValidation
{
    public static PlayValidationResult Validate(Combination candidate, Combination? center, int cardsInHandBeforePlay)
    {
        if (candidate.Count == cardsInHandBeforePlay && candidate.Cards.Any(x => x.Rank == Rank.Two))
            return PlayValidationResult.Failure("cannot_finish_with_two", "Không được về bằng quân 2 trong Sâm Lốc.");

        if (center is null) return PlayValidationResult.Success();

        if (candidate.Type == CombinationType.FourOfAKind && center.Type == CombinationType.Single && center.HighestRank == Rank.Two)
            return PlayValidationResult.Success();

        if (candidate.Type != center.Type || candidate.Count != center.Count)
            return PlayValidationResult.Failure("combination_mismatch", "Phải chặn bằng cùng loại và cùng số lá, trừ tứ quý chặt một quân 2.");

        if ((int)candidate.HighestRank <= (int)center.HighestRank)
            return PlayValidationResult.Failure("combination_not_higher", "Bộ bài chặn phải lớn hơn bộ đang nằm trên bàn.");

        return PlayValidationResult.Success();
    }
}
