namespace LongRest.Domain;

/// <summary>Starting class data. Class and subclass features are adjudicated separately.</summary>
public sealed class Class
{
    // shortcut: only starting class data is automated; add progression when leveling is introduced.
    public string Name { get; init; } = "";
    public int HitDie { get; init; } = 8;
    public IReadOnlyList<Ability> SavingThrows { get; init; } = [];
    public IReadOnlyList<WeaponCategory> Weapons { get; init; } = [];
    public IReadOnlyList<string> SpecificWeapons { get; init; } = [];
    public IReadOnlyList<ArmorCategory> Armor { get; init; } = [];
    public int SkillCount { get; init; } = 2;
    public IReadOnlyList<Skill> SkillChoices { get; init; } = [];
    public Ability? SpellcastingAbility { get; init; }
    public SpellAvailability SpellAvailability { get; init; }
    public int StartingCantrips { get; init; }
    public int StartingSpells { get; init; }
    public int StartingSlots { get; init; }
    public bool PactMagic { get; init; }
}
