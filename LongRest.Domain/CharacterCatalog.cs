namespace LongRest.Domain;

public static class CharacterCatalog
{
    public static IReadOnlyList<Class> Classes { get; } = Array.AsReadOnly<Class>([
        new() { Name = "Barbarian", HitDie = 12, SavingThrows = [Ability.Strength, Ability.Constitution],
            Weapons = [WeaponCategory.Simple, WeaponCategory.Martial], Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Shield],
            SkillChoices = [Skill.AnimalHandling, Skill.Athletics, Skill.Intimidation, Skill.Nature, Skill.Perception, Skill.Survival] },
        new() { Name = "Bard", HitDie = 8, SavingThrows = [Ability.Dexterity, Ability.Charisma],
            Weapons = [WeaponCategory.Simple], SpecificWeapons = ["Hand crossbow", "Longsword", "Rapier", "Shortsword"], Armor = [ArmorCategory.Light],
            SkillCount = 3, SkillChoices = Enum.GetValues<Skill>(), SpellcastingAbility = Ability.Charisma,
            StartingCantrips = 2, StartingSpells = 4, StartingSlots = 2 },
        new() { Name = "Cleric", HitDie = 8, SavingThrows = [Ability.Wisdom, Ability.Charisma],
            Weapons = [WeaponCategory.Simple], Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Shield],
            SkillChoices = [Skill.History, Skill.Insight, Skill.Medicine, Skill.Persuasion, Skill.Religion],
            SpellcastingAbility = Ability.Wisdom, SpellAvailability = SpellAvailability.Prepared, StartingCantrips = 3, StartingSlots = 2 },
        new() { Name = "Druid", HitDie = 8, SavingThrows = [Ability.Intelligence, Ability.Wisdom],
            SpecificWeapons = ["Club", "Dagger", "Dart", "Javelin", "Mace", "Quarterstaff", "Scimitar", "Sickle", "Sling", "Spear"],
            Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Shield],
            SkillChoices = [Skill.Arcana, Skill.AnimalHandling, Skill.Insight, Skill.Medicine, Skill.Nature, Skill.Perception, Skill.Religion, Skill.Survival],
            SpellcastingAbility = Ability.Wisdom, SpellAvailability = SpellAvailability.Prepared, StartingCantrips = 2, StartingSlots = 2 },
        new() { Name = "Fighter", HitDie = 10, SavingThrows = [Ability.Strength, Ability.Constitution],
            Weapons = [WeaponCategory.Simple, WeaponCategory.Martial], Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Heavy, ArmorCategory.Shield],
            SkillChoices = [Skill.Acrobatics, Skill.AnimalHandling, Skill.Athletics, Skill.History, Skill.Insight, Skill.Intimidation, Skill.Perception, Skill.Survival] },
        new() { Name = "Monk", HitDie = 8, SavingThrows = [Ability.Strength, Ability.Dexterity],
            Weapons = [WeaponCategory.Simple], SpecificWeapons = ["Shortsword"],
            SkillChoices = [Skill.Acrobatics, Skill.Athletics, Skill.History, Skill.Insight, Skill.Religion, Skill.Stealth] },
        new() { Name = "Paladin", HitDie = 10, SavingThrows = [Ability.Wisdom, Ability.Charisma],
            Weapons = [WeaponCategory.Simple, WeaponCategory.Martial], Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Heavy, ArmorCategory.Shield],
            SkillChoices = [Skill.Athletics, Skill.Insight, Skill.Intimidation, Skill.Medicine, Skill.Persuasion, Skill.Religion] },
        new() { Name = "Ranger", HitDie = 10, SavingThrows = [Ability.Strength, Ability.Dexterity],
            Weapons = [WeaponCategory.Simple, WeaponCategory.Martial], Armor = [ArmorCategory.Light, ArmorCategory.Medium, ArmorCategory.Shield],
            SkillCount = 3, SkillChoices = [Skill.AnimalHandling, Skill.Athletics, Skill.Insight, Skill.Investigation, Skill.Nature, Skill.Perception, Skill.Stealth, Skill.Survival] },
        new() { Name = "Rogue", HitDie = 8, SavingThrows = [Ability.Dexterity, Ability.Intelligence],
            Weapons = [WeaponCategory.Simple], SpecificWeapons = ["Hand crossbow", "Longsword", "Rapier", "Shortsword"], Armor = [ArmorCategory.Light],
            SkillCount = 4, SkillChoices = [Skill.Acrobatics, Skill.Athletics, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Investigation, Skill.Perception, Skill.Performance, Skill.Persuasion, Skill.SleightOfHand, Skill.Stealth] },
        new() { Name = "Sorcerer", HitDie = 6, SavingThrows = [Ability.Constitution, Ability.Charisma],
            SpecificWeapons = ["Dagger", "Dart", "Sling", "Quarterstaff", "Light crossbow"],
            SkillChoices = [Skill.Arcana, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Persuasion, Skill.Religion],
            SpellcastingAbility = Ability.Charisma, StartingCantrips = 4, StartingSpells = 2, StartingSlots = 2 },
        new() { Name = "Warlock", HitDie = 8, SavingThrows = [Ability.Wisdom, Ability.Charisma],
            Weapons = [WeaponCategory.Simple], Armor = [ArmorCategory.Light],
            SkillChoices = [Skill.Arcana, Skill.Deception, Skill.History, Skill.Intimidation, Skill.Investigation, Skill.Nature, Skill.Religion],
            SpellcastingAbility = Ability.Charisma, StartingCantrips = 2, StartingSpells = 2, StartingSlots = 1, PactMagic = true },
        new() { Name = "Wizard", HitDie = 6, SavingThrows = [Ability.Intelligence, Ability.Wisdom],
            SpecificWeapons = ["Dagger", "Dart", "Sling", "Quarterstaff", "Light crossbow"],
            SkillChoices = [Skill.Arcana, Skill.History, Skill.Insight, Skill.Investigation, Skill.Medicine, Skill.Religion],
            SpellcastingAbility = Ability.Intelligence, SpellAvailability = SpellAvailability.Prepared, StartingCantrips = 3, StartingSpells = 6, StartingSlots = 2 }
    ]);

    private static Dictionary<Ability, int> Bonuses(params (Ability Ability, int Increase)[] values) =>
        values.ToDictionary(pair => pair.Ability, pair => pair.Increase);

    public static IReadOnlyList<Race> Races { get; } = Array.AsReadOnly<Race>([
        new() { Name = "Hill dwarf", Speed = 25, IgnoreArmorSpeedPenalty = true, HitPointBonus = 1,
            AbilityBonuses = Bonuses((Ability.Constitution, 2), (Ability.Wisdom, 1)), Resistances = [DamageType.Poison],
            Weapons = ["Battleaxe", "Handaxe", "Light hammer", "Warhammer"] },
        new() { Name = "Mountain dwarf", Speed = 25, IgnoreArmorSpeedPenalty = true,
            AbilityBonuses = Bonuses((Ability.Constitution, 2), (Ability.Strength, 2)), Resistances = [DamageType.Poison],
            Armor = [ArmorCategory.Light, ArmorCategory.Medium], Weapons = ["Battleaxe", "Handaxe", "Light hammer", "Warhammer"] },
        new() { Name = "High elf", AbilityBonuses = Bonuses((Ability.Dexterity, 2), (Ability.Intelligence, 1)),
            Skills = [Skill.Perception], Weapons = ["Longsword", "Shortsword", "Shortbow", "Longbow"] },
        new() { Name = "Wood elf", Speed = 35, AbilityBonuses = Bonuses((Ability.Dexterity, 2), (Ability.Wisdom, 1)),
            Skills = [Skill.Perception], Weapons = ["Longsword", "Shortsword", "Shortbow", "Longbow"] },
        new() { Name = "Drow", AbilityBonuses = Bonuses((Ability.Dexterity, 2), (Ability.Charisma, 1)),
            Skills = [Skill.Perception], Weapons = ["Rapier", "Shortsword", "Hand crossbow"] },
        new() { Name = "Lightfoot halfling", Speed = 25, Small = true, AbilityBonuses = Bonuses((Ability.Dexterity, 2), (Ability.Charisma, 1)) },
        new() { Name = "Stout halfling", Speed = 25, Small = true, AbilityBonuses = Bonuses((Ability.Dexterity, 2), (Ability.Constitution, 1)),
            Resistances = [DamageType.Poison] },
        new() { Name = "Human", AbilityBonuses = Enum.GetValues<Ability>().ToDictionary(ability => ability, _ => 1) },
        new() { Name = "Dragonborn", AbilityBonuses = Bonuses((Ability.Strength, 2), (Ability.Charisma, 1)) },
        new() { Name = "Forest gnome", Speed = 25, Small = true, AbilityBonuses = Bonuses((Ability.Intelligence, 2), (Ability.Dexterity, 1)) },
        new() { Name = "Rock gnome", Speed = 25, Small = true, AbilityBonuses = Bonuses((Ability.Intelligence, 2), (Ability.Constitution, 1)) },
        new() { Name = "Half-elf", AbilityBonuses = Bonuses((Ability.Charisma, 2)), AbilityChoices = 2, SkillChoices = 2 },
        new() { Name = "Half-orc", AbilityBonuses = Bonuses((Ability.Strength, 2), (Ability.Constitution, 1)), Skills = [Skill.Intimidation] },
        new() { Name = "Tiefling", AbilityBonuses = Bonuses((Ability.Intelligence, 1), (Ability.Charisma, 2)), Resistances = [DamageType.Fire] }
    ]);

    public static IReadOnlyList<Background> Backgrounds { get; } = Array.AsReadOnly<Background>([
        new() { Name = "Acolyte", Skills = [Skill.Insight, Skill.Religion] },
        new() { Name = "Charlatan", Skills = [Skill.Deception, Skill.SleightOfHand] },
        new() { Name = "Criminal", Skills = [Skill.Deception, Skill.Stealth] },
        new() { Name = "Entertainer", Skills = [Skill.Acrobatics, Skill.Performance] },
        new() { Name = "Folk hero", Skills = [Skill.AnimalHandling, Skill.Survival] },
        new() { Name = "Guild artisan", Skills = [Skill.Insight, Skill.Persuasion] },
        new() { Name = "Hermit", Skills = [Skill.Medicine, Skill.Religion] },
        new() { Name = "Noble", Skills = [Skill.History, Skill.Persuasion] },
        new() { Name = "Outlander", Skills = [Skill.Athletics, Skill.Survival] },
        new() { Name = "Sage", Skills = [Skill.Arcana, Skill.History] },
        new() { Name = "Sailor", Skills = [Skill.Athletics, Skill.Perception] },
        new() { Name = "Soldier", Skills = [Skill.Athletics, Skill.Intimidation] },
        new() { Name = "Urchin", Skills = [Skill.SleightOfHand, Skill.Stealth] }
    ]);
}
