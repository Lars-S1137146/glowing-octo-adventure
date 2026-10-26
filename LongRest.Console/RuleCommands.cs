using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed partial class ConsoleApplication
{
    private bool Yes(string question) => Prompt(question + " [yes/no]").Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

    private DiceThrow? ReadThrow(ThrowType? type)
    {
        if (type is null) { output.WriteLine("Automatic failure; no dice needed."); return null; }
        var count = type == ThrowType.Normal ? 1 : 2;
        var faces = Prompt($"{type}: enter {count} d20 face(s), space separated").Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(Number).ToArray();
        if (faces.Length != count) throw new ArgumentException($"Supply {count} face(s).");
        return type switch
        {
            ThrowType.Normal => new NormalDiceThrow(faces[0]),
            ThrowType.Advantage => new AdvantageDiceThrow(faces[0], faces[1]),
            _ => new DisadvantageDiceThrow(faces[0], faces[1])
        };
    }

    private void CheckCommand(string[] words, bool savingThrow)
    {
        if (words.Length < 3 || words.Length > (savingThrow ? 3 : 4))
            throw new ArgumentException(savingThrow ? "Use save <ability> <DC>." : "Use check <ability> <DC> [skill].");
        var ability = Choice<Ability>(words[1]);
        var dc = Number(words[2]);
        Skill? skill = words.Length == 4 ? Choice<Skill>(words[3]) : null;
        var hero = Hero();
        var circumstance = Prompt("External roll circumstances: Normal, Advantage, Disadvantage (Enter = Normal)");
        var mode = string.IsNullOrWhiteSpace(circumstance) ? ThrowType.Normal : Choice<ThrowType>(circumstance);
        bool? fearVisible = !savingThrow && hero.ActiveConditions.Contains(Condition.Frightened) ? Yes("Is your fear source visible?") : null;
        var context = new RollContext(mode == ThrowType.Advantage ? 1 : 0, mode == ThrowType.Disadvantage ? 1 : 0, FearSourceVisible: fearVisible);
        var required = savingThrow ? hero.SaveThrowType(ability, context) : hero.CheckThrowType(ability, skill, context);
        var roll = ReadThrow(required);
        var result = savingThrow ? hero.Save(ability, roll, dc, context) : hero.Check(ability, roll, dc, skill, context);
        output.WriteLine($"{result.Total} vs DC {dc}: {(result.Success ? "success" : "failure")}");
    }

    private void DamageCommand(string[] words)
    {
        Exact(words, 3, "damage <amount> <type>");
        var result = Hero().TakeDamage(new(Number(words[1]), Choice<DamageType>(words[2])));
        output.WriteLine($"Damage taken: {result.DamageTaken}; HP: {Hero().Health.Value}/{Hero().Health.Max}; temporary HP: {Hero().Health.TempHealth}");
        if (result.ConcentrationSaveDc is not null)
            output.WriteLine($"Concentration save pending: DC {result.ConcentrationSaveDc}. Use concentration save.");
        if (Hero().Health.IsDead) output.WriteLine("The character is dead.");
        else if (Hero().Health.IsDowned) output.WriteLine("The character is unconscious at zero HP.");
    }

    private void RestCommand(string[] words)
    {
        Exact(words, 2, "rest short or rest long");
        var hero = Hero();
        switch (words[1].ToLowerInvariant())
        {
            case "long":
                if (!Yes("Have you completed a qualifying 8-hour rest, started with at least 1 HP, and had no other long-rest benefit in the past 24 game hours?"))
                    throw new InvalidOperationException("Long-rest requirements were not met.");
                hero.LongRest();
                break;
            case "short":
                if (!Yes("Have you completed at least 1 hour of rest?")) throw new InvalidOperationException("Finish the rest first.");
                // Spend one die at a time so the player can decide after seeing each result.
                hero.ShortRest(new DiceRoll(hero.HitDice.Sides));
                while (hero.HitDice.Remaining > 0)
                {
                    var face = Prompt($"HP {hero.Health.Value}/{hero.Health.Max}; roll d{hero.HitDice.Sides} to spend a Hit Die (Enter finishes)");
                    if (string.IsNullOrWhiteSpace(face)) break;
                    hero.ShortRest(new DiceRoll(hero.HitDice.Sides, Number(face)));
                }
                break;
            default: throw new ArgumentException("Use rest short or rest long.");
        }
        output.WriteLine($"Rest complete. HP {hero.Health.Value}/{hero.Health.Max}; Hit Dice {hero.HitDice.Remaining}/{hero.HitDice.Total}.");
    }

    private void InventoryCommand(string[] words)
    {
        if (words.Length == 1)
        {
            foreach (var (item, quantity) in Hero().Inventory.Items)
            {
                var equipped = Hero().Inventory.Armor == item || Hero().Inventory.Shield == item || Hero().Inventory.HeldWeapons.Contains(item);
                output.WriteLine($"{quantity} x {item.Name}" + (equipped ? " (equipped)" : ""));
            }
            if (Hero().Inventory.Items.Count == 0) output.WriteLine("Inventory is empty.");
            return;
        }
        if (words[1].Equals("catalog", StringComparison.OrdinalIgnoreCase))
        {
            Exact(words, 2, "inventory catalog");
            foreach (var item in EquipmentCatalog.All) output.WriteLine($"{item.Name} ({item.Kind})");
            return;
        }
        var equipment = EquipmentCatalog.Find(NameAfter(words, 2));
        switch (words[1].ToLowerInvariant())
        {
            case "add": Hero().Inventory.Add(equipment); break;
            case "remove": Hero().Inventory.Remove(equipment); break;
            default: throw new ArgumentException("Use inventory add <item> or inventory remove <item>.");
        }
        output.WriteLine("Updated inventory.");
    }

    private void ConditionCommand(string[] words)
    {
        if (words.Length < 3) throw new ArgumentException("Use condition add <condition> or condition remove <condition>.");
        var condition = Choice<Condition>(NameAfter(words, 2));
        switch (words[1].ToLowerInvariant())
        {
            case "add":
                Character? source = condition is Condition.Charmed or Condition.Frightened ?
                    CurrentUser!.FindCharacter(Prompt("Source character name")) : null;
                Hero().AddCondition(condition, source);
                break;
            case "remove": Hero().RemoveCondition(condition); break;
            default: throw new ArgumentException("Use condition add <condition> or condition remove <condition>.");
        }
        output.WriteLine("Conditions: " + string.Join(", ", Hero().ActiveConditions));
    }
}
