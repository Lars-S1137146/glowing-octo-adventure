namespace LongRest.Domain;

public static class EquipmentCatalog
{
    private static Equipment Weapon(string name, int sides, DamageType type, WeaponCategory category = WeaponCategory.Simple,
        WeaponProperties properties = WeaponProperties.None, WeaponKind kind = WeaponKind.Melee,
        int shortRange = 0, int longRange = 0, int dice = 1, int reach = 5, int versatileSides = 8) =>
        new(name, EquipmentKind.Weapon) { DamageSides = sides, DamageDice = dice, DamageType = type,
            WeaponCategory = category, Properties = properties, WeaponKind = kind, ShortRange = shortRange,
            LongRange = longRange, Reach = reach, VersatileSides = versatileSides };

    private static Equipment Armor(string name, ArmorCategory category, int ac, int strength = 0, bool stealthDisadvantage = false) =>
        new(name, EquipmentKind.Armor) { ArmorCategory = category, BaseArmorClass = ac,
            StrengthRequirement = strength, StealthDisadvantage = stealthDisadvantage };

    public static IReadOnlyList<Equipment> All { get; } = Array.AsReadOnly<Equipment>([
        Weapon("Club", 4, DamageType.Bludgeoning, properties: WeaponProperties.Light),
        Weapon("Dagger", 4, DamageType.Piercing, properties: WeaponProperties.Finesse | WeaponProperties.Light | WeaponProperties.Thrown, shortRange: 20, longRange: 60),
        Weapon("Greatclub", 8, DamageType.Bludgeoning, properties: WeaponProperties.TwoHanded),
        Weapon("Handaxe", 6, DamageType.Slashing, properties: WeaponProperties.Light | WeaponProperties.Thrown, shortRange: 20, longRange: 60),
        Weapon("Javelin", 6, DamageType.Piercing, properties: WeaponProperties.Thrown, shortRange: 30, longRange: 120),
        Weapon("Light hammer", 4, DamageType.Bludgeoning, properties: WeaponProperties.Light | WeaponProperties.Thrown, shortRange: 20, longRange: 60),
        Weapon("Mace", 6, DamageType.Bludgeoning),
        Weapon("Quarterstaff", 6, DamageType.Bludgeoning, properties: WeaponProperties.Versatile),
        Weapon("Sickle", 4, DamageType.Slashing, properties: WeaponProperties.Light),
        Weapon("Spear", 6, DamageType.Piercing, properties: WeaponProperties.Thrown | WeaponProperties.Versatile, shortRange: 20, longRange: 60),
        Weapon("Light crossbow", 8, DamageType.Piercing, properties: WeaponProperties.TwoHanded, kind: WeaponKind.Ranged, shortRange: 80, longRange: 320),
        Weapon("Dart", 4, DamageType.Piercing, properties: WeaponProperties.Finesse | WeaponProperties.Thrown, kind: WeaponKind.Ranged, shortRange: 20, longRange: 60),
        Weapon("Shortbow", 6, DamageType.Piercing, properties: WeaponProperties.TwoHanded, kind: WeaponKind.Ranged, shortRange: 80, longRange: 320),
        Weapon("Sling", 4, DamageType.Bludgeoning, kind: WeaponKind.Ranged, shortRange: 30, longRange: 120),
        Weapon("Battleaxe", 8, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Versatile, versatileSides: 10),
        Weapon("Flail", 8, DamageType.Bludgeoning, WeaponCategory.Martial),
        Weapon("Glaive", 10, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, reach: 10),
        Weapon("Greataxe", 12, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded),
        Weapon("Greatsword", 6, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, dice: 2),
        Weapon("Halberd", 10, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, reach: 10),
        Weapon("Longsword", 8, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Versatile, versatileSides: 10),
        Weapon("Maul", 6, DamageType.Bludgeoning, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, dice: 2),
        Weapon("Morningstar", 8, DamageType.Piercing, WeaponCategory.Martial),
        Weapon("Pike", 10, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, reach: 10),
        Weapon("Rapier", 8, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Finesse),
        Weapon("Scimitar", 6, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Finesse | WeaponProperties.Light),
        Weapon("Shortsword", 6, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Finesse | WeaponProperties.Light),
        Weapon("Trident", 6, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Thrown | WeaponProperties.Versatile, shortRange: 20, longRange: 60),
        Weapon("War pick", 8, DamageType.Piercing, WeaponCategory.Martial),
        Weapon("Warhammer", 8, DamageType.Bludgeoning, WeaponCategory.Martial, WeaponProperties.Versatile, versatileSides: 10),
        Weapon("Whip", 4, DamageType.Slashing, WeaponCategory.Martial, WeaponProperties.Finesse, reach: 10),
        new("Blowgun", EquipmentKind.Weapon) { FixedDamage = 1, DamageType = DamageType.Piercing,
            WeaponCategory = WeaponCategory.Martial, WeaponKind = WeaponKind.Ranged, ShortRange = 25, LongRange = 100 },
        Weapon("Hand crossbow", 6, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Light, WeaponKind.Ranged, 30, 120),
        Weapon("Heavy crossbow", 10, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, WeaponKind.Ranged, 100, 400),
        Weapon("Longbow", 8, DamageType.Piercing, WeaponCategory.Martial, WeaponProperties.Heavy | WeaponProperties.TwoHanded, WeaponKind.Ranged, 150, 600),
        Armor("Padded", ArmorCategory.Light, 11, stealthDisadvantage: true),
        Armor("Leather", ArmorCategory.Light, 11),
        Armor("Studded leather", ArmorCategory.Light, 12),
        Armor("Hide", ArmorCategory.Medium, 12),
        Armor("Chain shirt", ArmorCategory.Medium, 13),
        Armor("Scale mail", ArmorCategory.Medium, 14, stealthDisadvantage: true),
        Armor("Breastplate", ArmorCategory.Medium, 14),
        Armor("Half plate", ArmorCategory.Medium, 15, stealthDisadvantage: true),
        Armor("Ring mail", ArmorCategory.Heavy, 14, stealthDisadvantage: true),
        Armor("Chain mail", ArmorCategory.Heavy, 16, 13, true),
        Armor("Splint", ArmorCategory.Heavy, 17, 15, true),
        Armor("Plate", ArmorCategory.Heavy, 18, 15, true),
        new("Shield", EquipmentKind.Shield) { ArmorCategory = ArmorCategory.Shield },
        new("Thieves' tools", EquipmentKind.Tool),
        new("Rope"), new("Backpack"), new("Rations"), new("Torch")
    ]);

    public static Equipment Find(string name) => All.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown equipment. Use 'inventory catalog' to see available names.");
}
