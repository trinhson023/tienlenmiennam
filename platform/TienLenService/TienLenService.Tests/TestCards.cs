using TienLenService.Domain.Cards;

namespace TienLenService.Tests;

internal static class TestCards
{
    public static Card C(string code) => CardCode.Parse(code);
    public static Card[] Cs(params string[] codes) => codes.Select(CardCode.Parse).ToArray();
}
