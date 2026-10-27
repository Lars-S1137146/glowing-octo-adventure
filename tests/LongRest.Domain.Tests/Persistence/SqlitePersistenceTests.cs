using LongRest.ConsoleApp;
using LongRest.Domain.Tests.Characters;

namespace LongRest.Domain.Tests.Persistence;

public class SqlitePersistenceTests
{
    [Test]
    public async Task MissingDatabaseAndDirectoryAreCreatedAndExistingAccountsAreRetained()
    {
        using var database = new TemporaryDatabase();
        await Assert.That(File.Exists(database.PathName)).IsFalse();
        var accounts = database.Open();
        accounts.Register(" Alice ", "a secret password");
        accounts.Save();
        var reopened = database.Open();
        await Assert.That(reopened.Login("ALICE", "a secret password").Name).IsEqualTo("Alice");
        await Assert.That(() => reopened.Login("alice", "wrong")).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => reopened.Register("ALICE", "other")).ThrowsExactly<InvalidOperationException>();
        await Assert.That(database.Execute("PRAGMA user_version;")).IsEqualTo((object)1L);
        await Assert.That(database.Execute("SELECT length(PasswordSalt) FROM Users;")).IsEqualTo((object)16L);
        await Assert.That(database.Execute("SELECT length(PasswordHash) FROM Users;")).IsEqualTo((object)32L);
        await Assert.That(System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(database.PathName)).Contains("a secret password")).IsFalse();
    }

    [Test]
    public async Task ReopeningPreservesCharacterResourcesAndCatalogIdentity()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var wizard = CreationTests.Create();
        wizard.Rename("Merlin");
        wizard.Stats.Set(Ability.Strength, 17);
        wizard.Stats.Set(Ability.Constitution, 16);
        var source = new Character("Dragon", new Stats(), maximumHealth: 40);
        user.AddCharacter(wizard);
        user.AddCharacter(source);
        foreach (var name in new[] { "Magic Missile", "Fog Cloud" })
        {
            wizard.LearnSpell(SpellCatalog.Find(name));
            wizard.PrepareSpell(SpellCatalog.Find(name));
        }
        wizard.LearnSpell(SpellCatalog.Find("Light"));
        var magic = wizard.Spellcasting!;
        magic.Cast(wizard, SpellCatalog.Find("Fog Cloud"), 1);
        magic.Cast(wizard, SpellCatalog.Find("Magic Missile"), 1);
        wizard.ShortRest(new DiceRoll(6, 1));
        wizard.TakeDamage(new(2, DamageType.Fire));
        wizard.TakeDamage(new(1, DamageType.Cold));
        wizard.Health.OfferTemporary(5, true);
        wizard.Inventory.Add(EquipmentCatalog.Find("Dagger"));
        wizard.Inventory.Equip(EquipmentCatalog.Find("Dagger"));
        wizard.Inventory.Add(EquipmentCatalog.Find("Quarterstaff"), 2);
        wizard.AddCondition(Condition.Frightened, source);
        accounts.Save();

        var restoredUser = database.Open().Login("alice", "secret");
        var restored = restoredUser.FindCharacter("Merlin");
        await Assert.That(restored.Id).IsEqualTo(wizard.Id);
        await Assert.That(restored.Stats.Strength).IsEqualTo(17);
        await Assert.That(restored.Health.Value).IsEqualTo(wizard.Health.Value);
        await Assert.That(restored.Health.Max).IsEqualTo(9);
        await Assert.That(restored.Health.TempHealth).IsEqualTo(5);
        await Assert.That(restored.HitDice.Spent).IsEqualTo(1);
        await Assert.That(restored.Class.Name).IsEqualTo("Wizard");
        await Assert.That(restored.Background.Name).IsEqualTo("Sage");
        await Assert.That(restored.ConditionSource(Condition.Frightened)).IsEqualTo(restoredUser.FindCharacter("Dragon"));
        await Assert.That(restored.Inventory.HeldWeapons.Single()).IsEqualTo(EquipmentCatalog.Find("Dagger"));
        await Assert.That(restored.Inventory.Quantity(EquipmentCatalog.Find("Quarterstaff"))).IsEqualTo(2);
        await Assert.That(restored.Spellcasting!.RemainingSlots(1)).IsEqualTo(0);
        await Assert.That(restored.Spellcasting.ConcentratingOn).IsEqualTo(SpellCatalog.Find("Fog Cloud"));
        await Assert.That(restored.Spellcasting.PendingConcentrationSaves.SequenceEqual(new[] { 10, 10 })).IsTrue();
        await Assert.That(restored.Spellcasting.KnownSpells.Select(spell => spell.Name).SequenceEqual(new[] { "Magic Missile", "Fog Cloud" })).IsTrue();
        await Assert.That(restored.Spellcasting.Cantrips.Single()).IsEqualTo(SpellCatalog.Find("Light"));

        restored.Spellcasting.ResolveConcentrationSave(restored, new NormalDiceThrow(10));
        restored.LongRest();
        restored.Spellcasting.Cast(restored, SpellCatalog.Find("Magic Missile"), 1);
        restored.Inventory.Unequip(EquipmentCatalog.Find("Dagger"));
        restored.Stats.Set(Ability.Constitution, 18);
        await Assert.That(restored.Health.Max).IsEqualTo(10);
    }

    [Test]
    public async Task DyingStableDeadAndIndependentUnconsciousnessRemainDistinct()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var dying = new Character("Dying", new Stats(), 20);
        dying.Health.Set(0, 20, 4);
        dying.Health.DeathSave(new NormalDiceThrow(10));
        dying.Health.DeathSave(new NormalDiceThrow(1));
        var stable = new Character("Stable", new Stats(), 20);
        stable.Health.Set(0, 20, 0);
        stable.Health.Stabilize();
        var dead = new Character("Dead", new Stats(), 20);
        dead.Health.MarkDead();
        var sleeping = new Character("Sleeping", new Stats(), 20);
        sleeping.AddCondition(Condition.Unconscious);
        foreach (var hero in new[] { dying, stable, dead, sleeping }) user.AddCharacter(hero);
        accounts.Save();

        var restored = database.Open().Login("Alice", "secret");
        var restoredDying = restored.FindCharacter("Dying");
        await Assert.That(restoredDying.Health.DeathSuccesses).IsEqualTo(1);
        await Assert.That(restoredDying.Health.DeathFailures).IsEqualTo(2);
        await Assert.That(restoredDying.Health.TempHealth).IsEqualTo(4);
        restoredDying.Heal(1);
        await Assert.That(restoredDying.ActiveConditions.Contains(Condition.Unconscious)).IsFalse();
        await Assert.That(restoredDying.ActiveConditions.Contains(Condition.Prone)).IsTrue();
        await Assert.That(restored.FindCharacter("Stable").Health.IsStable).IsTrue();
        await Assert.That(restored.FindCharacter("Dead").Health.IsDead).IsTrue();
        await Assert.That(() => restored.FindCharacter("Dead").Heal(1)).ThrowsExactly<InvalidOperationException>();
        var restoredSleeping = restored.FindCharacter("Sleeping");
        restoredSleeping.Heal(1);
        await Assert.That(restoredSleeping.ActiveConditions.Contains(Condition.Unconscious)).IsTrue();
    }

    [Test]
    public async Task CustomDefinitionsAndTrainingSurviveWithoutMergingSameNameObjects()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var tool = new Equipment("Custom tools", EquipmentKind.Tool);
        var first = new Equipment("Twin", EquipmentKind.Weapon) { DamageSides = 12 };
        var second = new Equipment("Twin", EquipmentKind.Weapon) { DamageSides = 4 };
        var firstSpell = new Spell("Custom spark", 1, concentration: true);
        var secondSpell = new Spell("Custom spark", 1);
        var magic = new LongRest.Domain.Spellcasting(Ability.Wisdom, SpellAvailability.Known, [firstSpell, secondSpell], [],
            new Dictionary<int, int> { [1] = 3 }, saveDcModifier: 2, recoverOnShortRest: true);
        var hero = new Character("Monster", new Stats(dexterity: 14), 40, level: null, proficiencyBonus: 4,
            speed: 40, proficiencies: new(skills: new Dictionary<Skill, Training> { [Skill.Acrobatics] = Training.Expertise },
                saves: [Ability.Constitution], weapons: [WeaponCategory.Martial], armor: [ArmorCategory.Heavy],
                tools: new Dictionary<Equipment, Training> { [tool] = Training.Expertise }, specificWeapons: ["Twin"]),
            hitDice: new(8, 10, 3), spellcasting: magic, resistances: [DamageType.Fire],
            vulnerabilities: [DamageType.Cold], immunities: [DamageType.Poison], armorClass: 19);
        hero.SetInitiativeBonus(4);
        hero.Inventory.Add(first, 2);
        hero.Inventory.Add(second);
        hero.Inventory.Equip(second);
        magic.Cast(hero, firstSpell, 1);
        user.AddCharacter(hero);
        accounts.Save();

        var restored = database.Open().Login("Alice", "secret").Characters.Single();
        await Assert.That(restored.Id).IsEqualTo(hero.Id);
        await Assert.That(restored.PlayerLevel).IsNull();
        await Assert.That(restored.ProficiencyBonus).IsEqualTo(4);
        await Assert.That(restored.ArmorClass).IsEqualTo(19);
        await Assert.That(restored.CurrentSpeed).IsEqualTo(40);
        await Assert.That(restored.InitiativeModifier).IsEqualTo(6);
        await Assert.That(restored.HitDice.Spent).IsEqualTo(3);
        await Assert.That(restored.Inventory.Items.Count).IsEqualTo(2);
        await Assert.That(restored.Inventory.HeldWeapons.Single().Id).IsEqualTo(second.Id);
        await Assert.That(restored.Inventory.HeldWeapons.Single().DamageSides).IsEqualTo(4);
        var restoredTool = restored.Proficiencies.Tools.Keys.Single();
        await Assert.That(restored.Proficiencies.ForTool(restoredTool)).IsEqualTo(Training.Expertise);
        await Assert.That(restored.Proficiencies.ForWeapon(restored.Inventory.HeldWeapons.Single())).IsTrue();
        await Assert.That(restored.Proficiencies.ForSave(Ability.Constitution)).IsTrue();
        await Assert.That(restored.Proficiencies.ForArmor(ArmorCategory.Heavy)).IsTrue();
        await Assert.That(restored.Spellcasting!.KnownSpells.Count).IsEqualTo(2);
        await Assert.That(restored.Spellcasting.KnownSpells[0] == restored.Spellcasting.KnownSpells[1]).IsFalse();
        await Assert.That(restored.Spellcasting.ConcentratingOn).IsEqualTo(restored.Spellcasting.KnownSpells[0]);
        await Assert.That(restored.Spellcasting.RemainingSlots(1)).IsEqualTo(2);
        await Assert.That(restored.Spellcasting.SaveDcModifier).IsEqualTo(2);
        await Assert.That(restored.Spellcasting.RecoverOnShortRest).IsTrue();
        await Assert.That(restored.Resistances.Contains(DamageType.Fire)).IsTrue();
        await Assert.That(restored.Vulnerabilities.Contains(DamageType.Cold)).IsTrue();
        await Assert.That(restored.Immunities.Contains(DamageType.Poison)).IsTrue();
    }

    [Test]
    public async Task ConflictingRegistrationRollsBackTheWholeSaveWithoutOverwritingCredentials()
    {
        using var database = new TemporaryDatabase();
        var first = database.Open();
        var second = database.Open();
        first.Register("Alice", "original");
        first.Save();
        second.Register("Bob", "secret");
        second.Register("ALICE", "replacement");
        await Assert.That(() => second.Save()).ThrowsExactly<InvalidOperationException>();
        var restored = database.Open();
        await Assert.That(restored.Users.Count).IsEqualTo(1);
        await Assert.That(restored.Login("Alice", "original").Name).IsEqualTo("Alice");
        await Assert.That(() => restored.Login("Alice", "replacement")).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task InvalidSavedDataAndUnsupportedVersionsAreReportedWithoutResettingTheDatabase()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        accounts.Register("Alice", "secret");
        accounts.Save();
        database.Execute("UPDATE Users SET Characters = 'invalid-json';");
        await Assert.That(() => database.Open()).ThrowsExactly<InvalidDataException>();
        await Assert.That(database.Execute("SELECT Characters FROM Users;")).IsEqualTo((object)"invalid-json");
        database.Execute("PRAGMA user_version = 2;");
        await Assert.That(() => database.Open()).ThrowsExactly<InvalidDataException>();
        await Assert.That(database.Execute("PRAGMA user_version;")).IsEqualTo((object)2L);
    }

    [Test]
    public async Task ForgettingASpellDoesNotLoseItsActiveConcentrationAfterRestart()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var hero = CreationTests.Create();
        var spell = SpellCatalog.Find("Fog Cloud");
        hero.LearnSpell(spell);
        hero.PrepareSpell(spell);
        hero.Spellcasting!.Cast(hero, spell, 1);
        hero.Spellcasting.RemoveSpell(spell);
        hero.TakeDamage(new(1, DamageType.Fire));
        user.AddCharacter(hero);
        accounts.Save();
        var restored = database.Open().Login("Alice", "secret").Characters.Single();
        await Assert.That(restored.Spellcasting!.KnownSpells.Count).IsEqualTo(0);
        await Assert.That(restored.Spellcasting.AvailableSpells.Count).IsEqualTo(0);
        await Assert.That(restored.Spellcasting.ConcentratingOn).IsEqualTo(spell);
        await Assert.That(restored.Spellcasting.PendingConcentrationSaves.Single()).IsEqualTo(10);
        restored.Spellcasting.ResolveConcentrationSave(restored, new NormalDiceThrow(10));
        await Assert.That(restored.Spellcasting.ConcentratingOn).IsEqualTo(spell);
    }

    [Test]
    public async Task MissingConditionSourcesCannotProduceAnUnreadableSave()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var hero = Hero();
        user.AddCharacter(hero);
        accounts.Save();
        hero.AddCondition(Condition.Frightened, Hero());
        await Assert.That(() => accounts.Save()).ThrowsExactly<InvalidDataException>();
        var restored = database.Open().Login("Alice", "secret").Characters.Single();
        await Assert.That(restored.ActiveConditions.Contains(Condition.Frightened)).IsFalse();
    }
}
