using LongRest.Domain;

namespace LongRest.ConsoleApp;

// Plain saved data; the domain itself has no dependency on JSON or SQLite.
internal sealed class CharacterData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Stats Stats { get; set; } = new();
    public Class Class { get; set; } = new();
    public Race Race { get; set; } = new();
    public Background Background { get; set; } = new();
    public int? PlayerLevel { get; set; }
    public int? StatBlockProficiency { get; set; }
    public int Speed { get; set; }
    public int InitiativeBonus { get; set; }
    public int? ArmorClass { get; set; }
    public int HitDiceTotal { get; set; }
    public int HitDiceSides { get; set; }
    public int HitDiceSpent { get; set; }
    public DamageType[] Resistances { get; set; } = [];
    public DamageType[] Vulnerabilities { get; set; } = [];
    public DamageType[] Immunities { get; set; } = [];
    public HealthData Health { get; set; } = new();
    public Dictionary<Skill, Training> Skills { get; set; } = [];
    public Ability[] Saves { get; set; } = [];
    public WeaponCategory[] Weapons { get; set; } = [];
    public ArmorCategory[] Armor { get; set; } = [];
    public string[] SpecificWeapons { get; set; } = [];
    public List<EquipmentData> Equipment { get; set; } = [];
    public Dictionary<int, Training> Tools { get; set; } = [];
    public Dictionary<int, int> Inventory { get; set; } = [];
    public int[] Equipped { get; set; } = [];
    public List<ConditionData> Conditions { get; set; } = [];
    public MagicData? Magic { get; set; }
}

internal sealed class HealthData
{
    public int Current { get; set; }
    public int Maximum { get; set; }
    public int Temporary { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
    public bool Stable { get; set; }
    public bool Dead { get; set; }
}

internal sealed class EquipmentData
{
    public Equipment Definition { get; set; } = new("Item");
    public bool FromCatalog { get; set; }
}

internal sealed class ConditionData
{
    public Condition Condition { get; set; }
    public Guid? SourceId { get; set; }
}

internal sealed class MagicData
{
    public Ability Ability { get; set; }
    public SpellAvailability Availability { get; set; }
    public int SaveDcModifier { get; set; }
    public bool RecoverOnShortRest { get; set; }
    public Dictionary<int, int> MaximumSlots { get; set; } = [];
    public Dictionary<int, int> RemainingSlots { get; set; } = [];
    public List<SpellData> Spells { get; set; } = [];
    public int[] Known { get; set; } = [];
    public int[] Available { get; set; } = [];
    public int[] Cantrips { get; set; } = [];
    public int? Concentration { get; set; }
    public int[] PendingSaves { get; set; } = [];
}

internal sealed class SpellData
{
    public Spell Definition { get; set; } = new("Spell", 0);
    public bool FromCatalog { get; set; }
}

