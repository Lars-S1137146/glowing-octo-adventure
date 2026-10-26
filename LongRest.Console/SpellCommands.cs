using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed partial class ConsoleApplication
{
    private Spellcasting Magic() => Hero().Spellcasting ?? throw new InvalidOperationException("This character has no spellcasting.");

    private void SpellsCommand(string[] words)
    {
        var hero = Hero();
        if (words.Length > 1)
        {
            if (!words[1].Equals("catalog", StringComparison.OrdinalIgnoreCase) || words.Length > 3)
                throw new ArgumentException("Use spells or spells catalog [level].");
            int? level = words.Length == 3 ? Number(words[2]) : null;
            if (level is < 0 or > 9) throw new ArgumentException("Spell level must be 0 through 9.");
            foreach (var spell in SpellCatalog.All.Where(spell => spell.Classes.Contains(hero.Class.Name) && (level is null || spell.Level == level)))
                output.WriteLine($"{spell.Name} | level {spell.Level} | {spell.CastingTime}" + (spell.Concentration ? " | concentration" : ""));
            return;
        }
        var magic = Magic();
        output.WriteLine("Cantrips: " + string.Join(", ", magic.Cantrips.Select(spell => spell.Name)));
        output.WriteLine("Known / spellbook: " + string.Join(", ", magic.KnownSpells.Select(spell => spell.Name)));
        output.WriteLine($"{magic.Availability}: " + string.Join(", ", magic.AvailableSpells.Select(spell => spell.Name)));
        foreach (var (level, maximum) in magic.MaximumSlots.OrderBy(slot => slot.Key))
            output.WriteLine($"Level {level} slots: {magic.RemainingSlots(level)}/{maximum}");
        output.WriteLine($"Spell attack: {magic.AttackModifier(hero):+0;-0;0}, save DC: {magic.SaveDc(hero)}");
        ShowConcentration(magic);
    }

    private void SpellCommand(string[] words)
    {
        if (words.Length < 3) throw new ArgumentException("Use spell show, learn, forget, prepare, or unprepare <name>.");
        var spell = SpellCatalog.Find(NameAfter(words, 2));
        switch (words[1].ToLowerInvariant())
        {
            case "show":
                output.WriteLine($"{spell.Name} | level {spell.Level} {spell.School}");
                output.WriteLine($"{spell.CastingTime} | Range: {spell.Range} | Duration: {spell.Duration}");
                output.WriteLine($"Components: {spell.Components} | Ritual: {spell.Ritual}");
                output.WriteLine("Classes: " + string.Join(", ", spell.Classes));
                output.WriteLine("Spell effects and component costs are adjudicated manually.");
                return;
            case "learn": Hero().LearnSpell(spell); break;
            case "prepare": Hero().PrepareSpell(spell); break;
            case "unprepare": Magic().UnprepareSpell(spell); break;
            case "forget": Magic().RemoveSpell(spell); break;
            default: throw new ArgumentException("Unknown spell command.");
        }
        output.WriteLine("Updated spells.");
    }

    private void CastCommand(string[] words)
    {
        if (words.Length < 2) throw new ArgumentException("Use cast <name> [slot level].");
        int? level = null;
        var nameWords = words.Skip(1).ToList();
        if (nameWords.Count > 1 && int.TryParse(nameWords[^1], out var selectedLevel))
        {
            level = selectedLevel;
            nameWords.RemoveAt(nameWords.Count - 1);
        }
        var spell = SpellCatalog.Find(string.Join(" ", nameWords));
        var triggered = spell.CastingTime == CastingTime.Reaction && Yes("Has this spell's reaction trigger occurred?");
        var result = Magic().Cast(Hero(), spell, level ?? spell.Level, triggered);
        output.WriteLine($"Cast {result.Spell.Name} at level {result.CastingLevel}. Apply its effects manually.");
        if (result.CastingLevel > 0) output.WriteLine($"Level {result.CastingLevel} slots left: {Magic().RemainingSlots(result.CastingLevel)}");
        ShowConcentration(Magic());
    }

    private void ShowConcentration(Spellcasting magic)
    {
        output.WriteLine("Concentration: " + (magic.ConcentratingOn?.Name ?? "none"));
        if (magic.PendingConcentrationSaves.Count > 0)
            output.WriteLine("Pending Constitution saves, in order: " + string.Join(", ", magic.PendingConcentrationSaves));
    }

    private void ConcentrationCommand(string[] words)
    {
        var magic = Magic();
        if (words.Length == 1) { ShowConcentration(magic); return; }
        Exact(words, 2, "concentration [save|end]");
        switch (words[1].ToLowerInvariant())
        {
            case "end": magic.EndConcentration(); break;
            case "save":
                if (magic.PendingConcentrationSaves.Count == 0) throw new InvalidOperationException("No concentration save is pending.");
                var result = magic.ResolveConcentrationSave(Hero(), ReadThrow(Hero().SaveThrowType(Ability.Constitution)));
                output.WriteLine(result.Success ? "Concentration maintained." : "Concentration ended.");
                break;
            default: throw new ArgumentException("Use concentration, concentration save, or concentration end.");
        }
        ShowConcentration(magic);
    }
}
