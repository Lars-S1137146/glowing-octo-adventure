namespace LongRest.Domain.Tests.Characters;

public class CreationTests
{
    internal static Character Create(string race = "Human", string characterClass = "Wizard", string background = "Sage",
        Stats? scores = null, AbilityScoreMethod method = AbilityScoreMethod.StandardArray,
        Ability[]? abilityChoices = null, Skill[]? skillChoices = null, DamageType? ancestry = null)
    {
        var raceDefinition = CharacterCatalog.Races.Single(value => value.Name == race);
        var classDefinition = CharacterCatalog.Classes.Single(value => value.Name == characterClass);
        var backgroundDefinition = CharacterCatalog.Backgrounds.Single(value => value.Name == background);
        var inherited = backgroundDefinition.Skills.Concat(raceDefinition.Skills);
        var classSkills = classDefinition.SkillChoices.Except(inherited).Take(classDefinition.SkillCount).ToArray();
        return CharacterCreator.Create("Adventurer", scores ?? new(8, 10, 13, 15, 14, 12),
            raceDefinition, classDefinition, backgroundDefinition, method, classSkills, abilityChoices, skillChoices, ancestry);
    }

    [Test]
    public async Task HumanWizardAppliesStartingStatisticsAndTraining()
    {
        var hero = Create();
        await Assert.That(hero.Stats.Strength).IsEqualTo(9);
        await Assert.That(hero.Stats.Intelligence).IsEqualTo(16);
        await Assert.That(hero.Health.Max).IsEqualTo(8);
        await Assert.That(hero.HitDice.Sides).IsEqualTo(6);
        await Assert.That(hero.Proficiencies.ForSave(Ability.Intelligence)).IsTrue();
        await Assert.That(hero.Proficiencies.ForSkill(Skill.Arcana)).IsEqualTo(Training.Proficient);
        await Assert.That(hero.Proficiencies.ForWeapon(EquipmentCatalog.Find("Dagger"))).IsTrue();
        await Assert.That(hero.Proficiencies.ForWeapon(EquipmentCatalog.Find("Longsword"))).IsFalse();
        await Assert.That(hero.Spellcasting!.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    [Arguments("Barbarian", 12)]
    [Arguments("Bard", 8)]
    [Arguments("Cleric", 8)]
    [Arguments("Druid", 8)]
    [Arguments("Fighter", 10)]
    [Arguments("Monk", 8)]
    [Arguments("Paladin", 10)]
    [Arguments("Ranger", 10)]
    [Arguments("Rogue", 8)]
    [Arguments("Sorcerer", 6)]
    [Arguments("Warlock", 8)]
    [Arguments("Wizard", 6)]
    public async Task EveryClassCanCreateALevelOneCharacter(string characterClass, int hitDie)
    {
        var hero = Create(characterClass: characterClass);
        await Assert.That(hero.HitDice.Sides).IsEqualTo(hitDie);
        await Assert.That(hero.Health.Max).IsEqualTo(hitDie + 2);
        await Assert.That(hero.PlayerLevel).IsEqualTo((int?)1);
    }

    [Test]
    public async Task HillDwarfGetsHitPointBonusAndIgnoresHeavyArmorSpeedPenalty()
    {
        var hero = Create("Hill dwarf", "Fighter");
        hero.Inventory.Add(EquipmentCatalog.Find("Chain mail"));
        hero.Inventory.Equip(EquipmentCatalog.Find("Chain mail"));
        await Assert.That(hero.Health.Max).IsEqualTo(13);
        await Assert.That(hero.CurrentSpeed).IsEqualTo(25);
        await Assert.That(hero.Resistances.Contains(DamageType.Poison)).IsTrue();
    }

    [Test]
    public async Task HalfElfChoosesTwoOtherAbilitiesAndTwoSkills()
    {
        var hero = Create("Half-elf", abilityChoices: [Ability.Strength, Ability.Constitution],
            skillChoices: [Skill.Acrobatics, Skill.AnimalHandling]);
        await Assert.That(hero.Stats.Charisma).IsEqualTo(14);
        await Assert.That(hero.Stats.Strength).IsEqualTo(9);
        await Assert.That(hero.Stats.Constitution).IsEqualTo(14);
        await Assert.That(hero.Proficiencies.ForSkill(Skill.Acrobatics)).IsEqualTo(Training.Proficient);
        await Assert.That(() => Create("Half-elf", abilityChoices: [Ability.Charisma, Ability.Strength],
            skillChoices: [Skill.Acrobatics, Skill.AnimalHandling])).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task DragonbornRequiresAnAncestryAndGainsItsResistance()
    {
        await Assert.That(() => Create("Dragonborn")).ThrowsExactly<ArgumentException>();
        await Assert.That(() => Create("Dragonborn", ancestry: DamageType.Force)).ThrowsExactly<ArgumentException>();
        var hero = Create("Dragonborn", ancestry: DamageType.Lightning);
        hero.TakeDamage(new(3, DamageType.Lightning));
        await Assert.That(hero.Health.Value).IsEqualTo(hero.Health.Max - 1);
    }

    [Test]
    public async Task AbilityGenerationIsValidatedBeforeRacialIncreasesAndDoesNotMutateInputs()
    {
        var scores = new Stats(15, 15, 15, 8, 8, 8);
        var hero = Create(scores: scores, method: AbilityScoreMethod.PointBuy);
        await Assert.That(scores.Strength).IsEqualTo(15);
        await Assert.That(hero.Stats.Strength).IsEqualTo(16);
        await Assert.That(() => Create(scores: new(15, 15, 15, 15, 15, 15), method: AbilityScoreMethod.PointBuy))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => Create(scores: new(10, 10, 10, 10, 10, 10))).ThrowsExactly<ArgumentException>();
        await Assert.That(() => Create(scores: new(2, 10, 10, 10, 10, 10), method: AbilityScoreMethod.Rolled))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConstitutionEditAdjustsMaximumAndCurrentHpForAllExistingLevels()
    {
        var hero = Hero(new Stats(constitution: 12), hp: 30, level: 3);
        hero.TakeDamage(new(10, DamageType.Fire));
        hero.Stats.Set(Ability.Constitution, 14);
        await Assert.That(hero.Health.Max).IsEqualTo(33);
        await Assert.That(hero.Health.Value).IsEqualTo(23);
        hero.Stats.Set(Ability.Constitution, 10);
        await Assert.That(hero.Health.Max).IsEqualTo(27);
        await Assert.That(hero.Health.Value).IsEqualTo(17);
    }

    [Test]
    public async Task ConstitutionIncreaseDoesNotReviveADeadCharacter()
    {
        var hero = Create();
        hero.Health.MarkDead();
        hero.Stats.Set(Ability.Constitution, 18);
        await Assert.That(hero.Health.IsDead).IsTrue();
        await Assert.That(hero.Health.Value).IsEqualTo(0);
    }

    [Test]
    public async Task WizardMustLearnThenPrepareAndCannotPrepareAboveItsSlotLevel()
    {
        var hero = Create();
        var missile = SpellCatalog.Find("Magic Missile");
        await Assert.That(() => hero.PrepareSpell(missile)).ThrowsExactly<InvalidOperationException>();
        hero.LearnSpell(missile);
        await Assert.That(() => hero.Spellcasting!.Cast(hero, missile, 1)).ThrowsExactly<InvalidOperationException>();
        hero.PrepareSpell(missile);
        hero.Spellcasting!.Cast(hero, missile, 1);
        await Assert.That(hero.Spellcasting.RemainingSlots(1)).IsEqualTo(1);
        await Assert.That(() => hero.LearnSpell(SpellCatalog.Find("Fireball"))).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => hero.LearnSpell(SpellCatalog.Find("Healing Word"))).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ClericPreparesFromItsClassListAndRespectsPreparationLimit()
    {
        var hero = Create(characterClass: "Cleric");
        foreach (var name in new[] { "Bless", "Cure Wounds", "Healing Word" }) hero.PrepareSpell(SpellCatalog.Find(name));
        await Assert.That(() => hero.PrepareSpell(SpellCatalog.Find("Guiding Bolt"))).ThrowsExactly<InvalidOperationException>();
        hero.Spellcasting!.UnprepareSpell(SpellCatalog.Find("Bless"));
        hero.PrepareSpell(SpellCatalog.Find("Guiding Bolt"));
        await Assert.That(hero.Spellcasting.AvailableSpells.Count).IsEqualTo(3);
    }

    [Test]
    public async Task KnownCasterHasItsStartingSpellAndCantripLimits()
    {
        var hero = Create(characterClass: "Sorcerer");
        hero.LearnSpell(SpellCatalog.Find("Magic Missile"));
        hero.LearnSpell(SpellCatalog.Find("Shield"));
        await Assert.That(() => hero.LearnSpell(SpellCatalog.Find("Sleep"))).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Spellcasting!.KnownSpells.Count).IsEqualTo(2);
        await Assert.That(() => hero.PrepareSpell(SpellCatalog.Find("Shield"))).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task LevelOnePactSlotReturnsOnShortRestWithoutSpendingAHitDie()
    {
        var hero = Create(characterClass: "Warlock");
        var spell = SpellCatalog.Find("Hex");
        hero.LearnSpell(spell);
        hero.Spellcasting!.Cast(hero, spell, 1);
        hero.ShortRest(new DiceRoll(8));
        await Assert.That(hero.Spellcasting.RemainingSlots(1)).IsEqualTo(1);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(0);
    }

    [Test]
    public async Task SpellCatalogContainsOriginalClassesAnd2014ConcentrationData()
    {
        await Assert.That(SpellCatalog.All.Count).IsEqualTo(361);
        await Assert.That(SpellCatalog.All.Select(spell => spell.Name).Distinct().Count()).IsEqualTo(361);
        await Assert.That(SpellCatalog.Find("Spiritual Weapon").Concentration).IsFalse();
        await Assert.That(SpellCatalog.Find("True Strike").Concentration).IsTrue();
        await Assert.That(SpellCatalog.Find("Healing Word").CastingTime).IsEqualTo(CastingTime.BonusAction);
        await Assert.That(SpellCatalog.Find("Aid").Classes.Contains("Bard")).IsFalse();
        await Assert.That(SpellCatalog.Find("Find Familiar").CastingTime).IsEqualTo(CastingTime.Longer);
    }
}
