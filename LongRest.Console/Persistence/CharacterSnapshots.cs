using LongRest.Domain;

namespace LongRest.ConsoleApp;

internal static class CharacterSnapshots
{
    public static CharacterData Capture(Character character)
    {
        var training = character.Proficiencies;
        var equipment = character.Inventory.Items.Keys.Concat(training.Tools.Keys).Distinct().ToList();
        var equipped = new List<Equipment>();
        if (character.Inventory.Armor is { } armor) equipped.Add(armor);
        if (character.Inventory.Shield is { } shield) equipped.Add(shield);
        equipped.AddRange(character.Inventory.HeldWeapons);
        var health = character.Health;
        return new()
        {
            Id = character.Id, Name = character.Name, Stats = character.Stats,
            Class = character.Class, Race = character.Race, Background = character.Background,
            PlayerLevel = character.PlayerLevel,
            StatBlockProficiency = character.PlayerLevel is null ? character.ProficiencyBonus : null,
            Speed = character.Speed,
            InitiativeBonus = character.InitiativeModifier - character.Stats.GetModifier(Ability.Dexterity),
            ArmorClass = character.ConfiguredArmorClass,
            HitDiceTotal = character.HitDice.Total, HitDiceSides = character.HitDice.Sides, HitDiceSpent = character.HitDice.Spent,
            Resistances = character.Resistances.ToArray(), Vulnerabilities = character.Vulnerabilities.ToArray(),
            Immunities = character.Immunities.ToArray(),
            Health = new()
            {
                Current = health.Value, Maximum = health.Max, Temporary = health.TempHealth,
                Successes = health.DeathSuccesses, Failures = health.DeathFailures, Stable = health.IsStable, Dead = health.IsDead
            },
            Skills = training.Skills.ToDictionary(), Saves = training.SavingThrows.ToArray(),
            Weapons = training.WeaponCategories.ToArray(), Armor = training.ArmorCategories.ToArray(),
            SpecificWeapons = training.SpecificWeapons.ToArray(),
            Equipment = equipment.Select(item => new EquipmentData
                { Definition = item, FromCatalog = EquipmentCatalog.All.Contains(item) }).ToList(),
            Tools = training.Tools.ToDictionary(pair => equipment.IndexOf(pair.Key), pair => pair.Value),
            Inventory = character.Inventory.Items.ToDictionary(pair => equipment.IndexOf(pair.Key), pair => pair.Value),
            Equipped = equipped.Select(item => equipment.IndexOf(item)).ToArray(),
            Conditions = character.AppliedConditions.Select(pair => new ConditionData
                { Condition = pair.Key, SourceId = pair.Value?.Id }).ToList(),
            Magic = character.Spellcasting is { } magic ? CaptureMagic(magic) : null
        };
    }

    private static MagicData CaptureMagic(Spellcasting magic)
    {
        var spells = magic.KnownSpells.Concat(magic.AvailableSpells).Concat(magic.Cantrips).ToList();
        if (magic.ConcentratingOn is { } concentration) spells.Add(concentration);
        spells = spells.Distinct().ToList();
        return new()
        {
            Ability = magic.Ability, Availability = magic.Availability,
            SaveDcModifier = magic.SaveDcModifier, RecoverOnShortRest = magic.RecoverOnShortRest,
            MaximumSlots = magic.MaximumSlots.ToDictionary(),
            RemainingSlots = magic.MaximumSlots.Keys.ToDictionary(level => level, magic.RemainingSlots),
            Spells = spells.Select(spell => new SpellData
                { Definition = spell, FromCatalog = SpellCatalog.All.Contains(spell) }).ToList(),
            Known = magic.KnownSpells.Select(spell => spells.IndexOf(spell)).ToArray(),
            Available = magic.AvailableSpells.Select(spell => spells.IndexOf(spell)).ToArray(),
            Cantrips = magic.Cantrips.Select(spell => spells.IndexOf(spell)).ToArray(),
            Concentration = magic.ConcentratingOn is { } current ? spells.IndexOf(current) : null,
            PendingSaves = magic.PendingConcentrationSaves.ToArray()
        };
    }

