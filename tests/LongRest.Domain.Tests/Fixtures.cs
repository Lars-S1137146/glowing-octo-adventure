namespace LongRest.Domain.Tests;

internal static class Fixtures
{
    public static Character Hero(Stats? stats = null, int hp = 20, int level = 1,
        Proficiencies? proficiencies = null, HitDice? hitDice = null, LongRest.Domain.Spellcasting? spells = null,
        DamageType[]? resistances = null, DamageType[]? vulnerabilities = null, DamageType[]? immunities = null) =>
        new("Adventurer", stats ?? new(), hp, level, proficiencies: proficiencies,
            hitDice: hitDice, spellcasting: spells, resistances: resistances,
            vulnerabilities: vulnerabilities, immunities: immunities);

    public static Equipment Sword() => new("Longsword", EquipmentKind.Weapon)
    {
        WeaponCategory = WeaponCategory.Martial, DamageSides = 8,
        DamageType = DamageType.Slashing, Properties = WeaponProperties.Versatile, VersatileSides = 10
    };
    public static Equipment Armor(ArmorCategory category = ArmorCategory.Heavy, int ac = 16) => new("Armor", EquipmentKind.Armor)
    { ArmorCategory = category, BaseArmorClass = ac };
    public static Equipment Shield() => new("Shield", EquipmentKind.Shield) { ArmorCategory = ArmorCategory.Shield };
    public static Equipment Bow() => new("Longbow", EquipmentKind.Weapon)
    { WeaponCategory = WeaponCategory.Martial, WeaponKind = WeaponKind.Ranged, DamageSides = 8,
        DamageType = DamageType.Piercing, ShortRange = 150, LongRange = 600, Properties = WeaponProperties.TwoHanded };
    public static AttackContext Nearby(int distance = 5, Cover cover = Cover.None) => new(distance, true, true, false, cover);
    public static Equipment Own(Character character, Equipment item, bool equip = true)
    {
        character.Inventory.Add(item);
        if (equip) character.Inventory.Equip(item);
        return item;
    }
    public static LongRest.Domain.Spellcasting Magic(Spell[]? spells = null, Spell[]? cantrips = null,
        Dictionary<int, int>? slots = null, SpellAvailability mode = SpellAvailability.Known) =>
        new(Ability.Intelligence, mode, spells ?? [], cantrips ?? [], slots ?? new() { [1] = 2, [3] = 1 });
    public static Encounter Fight(Character first, Character? second = null, int attacks = 1, bool surprised = false,
        int actions = 1)
    {
        var encounter = new Encounter(second is null
            ? [new(first, 15, surprised, actions, attacks)]
            : [new(first, 15, surprised, actions, attacks), new(second, 10)]);
        encounter.StartTurn();
        return encounter;
    }

    public static (int Hp, int Max, int Temp, bool Dead) HealthState(Character character) =>
        (character.Health.Value, character.Health.Max, character.Health.TempHealth, character.Health.IsDead);
}


