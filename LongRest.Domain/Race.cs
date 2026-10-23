namespace LongRest.Domain;

public sealed class Race
{
    public string Name { get; init; } = "";
    public int Speed { get; init; } = 30;
    public bool Small { get; init; }
    public bool IgnoreArmorSpeedPenalty { get; init; }
    public int HitPointBonus { get; init; }
    public IReadOnlyDictionary<Ability, int> AbilityBonuses { get; init; } = new Dictionary<Ability, int>();
    public int AbilityChoices { get; init; }
    public int SkillChoices { get; init; }
    public IReadOnlyList<Skill> Skills { get; init; } = [];
    public IReadOnlyList<DamageType> Resistances { get; init; } = [];
    public IReadOnlyList<ArmorCategory> Armor { get; init; } = [];
    public IReadOnlyList<string> Weapons { get; init; } = [];
}
