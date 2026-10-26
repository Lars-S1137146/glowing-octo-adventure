using LongRest.ConsoleApp;

namespace LongRest.Domain.Tests.Console;

public class JourneyTests
{
    private const string CreateWizard = """
        character create
        Merlin
        Human
        Wizard
        Sage

        8 10 13 15 14 12

        Dagger, Quarterstaff
        """;

    [Test]
    public async Task RegisterCreateCastRecoverEditAndLoginAgain()
    {
        var script = "register Alice\nsecret\n" + CreateWizard + "\n" + """
            equip Dagger
            spell learn "Magic Missile"
            spell prepare "Magic Missile"
            spell learn "Fog Cloud"
            spell prepare "Fog Cloud"
            spell learn Light
            cast "Fog Cloud"
            damage 2 fire
            concentration save
            10
            cast Magic Missile 1
            rest short
            yes
            4
            temp 5
            rest long
            yes
            stats set strength 17
            spells
            logout
            login alice
            secret
            character select Merlin
            sheet
            quit
            """;
        var accounts = new UserAccounts();
        var output = new StringWriter();
        var app = new ConsoleApplication(new StringReader(script), output, accounts);
        app.Run();
        var hero = accounts.Users.Single().Characters.Single();
        await Assert.That(output.ToString().Contains("Error:")).IsFalse();
        await Assert.That(output.ToString().Contains("secret")).IsFalse();
        await Assert.That(hero.Name).IsEqualTo("Merlin");
        await Assert.That(hero.Stats.Strength).IsEqualTo(17);
        await Assert.That(hero.Inventory.HeldWeapons.Single().Name).IsEqualTo("Dagger");
        await Assert.That(hero.Health.Value).IsEqualTo(hero.Health.Max);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(0);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(0);
        await Assert.That(hero.Spellcasting!.RemainingSlots(1)).IsEqualTo(2);
        await Assert.That(hero.Spellcasting.PendingConcentrationSaves.Count).IsEqualTo(0);
        await Assert.That(app.SelectedCharacter).IsEqualTo(hero);
    }

    [Test]
    public async Task InvalidEditsAndCommandsLeaveTheSessionUsable()
    {
        var script = "register Alice\nsecret\n" + CreateWizard + "\n" + """
            stats set strength 100
            damage -5 fire
            cast Magic Missile
            nonsense
            sheet
            quit
            """;
        var accounts = new UserAccounts();
        var output = new StringWriter();
        new ConsoleApplication(new StringReader(script), output, accounts).Run();
        var hero = accounts.Users.Single().Characters.Single();
        await Assert.That(hero.Stats.Strength).IsEqualTo(9);
        await Assert.That(hero.Health.Value).IsEqualTo(8);
        await Assert.That(hero.Spellcasting!.RemainingSlots(1)).IsEqualTo(2);
        await Assert.That(output.ToString().Split("Error:").Length - 1).IsEqualTo(4);
        await Assert.That(output.ToString().Contains("HP 8/8")).IsTrue();
    }

    [Test]
    public async Task LoggingOutClearsSelectedCharacterAndPreventsCrossAccountSelection()
    {
        var script = "register Alice\nsecret\n" + CreateWizard + "\nlogout\nregister Bob\nsecret\ncharacter select Merlin\nquit\n";
        var accounts = new UserAccounts();
        var output = new StringWriter();
        var app = new ConsoleApplication(new StringReader(script), output, accounts);
        app.Run();
        await Assert.That(app.CurrentUser!.Name).IsEqualTo("Bob");
        await Assert.That(app.SelectedCharacter).IsNull();
        await Assert.That(app.CurrentUser.Characters.Count).IsEqualTo(0);
        await Assert.That(output.ToString().Contains("No character with that name belongs to this account.")).IsTrue();
    }

    [Test]
    public async Task EofDuringCreationDoesNotAddAPartialCharacter()
    {
        var accounts = new UserAccounts();
        new ConsoleApplication(new StringReader("register Alice\nsecret\ncharacter create\nMerlin\n"), new StringWriter(), accounts).Run();
        await Assert.That(accounts.Users.Single().Characters.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CommandsRequiringLoginAreRejectedAndQuitAndEofAreSafe()
    {
        var output = new StringWriter();
        new ConsoleApplication(new StringReader("character list\nquit\n"), output).Run();
        await Assert.That(output.ToString().Contains("Register or login first.")).IsTrue();
        new ConsoleApplication(new StringReader(""), output).Run();
    }
}
