namespace LongRest.Domain;

public sealed partial class Character
{
    private readonly Dictionary<Condition, Character?> conditions = [];
    private readonly int? statBlockProficiency;
    private int initiativeBonus;
    private readonly int? armorClassOverride;
    private bool wasAtZeroHp;
    internal event Action<Character>? LostDodgeBenefits;

    public Guid Id { get; }
    public string Name { get; private set; }
    public Class Class { get; }
    public Race Race { get; }
    public Background Background { get; }
    public Stats Stats { get; }
    public Health Health { get; }
    public Inventory Inventory { get; } = new();
    public Proficiencies Proficiencies { get; }
    public HitDice HitDice { get; }
    public Spellcasting? Spellcasting { get; }
    public int? PlayerLevel { get; }
    public int Level => PlayerLevel ?? 1;
    public int Speed { get; }
    public int ProficiencyBonus => statBlockProficiency ?? 2 + (Level - 1) / 4;
    public int InitiativeModifier => Stats.GetModifier(Ability.Dexterity) + initiativeBonus;
    public int PassivePerception => 10 + Stats.GetModifier(Ability.Wisdom) +
        (int)Proficiencies.ForSkill(Skill.Perception) * ProficiencyBonus;
    public IReadOnlyCollection<DamageType> Resistances { get; }
    public IReadOnlyCollection<DamageType> Vulnerabilities { get; }
    public IReadOnlyCollection<DamageType> Immunities { get; }
    public IReadOnlyDictionary<Condition, Character?> AppliedConditions =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<Condition, Character?>(conditions);
    public int? ConfiguredArmorClass => armorClassOverride;

    public Character(string name, Stats stats, int maximumHealth = 10, int? level = 1,
        int? proficiencyBonus = null, int speed = 30, Proficiencies? proficiencies = null,
        HitDice? hitDice = null, Spellcasting? spellcasting = null,
        IEnumerable<DamageType>? resistances = null, IEnumerable<DamageType>? vulnerabilities = null,
        IEnumerable<DamageType>? immunities = null, Class? characterClass = null,
        Race? race = null, Background? background = null, int? armorClass = null, Guid? id = null)
    {
        Name = Validation.Name(name);
        if (id == Guid.Empty) throw new ArgumentException("A character ID cannot be empty.", nameof(id));
        Id = id ?? Guid.NewGuid();
        ArgumentNullException.ThrowIfNull(stats);
        if (level is not null) Validation.Range(level.Value, 1, 20, nameof(level));
        if ((level is null && proficiencyBonus is null) || proficiencyBonus <= 0 ||
            (level is not null && proficiencyBonus is not null))
            throw new ArgumentOutOfRangeException(nameof(proficiencyBonus));
        ArgumentOutOfRangeException.ThrowIfNegative(speed);
        if (armorClass is not null) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(armorClass.Value);
        armorClassOverride = armorClass;
        Stats = stats;
        PlayerLevel = level;
        statBlockProficiency = proficiencyBonus;
        Speed = speed;
        Proficiencies = proficiencies ?? new();
        HitDice = hitDice ?? new(level ?? 1, 8);
        Spellcasting = spellcasting;
        Resistances = DamageTypes(resistances);
        Vulnerabilities = DamageTypes(vulnerabilities);
        Immunities = DamageTypes(immunities);
        Class = characterClass ?? new();
        Race = race ?? new();
        Background = background ?? new();
        Health = new(maximumHealth, maximumHealth);
        Health.Changed += HealthChanged;
        Stats.ConstitutionChanged += (previous, current) =>
            Health.ChangeMaximum((Stats.Modifier(current) - Stats.Modifier(previous)) * Level);
    }

    private static IReadOnlyCollection<DamageType> DamageTypes(IEnumerable<DamageType>? values)
    {
        var result = values?.ToHashSet() ?? [];
        foreach (var value in result) Validation.Defined(value);
        return Array.AsReadOnly(result.ToArray());
    }

    public void Rename(string name) => Name = Validation.Name(name);

    public int ArmorClass
    {
        get
        {
            if (armorClassOverride is not null) return armorClassOverride.Value;
            var dexterity = Stats.GetModifier(Ability.Dexterity);
            var armor = Inventory.Armor;
            var value = armor is null ? 10 + dexterity : armor.ArmorCategory switch
            {
                ArmorCategory.Light => armor.BaseArmorClass + dexterity,
                ArmorCategory.Medium => armor.BaseArmorClass + Math.Min(2, dexterity),
                _ => armor.BaseArmorClass
            };
            return value + (Inventory.Shield is null ? 0 : 2);
        }
    }

    internal bool HasUntrainedArmor =>
        (Inventory.Armor is { } armor && !Proficiencies.ForArmor(armor.ArmorCategory)) ||
        (Inventory.Shield is not null && !Proficiencies.ForArmor(ArmorCategory.Shield));

    public IReadOnlyCollection<Condition> ActiveConditions
    {
        get
        {
            var result = conditions.Keys.ToList();
            if (Health.IsUnconscious && !result.Contains(Condition.Unconscious)) result.Add(Condition.Unconscious);
            return result.AsReadOnly();
        }
    }

    internal bool Has(Condition condition) =>
        conditions.ContainsKey(condition) || (condition == Condition.Unconscious && Health.IsUnconscious);

    internal bool HasAny(params Condition[] values) => values.Any(Has);
    public bool CanAct => !Health.IsDead &&
        !HasAny(Condition.Incapacitated, Condition.Paralyzed, Condition.Petrified, Condition.Stunned, Condition.Unconscious);
    public bool CanSpeak => !Health.IsDead && !HasAny(Condition.Paralyzed, Condition.Petrified, Condition.Unconscious);
    public bool CanCast => CanAct && !HasUntrainedArmor;

    public int CurrentSpeed
    {
        get
        {
            if (Health.IsDead || HasAny(Condition.Grappled, Condition.Restrained, Condition.Paralyzed,
                    Condition.Petrified, Condition.Stunned, Condition.Unconscious)) return 0;
            var penalty = Inventory.Armor is { ArmorCategory: ArmorCategory.Heavy } armor &&
                Stats.Strength < armor.StrengthRequirement && !Race.IgnoreArmorSpeedPenalty ? 10 : 0;
            return Math.Max(0, Speed - penalty);
        }
    }

    public Character? ConditionSource(Condition condition)
    {
        Validation.Defined(condition);
        return conditions.GetValueOrDefault(condition);
    }

    public void AddCondition(Condition condition, Character? source = null)
    {
        Validation.Defined(condition);
        if (condition is Condition.Charmed or Condition.Frightened) ArgumentNullException.ThrowIfNull(source);
        conditions.TryAdd(condition, source);
        if (condition == Condition.Unconscious)
        {
            conditions.TryAdd(Condition.Prone, null);
            Inventory.DropHeldItems();
        }
        if (!CanAct) Spellcasting?.EndConcentration();
        if (!CanAct || CurrentSpeed == 0) LostDodgeBenefits?.Invoke(this);
    }

    public void RemoveCondition(Condition condition)
    {
        Validation.Defined(condition);
        conditions.Remove(condition);
    }

    private void HealthChanged()
    {
        if (Health.Value == 0 && !wasAtZeroHp)
        {
            conditions.TryAdd(Condition.Prone, null);
            Inventory.DropHeldItems();
        }
        wasAtZeroHp = Health.Value == 0;
        if (!CanAct) Spellcasting?.EndConcentration();
        if (!CanAct || CurrentSpeed == 0) LostDodgeBenefits?.Invoke(this);
    }

    public void SetInitiativeBonus(int bonus) => initiativeBonus = bonus;
}
