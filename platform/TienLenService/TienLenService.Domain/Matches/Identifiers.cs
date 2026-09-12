namespace TienLenService.Domain.Matches;

public readonly record struct MatchId
{
    public MatchId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Match id cannot be empty.", nameof(value));
        Value = value;
    }
    public Guid Value { get; }
    public static MatchId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct PlayerId
{
    public PlayerId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Player id cannot be empty.", nameof(value));
        Value = value;
    }
    public Guid Value { get; }
    public static PlayerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct SeatNumber : IComparable<SeatNumber>
{
    public SeatNumber(int value)
    {
        if (value is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(value), "Tiến Lên seats are 0-3.");
        Value = value;
    }
    public int Value { get; }
    public int CompareTo(SeatNumber other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}
