namespace LongRest.Domain;

public enum CastingTime
{
    Action,
    BonusAction,
    Reaction,
    Longer
}

public enum SpellAvailability
{
    Known,
    Prepared
}

public sealed class Spell
{
    public string Name { get; }

    public int Level { get; }

    public CastingTime CastingTime { get; }

    public bool Concentration { get; }
    // Display metadata only: effects, components, rituals and durations are not executed.
    public string? Range { get; init; }

    public string? School { get; init; }

    public string? Duration { get; init; }

    public string? Components { get; init; }

    public bool Ritual { get; init; }
    public IReadOnlyList<string> Classes { get; init; } = [];

    public Spell(
        string name,
        int level,
        CastingTime castingTime = CastingTime.Action,
        bool concentration = false)
    {
        Name = Validation.Name(name);
        Validation.Range(level, 0, 9, nameof(level));
        Validation.Defined(castingTime);
        Level = level;
        CastingTime = castingTime;
        Concentration = concentration;
    }
}
