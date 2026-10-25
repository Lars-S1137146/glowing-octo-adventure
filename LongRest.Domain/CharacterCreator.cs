namespace LongRest.Domain;

public enum AbilityScoreMethod { StandardArray, PointBuy, Rolled }

public static class CharacterCreator
{
    public static Character Create(string name, Stats baseScores, Race race, Class characterClass,
        Background background, AbilityScoreMethod method, IEnumerable<Skill> classSkills,
        IEnumerable<Ability>? racialAbilityChoices = null, IEnumerable<Skill>? racialSkillChoices = null,
        DamageType? dragonAncestry = null, IEnumerable<Skill>? backgroundReplacements = null)
    {
        ArgumentNullException.ThrowIfNull(baseScores);
        ArgumentNullException.ThrowIfNull(race);
        ArgumentNullException.ThrowIfNull(characterClass);
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(classSkills);
        Validation.Defined(method);
        var scores = Enum.GetValues<Ability>().Select(baseScores.GetScore).ToArray();
        if (method == AbilityScoreMethod.StandardArray &&
            !scores.Order().SequenceEqual(new[] { 8, 10, 12, 13, 14, 15 }))
            throw new ArgumentException("Assign 15, 14, 13, 12, 10 and 8 once each.");
        if (method == AbilityScoreMethod.PointBuy) PointBuy.Validate(scores);
        if (method == AbilityScoreMethod.Rolled)
            foreach (var score in scores) Validation.Range(score, 3, 18, nameof(baseScores));

        var boosts = racialAbilityChoices?.ToArray() ?? [];
        if (boosts.Length != race.AbilityChoices || boosts.Distinct().Count() != boosts.Length ||
            boosts.Any(ability => race.AbilityBonuses.ContainsKey(ability)))
            throw new ArgumentException("Choose distinct racial ability increases, excluding fixed increases.");
        foreach (var ability in boosts) Validation.Defined(ability);
        var stats = new Stats(scores[0], scores[1], scores[2], scores[3], scores[4], scores[5]);
        foreach (var (ability, bonus) in race.AbilityBonuses) stats.Set(ability, stats.GetScore(ability) + bonus);
        foreach (var ability in boosts) stats.Set(ability, stats.GetScore(ability) + 1);

        var inherited = race.Skills.Concat(background.Skills).ToHashSet();
        var missingBackgroundSkills = background.Skills.Count + race.Skills.Count - inherited.Count;
        var replacements = backgroundReplacements?.ToArray() ?? [];
        if (replacements.Length != missingBackgroundSkills || replacements.Distinct().Count() != replacements.Length ||
            replacements.Any(inherited.Contains))
            throw new ArgumentException("Replace overlapping racial and background skills with distinct new skills.");
        foreach (var skill in replacements) { Validation.Defined(skill); inherited.Add(skill); }
        var selectedSkills = classSkills.ToArray();
        if (selectedSkills.Length != characterClass.SkillCount || selectedSkills.Distinct().Count() != selectedSkills.Length ||
            selectedSkills.Any(skill => !characterClass.SkillChoices.Contains(skill) || inherited.Contains(skill)))
            throw new ArgumentException("Choose the required number of distinct class skills.");
        var raceSkills = racialSkillChoices?.ToArray() ?? [];
        if (raceSkills.Length != race.SkillChoices || raceSkills.Distinct().Count() != raceSkills.Length ||
            raceSkills.Any(skill => inherited.Contains(skill) || selectedSkills.Contains(skill)))
            throw new ArgumentException("Choose the required number of racial skills.");
        foreach (var skill in raceSkills) Validation.Defined(skill);
        var skills = inherited.Concat(selectedSkills).Concat(raceSkills);
        var training = skills.Distinct().ToDictionary(skill => skill, _ => Training.Proficient);
        var resistances = race.Resistances.ToList();
        if (race.Name == "Dragonborn")
        {
            if (dragonAncestry is not (DamageType.Acid or DamageType.Cold or DamageType.Fire or DamageType.Lightning or DamageType.Poison))
                throw new ArgumentException("Choose the damage type for your draconic ancestry.");
            resistances.Add(dragonAncestry.Value);
        }
        var proficiencies = new Proficiencies(training, characterClass.SavingThrows, characterClass.Weapons,
            characterClass.Armor.Concat(race.Armor), specificWeapons: characterClass.SpecificWeapons.Concat(race.Weapons));
        Spellcasting? magic = null;
        if (characterClass.SpellcastingAbility is { } castingAbility)
        {
            magic = new(castingAbility, characterClass.SpellAvailability, [], [],
                new Dictionary<int, int> { [1] = characterClass.StartingSlots }, recoverOnShortRest: characterClass.PactMagic);
        }
        var hp = characterClass.HitDie + stats.GetModifier(Ability.Constitution) + race.HitPointBonus;
        return new Character(name, stats, Math.Max(1, hp), speed: race.Speed, proficiencies: proficiencies,
            hitDice: new(1, characterClass.HitDie), spellcasting: magic, resistances: resistances,
            characterClass: characterClass, race: race, background: background);
    }
}
