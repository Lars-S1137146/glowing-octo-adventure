using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed partial class ConsoleApplication
{
    private void CharacterCommand(string[] words)
    {
        if (words.Length < 2) throw new ArgumentException("Use character create, list, select <name>, or rename <name>.");
        switch (words[1].ToLowerInvariant())
        {
            case "create":
                Exact(words, 2, "character create");
                var character = CreateCharacter();
                CurrentUser!.AddCharacter(character);
                SelectedCharacter = character;
                output.WriteLine("Created " + character.Name + ".");
                ShowSheet(character);
                break;
            case "list":
                Exact(words, 2, "character list");
                if (CurrentUser!.Characters.Count == 0) output.WriteLine("No characters yet. Use character create.");
                foreach (var hero in CurrentUser.Characters)
                    output.WriteLine($"{hero.Name} - level {hero.Level} {hero.Race.Name} {hero.Class.Name}");
                break;
            case "select":
                SelectedCharacter = CurrentUser!.FindCharacter(NameAfter(words, 2));
                ShowSheet(SelectedCharacter);
                break;
            case "rename":
                CurrentUser!.RenameCharacter(Hero(), NameAfter(words, 2));
                output.WriteLine("Renamed to " + Hero().Name + ".");
                break;
            default: throw new ArgumentException("Unknown character command.");
        }
    }

    private T Select<T>(string label, IReadOnlyList<T> choices, Func<T, string> name)
    {
        for (var i = 0; i < choices.Count; i++) output.WriteLine($"{i + 1}. {name(choices[i])}");
        var answer = Prompt(label);
        if (int.TryParse(answer, out var number) && number >= 1 && number <= choices.Count) return choices[number - 1];
        foreach (var value in choices)
            if (name(value).Equals(answer.Trim(), StringComparison.OrdinalIgnoreCase)) return value;
        throw new ArgumentException("Choose a listed name or number.");
    }

    private Skill[] SelectSkills(string label, IEnumerable<Skill> options, int count)
    {
        if (count == 0) return [];
        var available = options.ToArray();
        output.WriteLine("Options: " + string.Join(", ", available));
        var answer = Prompt($"{label}: choose {count}, comma separated (Enter takes the first {count})");
        var selected = string.IsNullOrWhiteSpace(answer) ? available.Take(count).ToArray() :
            answer.Split(',').Select(Choice<Skill>).ToArray();
        if (selected.Length != count || selected.Distinct().Count() != count || selected.Any(skill => !available.Contains(skill)))
            throw new ArgumentException("Choose the required number of distinct skills from the list.");
        return selected;
    }

    private Character CreateCharacter()
    {
        var name = Prompt("Character name");
        if (CurrentUser!.Characters.Any(character => character.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("You already have a character with that name.");
        var race = Select("Race", CharacterCatalog.Races, race => race.Name);
        var characterClass = Select("Class", CharacterCatalog.Classes, characterClass => characterClass.Name);
        var background = Select("Background", CharacterCatalog.Backgrounds, background => background.Name);
        var methodText = Prompt("Ability method: StandardArray, PointBuy, Rolled (Enter = StandardArray)");
        var method = string.IsNullOrWhiteSpace(methodText) ? AbilityScoreMethod.StandardArray : Choice<AbilityScoreMethod>(methodText);
        var scoreText = Prompt("Scores in STR DEX CON INT WIS CHA order (Enter = 15 14 13 12 10 8)");
        var values = string.IsNullOrWhiteSpace(scoreText) ? new[] { 15, 14, 13, 12, 10, 8 } :
            scoreText.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(Number).ToArray();
        if (values.Length != 6) throw new ArgumentException("Supply all six scores.");
        var stats = new Stats(values[0], values[1], values[2], values[3], values[4], values[5]);
        Ability[] boosts = [];
        if (race.AbilityChoices > 0)
            boosts = Prompt($"Choose {race.AbilityChoices} abilities to increase, excluding Charisma (comma separated)")
                .Split(',').Select(Choice<Ability>).ToArray();
        var inheritedSkills = background.Skills.Concat(race.Skills).Distinct().ToArray();
        var replacements = SelectSkills("Replace overlapping background skills", Enum.GetValues<Skill>().Except(inheritedSkills),
            background.Skills.Count + race.Skills.Count - inheritedSkills.Length);
        inheritedSkills = inheritedSkills.Concat(replacements).ToArray();
        var classSkills = SelectSkills("Class skills", characterClass.SkillChoices.Except(inheritedSkills), characterClass.SkillCount);
        var raceSkills = SelectSkills("Racial skills", Enum.GetValues<Skill>().Except(inheritedSkills).Except(classSkills), race.SkillChoices);
        DamageType? ancestry = race.Name == "Dragonborn" ?
            Choice<DamageType>(Prompt("Draconic ancestry resistance: Acid, Cold, Fire, Lightning, Poison")) : null;
        var hero = CharacterCreator.Create(name, stats, race, characterClass, background, method, classSkills, boosts, raceSkills, ancestry, replacements);
        var equipmentText = Prompt("Starting equipment: comma-separated catalog names (Enter = choose later)");
        var equipment = string.IsNullOrWhiteSpace(equipmentText) ? [] : equipmentText.Split(',').Select(name => EquipmentCatalog.Find(name.Trim())).ToArray();
        foreach (var item in equipment) hero.Inventory.Add(item);
        output.WriteLine("Equipment and spell choices can be set with inventory add, equip, spell learn, and spell prepare.");
        output.WriteLine("Class features, racial abilities, tools/languages, and background features need manual adjudication.");
        return hero;
    }

    private void StatsCommand(string[] words)
    {
        if (words.Length == 1) { ShowSheet(Hero()); return; }
        Exact(words, 4, "stats set <ability> <score>");
        if (!words[1].Equals("set", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Use stats set <ability> <score>.");
        Hero().Stats.Set(Choice<Ability>(words[2]), Number(words[3]));
        ShowSheet(Hero());
    }

    private void ShowSheet(Character hero)
    {
        output.WriteLine($"{hero.Name} - Level {hero.Level} {hero.Race.Name} {hero.Class.Name} ({hero.Background.Name})");
        output.WriteLine($"HP {hero.Health.Value}/{hero.Health.Max} | Temp {hero.Health.TempHealth} | AC {hero.ArmorClass} | Speed {hero.CurrentSpeed} ft | Proficiency +{hero.ProficiencyBonus}");
        foreach (var ability in Enum.GetValues<Ability>())
            output.WriteLine($"{ability,-12} {hero.Stats.GetScore(ability),2} ({hero.Stats.GetModifier(ability):+0;-0;0})");
        output.WriteLine("Skills: " + string.Join(", ", Enum.GetValues<Skill>().Where(skill => hero.Proficiencies.ForSkill(skill) != Training.None)));
        output.WriteLine("Conditions: " + (hero.ActiveConditions.Count == 0 ? "none" : string.Join(", ", hero.ActiveConditions)));
        output.WriteLine($"Hit Dice: {hero.HitDice.Remaining}/{hero.HitDice.Total} d{hero.HitDice.Sides} | Dead: {hero.Health.IsDead}");
    }
}
