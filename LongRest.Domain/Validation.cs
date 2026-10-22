namespace LongRest.Domain;

internal static class Validation
{
    public static void Defined<T>(T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    public static void Range(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }

    public static string Name(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
