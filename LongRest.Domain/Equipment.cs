namespace LongRest.Domain;

public enum EquipmentKind
{
    Item,
    Tool,
    Weapon,
    Armor,
    Shield
}

public enum ArmorCategory
{
    Light,
    Medium,
    Heavy,
    Shield
}

public enum WeaponCategory
{
    Simple,
    Martial
}

public enum WeaponKind
{
    Melee,
    Ranged
}

[Flags]
public enum WeaponProperties
{
    None = 0,
    Finesse = 1,
    Thrown = 2,
    Light = 4,
    Versatile = 8,
    TwoHanded = 16,
    Heavy = 32
}

/// <summary>
/// Shared definition. Inventory owns quantities and references these objects by identity.
/// </summary>
public sealed class Equipment
{
    public Guid Id { get; }

    public string Name { get; }

    public EquipmentKind Kind { get; }

    public ArmorCategory ArmorCategory { get; init; }

    public int BaseArmorClass { get; init; }

    public int StrengthRequirement { get; init; }

    public bool StealthDisadvantage { get; init; }

    public WeaponCategory WeaponCategory { get; init; }

    public WeaponKind WeaponKind { get; init; }

    public WeaponProperties Properties { get; init; }

    public int DamageDice { get; init; } = 1;
    public int? FixedDamage { get; init; }

    public int DamageSides { get; init; } = 6;

    public int VersatileSides { get; init; } = 8;

    public DamageType DamageType { get; init; } = DamageType.Bludgeoning;

    public int Reach { get; init; } = 5;

    public int ShortRange { get; init; }

    public int LongRange { get; init; }

    public Equipment(
        string name,
        EquipmentKind kind = EquipmentKind.Item,
        Guid id = default)
    {
        Name = Validation.Name(name);
        Validation.Defined(kind);
        Kind = kind;
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
    }
}
