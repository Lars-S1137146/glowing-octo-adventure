namespace LongRest.Domain.Tests.Interactions;

public class RuleInteractionTests
{
    [Test]
    public async Task TakingDamageAtZeroHpStillCausesAFailureWhenTemporaryHpAbsorbIt()
    {
        var hero = Hero();
        hero.Health.Set(0, 20, 5);
        hero.Health.Stabilize();
        var result = hero.TakeDamage(new(2, DamageType.Fire));
        await Assert.That(hero.Health.Value).IsEqualTo(0);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(3);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(1);
        await Assert.That(hero.Health.IsStable).IsFalse();
        await Assert.That(result.DamageTaken).IsEqualTo(2);
    }

    [Test]
    public async Task DamageAbsorbedByTemporaryHpStillRequiresConcentrationSave()
    {
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var hero = Hero(spells: magic);
        hero.Health.OfferTemporary(5, true);
        magic.Cast(hero, spell, 1);
        hero.TakeDamage(new(2, DamageType.Fire));
        await Assert.That(hero.Health.Value).IsEqualTo(20);
        await Assert.That(magic.PendingConcentrationSaves.Single()).IsEqualTo(10);
    }

    [Test]
    public async Task FixedWeaponDamageIsNotRolledOrDoubledOnACritical()
    {
        var attacker = Hero(new Stats(dexterity: 16));
        var blowgun = EquipmentCatalog.Find("Blowgun");
        var damage = CombatRules.RollDamage(attacker, blowgun, new(true, true, 25, 20), []);
        await Assert.That(damage.Amount).IsEqualTo(4);
        await Assert.That(damage.Type).IsEqualTo(DamageType.Piercing);
    }

    [Test]
    public async Task ThrownMeleeWeaponCannotKnockOutATargetNonlethally()
    {
        var attacker = Hero(new Stats(strength: 16));
        var javelin = Own(attacker, EquipmentCatalog.Find("Javelin"));
        var target = Hero();
        var attack = CombatRules.Attack(attacker, target, javelin, new NormalDiceThrow(15), Nearby(20) with { Thrown = true });
        var damage = CombatRules.RollDamage(attacker, javelin, attack, [new DiceRoll(6, 4)]);
        await Assert.That(damage.Melee).IsFalse();
        await Assert.That(() => target.TakeDamage(damage with { Nonlethal = true })).ThrowsExactly<ArgumentException>();
        await Assert.That(target.Health.Value).IsEqualTo(20);
    }

    [Test]
    public async Task NonFinesseBowCannotChooseStrength()
    {
        var hero = Hero();
        var bow = Own(hero, Bow());
        await Assert.That(() => CombatRules.Attack(hero, Hero(), bow, new NormalDiceThrow(10), Nearby(20), Ability.Strength))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task ShieldAndTwoWeaponsCannotOccupyThreeHands()
    {
        var hero = Hero();
        var first = Own(hero, Sword());
        var second = Own(hero, Sword());
        var shield = Own(hero, Shield(), equip: false);
        await Assert.That(() => hero.Inventory.Equip(shield)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Inventory.Shield).IsNull();
        hero.Inventory.Unequip(second);
        hero.Inventory.Equip(shield);
        await Assert.That(() => hero.Inventory.Equip(second)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Inventory.HeldWeapons.Single()).IsEqualTo(first);
    }

    [Test]
    public async Task LosingDodgeCannotBeUndoneByRemovingTheConditionDuringThatTurn()
    {
        var hero = Hero();
        using var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dodge);
        hero.AddCondition(Condition.Grappled);
        hero.RemoveCondition(Condition.Grappled);
        await Assert.That(encounter.IsDodging(hero)).IsFalse();
        var result = encounter.Save(hero, Ability.Dexterity, new NormalDiceThrow(10), 10);
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task StartingAnEncounterTwiceCannotRestoreSpentActions()
    {
        var hero = Hero();
        using var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dash);
        await Assert.That(() => encounter.StartTurn()).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
    }

    [Test]
    [Arguments(CombatAction.Help)]
    [Arguments(CombatAction.Hide)]
    [Arguments(CombatAction.Ready)]
    [Arguments(CombatAction.Search)]
    [Arguments(CombatAction.UseObject)]
    public async Task NarrativeActionsSpendAnActionWithoutClaimingToExecuteEffects(CombatAction action)
    {
        var hero = Hero();
        using var encounter = Fight(hero);
        encounter.TakeAction(hero, action);
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
        await Assert.That(encounter.AttacksRemaining).IsEqualTo(0);
        await Assert.That(hero.Health.Value).IsEqualTo(20);
    }

