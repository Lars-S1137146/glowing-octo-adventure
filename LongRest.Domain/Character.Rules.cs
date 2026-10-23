namespace LongRest.Domain;

public sealed partial class Character
{
    private void ValidateContext(Ability ability, RollContext context)
    {
        Validation.Defined(ability);
        Validation.Defined(context.Cover);
        ArgumentOutOfRangeException.ThrowIfNegative(context.AdvantageSources);
        ArgumentOutOfRangeException.ThrowIfNegative(context.DisadvantageSources);
        if (Has(Condition.Frightened) && context.FearSourceVisible is null)
            throw new ArgumentException("Supply whether the fear source is visible.");
    }

    public ThrowType? CheckThrowType(Ability ability, Skill? skill = null, RollContext? context = null)
    {
        context ??= new();
        ValidateContext(ability, context);
        if (skill is not null) Validation.Defined(skill.Value);
        if ((context.RequiresSight && Has(Condition.Blinded)) || (context.RequiresHearing && Has(Condition.Deafened)))
            return null;
        var advantage = context.AdvantageSources;
        var disadvantage = context.DisadvantageSources;
        if (Has(Condition.Poisoned) || (Has(Condition.Frightened) && context.FearSourceVisible == true))
            disadvantage++;
        if (HasUntrainedArmor && ability is Ability.Strength or Ability.Dexterity) disadvantage++;
        if (skill == Skill.Stealth && Inventory.Armor?.StealthDisadvantage == true) disadvantage++;
        if (context.SocialTarget?.ConditionSource(Condition.Charmed) == this) advantage++;
        return DiceThrow.RequiredType(advantage, disadvantage);
    }

    public ThrowType? SaveThrowType(Ability ability, RollContext? context = null)
    {
        context ??= new();
        // Fear affects checks and attacks, not saving throws.
        Validation.Defined(ability);
        Validation.Defined(context.Cover);
        ArgumentOutOfRangeException.ThrowIfNegative(context.AdvantageSources);
        ArgumentOutOfRangeException.ThrowIfNegative(context.DisadvantageSources);
        if (ability is Ability.Strength or Ability.Dexterity &&
            HasAny(Condition.Paralyzed, Condition.Petrified, Condition.Stunned, Condition.Unconscious)) return null;
        var disadvantage = context.DisadvantageSources;
        if (HasUntrainedArmor && ability is Ability.Strength or Ability.Dexterity) disadvantage++;
        if (ability == Ability.Dexterity && Has(Condition.Restrained)) disadvantage++;
        return DiceThrow.RequiredType(context.AdvantageSources, disadvantage);
    }

    public CheckResult Check(Ability ability, DiceThrow? roll, int dc, Skill? skill = null,
        RollContext? context = null, Equipment? tool = null)
    {
        context ??= new();
        var required = CheckThrowType(ability, skill, context);
        if (tool is not null && tool.Kind != EquipmentKind.Tool) throw new ArgumentException("Supply a tool definition.");
        if (required is null) return new(0, 0, false, true);
        ArgumentNullException.ThrowIfNull(roll);
        roll.Require(required.Value);
        var training = skill is null ? Training.None : Proficiencies.ForSkill(skill.Value);
        if (tool is not null) training = (Training)Math.Max((int)training, (int)Proficiencies.ForTool(tool));
        var total = roll.Value + Stats.GetModifier(ability) + ProficiencyBonus * (int)training + context.Modifier;
        return new(roll.Value, total, total >= dc);
    }

    public CheckResult Save(Ability ability, DiceThrow? roll, int dc, RollContext? context = null)
    {
        context ??= new();
        var required = SaveThrowType(ability, context);
        if (required is null) return new(0, 0, false, true);
        ArgumentNullException.ThrowIfNull(roll);
        roll.Require(required.Value);
        var total = roll.Value + Stats.GetModifier(ability) + context.Modifier +
            (Proficiencies.ForSave(ability) ? ProficiencyBonus : 0) +
            (ability == Ability.Dexterity ? Combat.CoverBonus(context.Cover) : 0);
        return new(roll.Value, total, total >= dc);
    }

    public ContestResult Contest(Character opponent, Ability ability, Skill? skill, DiceThrow? roll,
        Ability opponentAbility, Skill? opponentSkill, DiceThrow? opponentRoll,
        RollContext? context = null, RollContext? opponentContext = null)
    {
        ArgumentNullException.ThrowIfNull(opponent);
        var first = Check(ability, roll, 0, skill, context);
        var second = opponent.Check(opponentAbility, opponentRoll, 0, opponentSkill, opponentContext);
        var outcome = first.AutomaticFailure && !second.AutomaticFailure ? ContestOutcome.SecondWins :
            second.AutomaticFailure && !first.AutomaticFailure ? ContestOutcome.FirstWins :
            first.Total > second.Total ? ContestOutcome.FirstWins :
            first.Total < second.Total ? ContestOutcome.SecondWins : ContestOutcome.Tie;
        return new(first, second, outcome);
    }

}

