using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed partial class ConsoleApplication
{
    private readonly TextReader input;
    private readonly TextWriter output;
    private readonly Func<string?> readPassword;
    private readonly UserAccounts accounts;
    public User? CurrentUser { get; private set; }
    public Character? SelectedCharacter { get; private set; }

    public ConsoleApplication(TextReader input, TextWriter output, UserAccounts? accounts = null, Func<string?>? readPassword = null)
    {
        this.input = input;
        this.output = output;
        this.accounts = accounts ?? new();
        this.readPassword = readPassword ?? input.ReadLine;
    }

    public void Run()
    {
        output.WriteLine("LongRest - D&D 5e (2014)");
        output.WriteLine("Register or login to begin. Type help for commands.");
        output.WriteLine(accounts.DatabasePath is { } path
            ? "Changes are saved automatically. Database: " + path
            : "Accounts and characters last until you quit.");
        while (true)
        {
            output.Write(SelectedCharacter is null ? "longrest> " : SelectedCharacter.Name + "> ");
            var line = input.ReadLine();
            if (line is null) return;
            try
            {
                var words = CommandLine.Split(line);
                if (words.Length == 0) continue;
                if (words[0].Equals("quit", StringComparison.OrdinalIgnoreCase)) return;
                Execute(words);
            }
            catch (EndOfStreamException) { return; }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException)
            {
                output.WriteLine("Error: " + exception.Message);
            }
            finally
            {
                // Also retain completed steps when a multi-prompt command is interrupted.
                accounts.Save();
            }
        }
    }

    private void Execute(string[] words)
    {
        var command = words[0].ToLowerInvariant();
        if (command == "help") { ShowHelp(); return; }
        if (command is "register" or "login")
        {
            if (CurrentUser is not null) throw new InvalidOperationException("Logout before switching accounts.");
            if (words.Length > 2) throw new ArgumentException("Use register [username] or login [username]. Passwords are prompted separately.");
            var name = words.Length == 2 ? words[1] : Prompt("Username");
            output.Write("Password: ");
            var password = readPassword() ?? throw new EndOfStreamException();
            CurrentUser = command == "register" ? accounts.Register(name, password) : accounts.Login(name, password);
            output.WriteLine("Logged in as " + CurrentUser.Name + ".");
            output.WriteLine("Use character create, character list, or character select <name>.");
            return;
        }
        if (CurrentUser is null) throw new InvalidOperationException("Register or login first.");
        switch (command)
        {
            case "logout":
                CurrentUser = null;
                SelectedCharacter = null;
                output.WriteLine("Logged out.");
                break;
            case "character": CharacterCommand(words); break;
            case "sheet": ShowSheet(Hero()); break;
            case "stats": StatsCommand(words); break;
            case "spells": SpellsCommand(words); break;
            case "spell": SpellCommand(words); break;
            case "cast": CastCommand(words); break;
            case "concentration": ConcentrationCommand(words); break;
            case "inventory": InventoryCommand(words); break;
            case "equip": Hero().Inventory.Equip(EquipmentCatalog.Find(NameAfter(words, 1))); output.WriteLine("Equipped."); break;
            case "unequip": Hero().Inventory.Unequip(EquipmentCatalog.Find(NameAfter(words, 1))); output.WriteLine("Unequipped."); break;
            case "damage": DamageCommand(words); break;
            case "heal":
                Exact(words, 2, "heal <amount>");
                Hero().Heal(Number(words[1]));
                output.WriteLine("HP: " + Hero().Health.Value);
                break;
            case "temp":
                Exact(words, 2, "temp <amount>");
                Hero().Health.OfferTemporary(Number(words[1]), true);
                output.WriteLine("Temporary HP: " + Hero().Health.TempHealth);
                break;
            case "rest": RestCommand(words); break;
            case "check": CheckCommand(words, savingThrow: false); break;
            case "save": CheckCommand(words, savingThrow: true); break;
            case "deathsave":
                Exact(words, 1, "deathsave");
                Hero().Health.DeathSave(ReadThrow(ThrowType.Normal)!);
                output.WriteLine($"HP: {Hero().Health.Value}, successes: {Hero().Health.DeathSuccesses}, failures: {Hero().Health.DeathFailures}, stable: {Hero().Health.IsStable}, dead: {Hero().Health.IsDead}");
                break;
            case "condition": ConditionCommand(words); break;
            default: throw new ArgumentException("Unknown command. Type help.");
        }
    }

    private Character Hero() => SelectedCharacter ?? throw new InvalidOperationException("Create or select a character first.");

    private string Prompt(string label)
    {
        output.Write(label + ": ");
        return input.ReadLine() ?? throw new EndOfStreamException();
    }

    private static string NameAfter(string[] words, int index)
    {
        if (words.Length <= index) throw new ArgumentException("A name is required.");
        return string.Join(" ", words.Skip(index));
    }

    private static int Number(string text) => int.TryParse(text, out var value) ? value :
        throw new ArgumentException("Enter a whole number.");

    private static T Choice<T>(string text) where T : struct, Enum
    {
        text = string.Concat(text.Where(character => !char.IsWhiteSpace(character) && character != '-'));
        if (Enum.TryParse<T>(text, true, out var value) && Enum.IsDefined(value)) return value;
        throw new ArgumentException("Choose one of: " + string.Join(", ", Enum.GetNames<T>()));
    }

    private static void Exact(string[] words, int count, string usage)
    {
        if (words.Length != count) throw new ArgumentException("Use " + usage + ".");
    }

    private void ShowHelp()
    {
        output.WriteLine("""
            register [username] / login [username]   Password is prompted
            logout / quit
            character create / list / select <name> / rename <name>
            sheet / stats / stats set <ability> <score>
            spells / spells catalog [level]
            spell show <name> / learn <name> / forget <name>
            spell prepare <name> / unprepare <name>
            cast <name> [slot level]                  Reaction triggers are prompted
            concentration / concentration save / concentration end
            inventory / inventory catalog
            inventory add <item> / remove <item>     One item at a time
            equip <item> / unequip <item>
            damage <amount> <type> / heal <amount> / temp <amount>
            check <ability> <DC> [skill] / save <ability> <DC>
            deathsave
            condition add <condition> / remove <condition>
            rest short / rest long
            Names with spaces may be quoted. Dice faces are supplied when prompted.
            """);
    }
}