    [Test]
    public async Task AutomaticConditionAndExternalAdvantageCancelBeforeAskingForDice()
    {
        var hero = Hero();
        hero.AddCondition(Condition.Poisoned);
        await Assert.That(hero.CheckThrowType(Ability.Wisdom)).IsEqualTo((ThrowType?)ThrowType.Disadvantage);
        await Assert.That(hero.CheckThrowType(Ability.Wisdom, context: new(AdvantageSources: 2))).IsEqualTo((ThrowType?)ThrowType.Normal);
    }

    [Test]
    public async Task InvalidSpellResourceConfigurationDoesNotReachCasting()
    {
        await Assert.That(() => new LongRest.Domain.Spellcasting(Ability.Intelligence, SpellAvailability.Known,
            [], [], new Dictionary<int, int> { [0] = 2 })).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => new LongRest.Domain.Spellcasting(Ability.Intelligence, SpellAvailability.Known,
            [new Spell("Light", 0)], [], new Dictionary<int, int>())).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task ResourcesCannotBeSpentOnBehalfOfAnotherCaster()
    {
        var spell = new Spell("Bless", 1);
        var magic = Magic([spell]);
        var owner = Hero(spells: magic);
        await Assert.That(() => magic.Cast(Hero(), spell, 1)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
        magic.Cast(owner, spell, 1);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
    }

    [Test]
    public async Task DirectHealthTransitionsNotifyTheCharacterAboutUnconsciousness()
    {
        var hero = Hero();
        var sword = Own(hero, Sword());
        hero.Health.Set(0, 20, 0);
        await Assert.That(hero.Inventory.HeldWeapons.Contains(sword)).IsFalse();
        await Assert.That(hero.ActiveConditions.Contains(Condition.Unconscious)).IsTrue();
        hero.Health.DeathSave(new NormalDiceThrow(20));
        await Assert.That(hero.ActiveConditions.Contains(Condition.Unconscious)).IsFalse();
        await Assert.That(hero.ActiveConditions.Contains(Condition.Prone)).IsTrue();
    }

    [Test]
    public async Task OverlappingRacialAndBackgroundSkillsRequireAReplacement()
    {
        var race = CharacterCatalog.Races.Single(value => value.Name == "High elf");
        var fighter = CharacterCatalog.Classes.Single(value => value.Name == "Fighter");
        var sailor = CharacterCatalog.Backgrounds.Single(value => value.Name == "Sailor");
        await Assert.That(() => CharacterCreator.Create("Elf", new(15, 14, 13, 12, 10, 8), race, fighter,
            sailor, AbilityScoreMethod.StandardArray, [Skill.Insight, Skill.Intimidation])).ThrowsExactly<ArgumentException>();
        var hero = CharacterCreator.Create("Elf", new(15, 14, 13, 12, 10, 8), race, fighter,
            sailor, AbilityScoreMethod.StandardArray, [Skill.Insight, Skill.Intimidation], backgroundReplacements: [Skill.Arcana]);
        await Assert.That(Enum.GetValues<Skill>().Count(skill => hero.Proficiencies.ForSkill(skill) == Training.Proficient)).IsEqualTo(5);
    }

    [Test]
    public async Task ACantripCanBeForgottenAndReplacedWithoutGivingExtraChoices()
    {
        var hero = Characters.CreationTests.Create();
        foreach (var name in new[] { "Light", "Mage Hand", "Prestidigitation" }) hero.LearnSpell(SpellCatalog.Find(name));
        await Assert.That(() => hero.LearnSpell(SpellCatalog.Find("Ray of Frost"))).ThrowsExactly<InvalidOperationException>();
        hero.Spellcasting!.RemoveSpell(SpellCatalog.Find("Light"));
        hero.LearnSpell(SpellCatalog.Find("Ray of Frost"));
        await Assert.That(hero.Spellcasting.Cantrips.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ChangesToConfigurationCollectionsCannotRemoveLearnedSpells()
    {
        var spell = new Spell("Bless", 1);
        var configuredSpells = new List<Spell> { spell };
        var magic = new LongRest.Domain.Spellcasting(Ability.Wisdom, SpellAvailability.Known,
            configuredSpells, [], new Dictionary<int, int> { [1] = 2 });
        var hero = Hero(spells: magic);
        configuredSpells.Clear();
        magic.Cast(hero, spell, 1);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
    }

    [Test]
    public async Task DashAllowanceUsesCurrentSpeedAfterAStatChange()
    {
        var hero = Hero(new Stats(strength: 12));
        Own(hero, new Equipment("Chain mail", EquipmentKind.Armor)
            { ArmorCategory = ArmorCategory.Heavy, BaseArmorClass = 16, StrengthRequirement = 13 });
        using var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dash);
        encounter.Move(hero, 10);
        hero.Stats.Set(Ability.Strength, 16);
        await Assert.That(encounter.MovementRemaining).IsEqualTo(50);
        hero.AddCondition(Condition.Grappled);
        await Assert.That(encounter.MovementRemaining).IsEqualTo(0);
    }
}
