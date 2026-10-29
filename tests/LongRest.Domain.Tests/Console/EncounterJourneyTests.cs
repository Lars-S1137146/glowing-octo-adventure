using LongRest.ConsoleApp;

namespace LongRest.Domain.Tests.Console;

public class EncounterJourneyTests
{
    private const string CreateFighter = """
        character create
        Arthur
        Human
        Fighter
        Soldier

        15 14 13 12 10 8

        Longsword
        """;

    private const string CreateGoblin = """
        character create
        Goblin
        Human
        Rogue
        Criminal

        8 15 14 12 10 13

        Dagger
        """;

    [Test]
    public async Task StartEncounterAttackAndEndTurnJourney()
    {
        var script = "register Alice\nsecret\n" + CreateFighter + "\n" + CreateGoblin + "\n" + """
            character select Arthur
            equip Longsword
            character select Goblin
            equip Dagger
            character select Arthur
            encounter start Arthur Goblin
            15
            no
            10
            no
            encounter
            encounter action dodge
            encounter endturn
            encounter action attack
            encounter attack Arthur Dagger
            5
            None
            no
            Dexterity
            yes
            yes
            no
            18 16
            4
            encounter move 10
            encounter endturn
            encounter end
            quit
            """;

        var accounts = new UserAccounts();
        var output = new StringWriter();
        var app = new ConsoleApplication(new StringReader(script), output, accounts);
        app.Run();

        var text = output.ToString();
        if (text.Contains("Error:")) throw new Exception(text);
        await Assert.That(text.Contains("Encounter started! Round 1.")).IsTrue();
        await Assert.That(text.Contains("Arthur took action: Dodge.")).IsTrue();
        await Assert.That(text.Contains("Ended turn for Arthur.")).IsTrue();
        await Assert.That(text.Contains("Current turn: Goblin")).IsTrue();
        await Assert.That(text.Contains("Dealt 7 Piercing damage to Arthur.")).IsTrue();
        await Assert.That(text.Contains("Goblin moved 10 ft.")).IsTrue();
        await Assert.That(text.Contains("Encounter ended.")).IsTrue();
        await Assert.That(app.ActiveEncounter).IsNull();
    }

    [Test]
    public async Task StandaloneAttackCommandDamagesTarget()
    {
        var script = "register Alice\nsecret\n" + CreateFighter + "\n" + CreateGoblin + "\n" + """
            character select Arthur
            equip Longsword
            attack Goblin Longsword
            5
            None
            no
            yes
            yes
            no
            16
            7
            quit
            """;

        var accounts = new UserAccounts();
        var output = new StringWriter();
        var app = new ConsoleApplication(new StringReader(script), output, accounts);
        app.Run();

        var text = output.ToString();
        if (text.Contains("Error:")) throw new Exception(text);
        await Assert.That(text.Contains("HIT!")).IsTrue();
        await Assert.That(text.Contains("Dealt 10 Slashing damage to Goblin.")).IsTrue();
    }

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
    public async Task EncounterTiedInitiativeAndSpellcastingAndProneRecovery()
    {
        var script = "register Alice\nsecret\n" + CreateWizard + "\n" + CreateGoblin + "\n" + """
            character select Merlin
            spell learn "Magic Missile"
            spell prepare "Magic Missile"
            character select Goblin
            condition add Prone
            encounter start Merlin Goblin
            13
            no
            10
            no
            Goblin, Merlin
            encounter
            encounter stand
            encounter move 15
            encounter bonus
            encounter endturn
            encounter cast "Magic Missile" 1
            encounter endturn
            encounter end
            quit
            """;

        var accounts = new UserAccounts();
        var output = new StringWriter();
        var app = new ConsoleApplication(new StringReader(script), output, accounts);
        app.Run();

        var text = output.ToString();
        if (text.Contains("Error:")) throw new Exception(text);
        await Assert.That(text.Contains("Tied initiative for total 13: Merlin, Goblin")).IsTrue();
        await Assert.That(text.Contains("Goblin stood up. Movement remaining: 15 ft.")).IsTrue();
        await Assert.That(text.Contains("Goblin used bonus action.")).IsTrue();
        await Assert.That(text.Contains("Merlin cast Magic Missile at level 1.")).IsTrue();
    }
}