    public static List<Character> Restore(IReadOnlyList<CharacterData> data)
    {
        // Build every character first so condition sources can refer to later characters.
        var characters = data.Select(RestoreCharacter).ToList();
        var byId = characters.ToDictionary(character => character.Id);
        for (var i = 0; i < data.Count; i++)
        {
            var character = characters[i];
            foreach (var condition in character.AppliedConditions.Keys.ToArray()) character.RemoveCondition(condition);
            foreach (var saved in data[i].Conditions)
            {
                Character? source = null;
                if (saved.SourceId is { } id && !byId.TryGetValue(id, out source))
                    throw new InvalidDataException("A saved condition references a missing character.");
                character.AddCondition(saved.Condition, source);
            }
            if (!data[i].Conditions.Any(value => value.Condition == Condition.Prone)) character.RemoveCondition(Condition.Prone);
            foreach (var item in data[i].Equipped) character.Inventory.Equip(EquipmentAt(data[i], item));
            if (data[i].Magic is { } magic)
            {
                var concentration = magic.Concentration is { } index ? SpellAt(magic, index) : null;
                if (concentration is not null && !character.CanAct)
                    throw new InvalidDataException("An incapacitated character cannot maintain concentration.");
                character.Spellcasting!.RestoreResources(magic.RemainingSlots, concentration, magic.PendingSaves);
            }
        }
        return characters;
    }

    private static Equipment EquipmentAt(CharacterData data, int index)
    {
        if (index < 0 || index >= data.Equipment.Count) throw new InvalidDataException("Invalid saved equipment reference.");
        return data.Equipment[index].Definition;
    }

    private static Spell SpellAt(MagicData data, int index)
    {
        if (index < 0 || index >= data.Spells.Count) throw new InvalidDataException("Invalid saved spell reference.");
        return data.Spells[index].Definition;
    }

    private static Character RestoreCharacter(CharacterData data)
    {
        foreach (var item in data.Equipment)
            if (item.FromCatalog) item.Definition = EquipmentCatalog.Find(item.Definition.Name);
        var training = new Proficiencies(data.Skills, data.Saves, data.Weapons, data.Armor,
            data.Tools.ToDictionary(pair => EquipmentAt(data, pair.Key), pair => pair.Value), data.SpecificWeapons);
        var character = new Character(data.Name, data.Stats, data.Health.Maximum, data.PlayerLevel,
            data.StatBlockProficiency, data.Speed, training, new HitDice(data.HitDiceTotal, data.HitDiceSides, data.HitDiceSpent),
            data.Magic is { } magic ? RestoreMagic(magic) : null, data.Resistances, data.Vulnerabilities,
            data.Immunities, data.Class, data.Race, data.Background, data.ArmorClass, data.Id);
        character.SetInitiativeBonus(data.InitiativeBonus);
        var health = data.Health;
        character.Health.Restore(health.Current, health.Maximum, health.Temporary, health.Successes, health.Failures, health.Stable, health.Dead);
        foreach (var (index, quantity) in data.Inventory) character.Inventory.Add(EquipmentAt(data, index), quantity);
        return character;
    }

    private static Spellcasting RestoreMagic(MagicData data)
    {
        foreach (var spell in data.Spells)
            if (spell.FromCatalog) spell.Definition = SpellCatalog.Find(spell.Definition.Name);
        var magic = new Spellcasting(data.Ability, data.Availability, [], data.Cantrips.Select(index => SpellAt(data, index)),
            data.MaximumSlots, data.SaveDcModifier, data.RecoverOnShortRest);
        foreach (var index in data.Known) magic.LearnSpell(SpellAt(data, index));
        if (data.Availability == SpellAvailability.Prepared)
            foreach (var index in data.Available) magic.PrepareSpell(SpellAt(data, index));
        else if (!data.Available.SequenceEqual(data.Known))
            throw new InvalidDataException("Known spell availability does not match learned spells.");
        return magic;
    }
}

