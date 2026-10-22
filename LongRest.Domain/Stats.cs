namespace LongRest.Domain;

public sealed class Stats
{
    internal event Action<int, int>? ConstitutionChanged;
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }
    public int Constitution { get; private set; }
    public int Intelligence { get; private set; }
    public int Wisdom { get; private set; }
    public int Charisma { get; private set; }

    public Stats(int strength = 10, int dexterity = 10, int constitution = 10,
        int intelligence = 10, int wisdom = 10, int charisma = 10)
    {
        Set(strength, dexterity, constitution, intelligence, wisdom, charisma);
    }

    public void Set(int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma)
    {
        foreach (var score in new[] { strength, dexterity, constitution, intelligence, wisdom, charisma })
            Validation.Range(score, 1, 30, nameof(score));
        var previousConstitution = Constitution;
        Strength = strength;
        Dexterity = dexterity;
        Constitution = constitution;
        Intelligence = intelligence;
        Wisdom = wisdom;
        Charisma = charisma;
        if (previousConstitution != constitution) ConstitutionChanged?.Invoke(previousConstitution, constitution);
    }

    public void Set(Ability ability, int score)
    {
        Validation.Defined(ability);
        Validation.Range(score, 1, 30, nameof(score));
        var previousConstitution = Constitution;
        switch (ability)
        {
            case Ability.Strength: Strength = score; break;
            case Ability.Dexterity: Dexterity = score; break;
            case Ability.Constitution: Constitution = score; break;
            case Ability.Intelligence: Intelligence = score; break;
            case Ability.Wisdom: Wisdom = score; break;
            case Ability.Charisma: Charisma = score; break;
        }
        if (previousConstitution != Constitution) ConstitutionChanged?.Invoke(previousConstitution, Constitution);
    }

    public int GetScore(Ability ability) => ability switch
    {
        Ability.Strength => Strength,
        Ability.Dexterity => Dexterity,
        Ability.Constitution => Constitution,
        Ability.Intelligence => Intelligence,
        Ability.Wisdom => Wisdom,
        Ability.Charisma => Charisma,
        _ => throw new ArgumentOutOfRangeException(nameof(ability))
    };

    public int GetModifier(Ability ability) => Modifier(GetScore(ability));

    public static int Modifier(int score)
    {
        Validation.Range(score, 1, 30, nameof(score));
        return (int)Math.Floor((score - 10) / 2.0);
    }
}
