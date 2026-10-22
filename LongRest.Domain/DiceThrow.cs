namespace LongRest.Domain;

public enum ThrowType { Normal, Advantage, Disadvantage }

/// <summary>A supplied d20 throw, retaining the faces for display and rerolls.</summary>
public abstract class DiceThrow
{
    public abstract ThrowType Type { get; }
    public abstract int Value { get; }
    public IReadOnlyList<int> Faces { get; }

    private protected DiceThrow(params int[] faces)
    {
        foreach (var face in faces) Validation.Range(face, 1, 20, nameof(faces));
        Faces = Array.AsReadOnly(faces);
    }

    public static ThrowType RequiredType(int advantageSources, int disadvantageSources)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(advantageSources);
        ArgumentOutOfRangeException.ThrowIfNegative(disadvantageSources);
        if ((advantageSources > 0) == (disadvantageSources > 0)) return ThrowType.Normal;
        return advantageSources > 0 ? ThrowType.Advantage : ThrowType.Disadvantage;
    }

    internal void Require(ThrowType required)
    {
        if (Type != required) throw new ArgumentException($"This roll requires a {required} dice throw.");
    }
}

public sealed class NormalDiceThrow(int face) : DiceThrow(face)
{
    public override ThrowType Type => ThrowType.Normal;
    public override int Value => Faces[0];
}

public sealed class AdvantageDiceThrow(int first, int second) : DiceThrow(first, second)
{
    public override ThrowType Type => ThrowType.Advantage;
    public override int Value => Math.Max(Faces[0], Faces[1]);
}

public sealed class DisadvantageDiceThrow(int first, int second) : DiceThrow(first, second)
{
    public override ThrowType Type => ThrowType.Disadvantage;
    public override int Value => Math.Min(Faces[0], Faces[1]);
}

/// <summary>Rolled damage or healing dice of one size, without a d20 selection rule.</summary>
public sealed class DiceRoll
{
    public int Sides { get; }
    public IReadOnlyList<int> Faces { get; }
    public int Total => Faces.Sum();

    public DiceRoll(int sides, params int[] faces)
    {
        Validation.Range(sides, 2, 100, nameof(sides));
        ArgumentNullException.ThrowIfNull(faces);
        foreach (var face in faces) Validation.Range(face, 1, sides, nameof(faces));
        Sides = sides;
        Faces = Array.AsReadOnly((int[])faces.Clone());
    }
}
