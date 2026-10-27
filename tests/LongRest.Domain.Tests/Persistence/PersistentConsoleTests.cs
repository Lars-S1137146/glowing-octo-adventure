using LongRest.ConsoleApp;

namespace LongRest.Domain.Tests.Persistence;

public class PersistentConsoleTests
{
    [Test]
    public async Task SeparateConsoleSessionsRetainAccountsCharactersAndLaterEdits()
    {
        using var database = new TemporaryDatabase();
        var firstOutput = new StringWriter();
        new ConsoleApplication(new StringReader("""
            register Alice
            secret
            character create
            Merlin
            Human
            Wizard
            Sage

            8 10 13 15 14 12

            Dagger
            spell learn "Magic Missile"
            spell prepare "Magic Missile"
            cast "Magic Missile"
            equip Dagger
            quit
            """), firstOutput, database.Open()).Run();
        var secondOutput = new StringWriter();
        new ConsoleApplication(new StringReader("""
            login alice
            secret
            character select Merlin
            stats set strength 17
            character rename Gandalf
            spells
            cast "Magic Missile"
            logout
            register Bob
            other-secret
            character select Gandalf
            quit
            """), secondOutput, database.Open()).Run();
        var restored = database.Open();
        var wizard = restored.Login("Alice", "secret").FindCharacter("Gandalf");
        await Assert.That(firstOutput.ToString().Contains("Error:")).IsFalse();
        await Assert.That(wizard.Stats.Strength).IsEqualTo(17);
        await Assert.That(wizard.Spellcasting!.RemainingSlots(1)).IsEqualTo(0);
        await Assert.That(wizard.Inventory.HeldWeapons.Single()).IsEqualTo(EquipmentCatalog.Find("Dagger"));
        await Assert.That(secondOutput.ToString().Contains("No character with that name belongs to this account.")).IsTrue();
        await Assert.That(restored.Login("Bob", "other-secret").Characters.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EofAfterSpendingAHitDieSavesCompletedSteps()
    {
        using var database = new TemporaryDatabase();
        var accounts = database.Open();
        var user = accounts.Register("Alice", "secret");
        var hero = new Character("Fighter", new Stats(), maximumHealth: 20, hitDice: new(3, 8));
        hero.Health.Set(5, 20, 0);
        user.AddCharacter(hero);
        accounts.Save();
        new ConsoleApplication(new StringReader("login Alice\nsecret\ncharacter select Fighter\nrest short\nyes\n4\n"),
            new StringWriter(), database.Open()).Run();
        var restored = database.Open().Login("Alice", "secret").Characters.Single();
        await Assert.That(restored.Health.Value).IsEqualTo(9);
        await Assert.That(restored.HitDice.Spent).IsEqualTo(1);
    }
}
