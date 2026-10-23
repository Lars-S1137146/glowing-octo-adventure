namespace LongRest.Domain;

public sealed class HitDice
{
    public int Total { get; }
    public int Sides { get; }
    public int Spent { get; private set; }
    public int Remaining => Total - Spent;

    public HitDice(int total, int sides, int spent = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        if (sides is not (6 or 8 or 10 or 12)) throw new ArgumentOutOfRangeException(nameof(sides));
        Validation.Range(spent, 0, total, nameof(spent));
        Total = total;
        Sides = sides;
        Spent = spent;
    }

    internal void Spend(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > Remaining) throw new InvalidOperationException("Not enough Hit Dice remaining.");
        Spent += count;
    }

    internal void Recover() => Spent = Math.Max(0, Spent - Math.Max(1, Total / 2));
}
