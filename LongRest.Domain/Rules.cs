namespace LongRest.Domain;

public enum Ability
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

public enum Skill
{
    Acrobatics,
    AnimalHandling,
    Arcana,
    Athletics,
    Deception,
    History,
    Insight,
    Intimidation,
    Investigation,
    Medicine,
    Nature,
    Perception,
    Performance,
    Persuasion,
    Religion,
    SleightOfHand,
    Stealth,
    Survival
}

public enum Training
{
    None,
    Proficient,
    Expertise
}

public enum Cover
{
    None,
    Half,
    ThreeQuarters,
    Total
}

public enum DamageType
{
    Acid,
    Bludgeoning,
    Cold,
    Fire,
    Force,
    Lightning,
    Necrotic,
    Piercing,
    Poison,
    Psychic,
    Radiant,
    Slashing,
    Thunder
}

public enum Condition
{
    Blinded,
    Charmed,
    Deafened,
    Frightened,
    Grappled,
    Incapacitated,
    Invisible,
    Paralyzed,
    Petrified,
    Poisoned,
    Prone,
    Restrained,
    Stunned,
    Unconscious
}

public sealed record PointBuyResult(
    int Spent,
    int Remaining);

public static class PointBuy
{
    // Milestone: Point Buy (PHB 1). Exactly six base scores; no character mutation.
    public static PointBuyResult Validate(params int[] scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        if (scores.Length != 6) throw new ArgumentException("Supply exactly six scores.", nameof(scores));
        int[] costs = [0, 1, 2, 3, 4, 5, 7, 9];
        foreach (var score in scores) Validation.Range(score, 8, 15, nameof(scores));
        var spent = scores.Sum(score => costs[score - 8]);
        if (spent > 27) throw new ArgumentException("Point buy exceeds 27 points.", nameof(scores));
        return new(spent, 27 - spent);
    }
}

