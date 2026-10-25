namespace LongRest.Domain;

public enum CombatAction { Attack, Dash, Dodge, Disengage, Help, Hide, Ready, Search, UseObject }

public sealed record InitiativeEntry(Character Character, int Die, bool Surprised = false,
    int ActionsPerTurn = 1, int AttacksPerAction = 1);

public sealed class Encounter : IDisposable
{
    private readonly HashSet<Character> reactionsSpent = [];
    private readonly HashSet<Character> surpriseEnded = [];
    private readonly HashSet<Character> dodging = [];
    private readonly HashSet<Character> disengaged = [];
    private readonly List<Spell> spellsThisTurn = [];
    private List<Character> turnOrder = [];
    private int movementSpent;
    private int dashCount;
    private int turnIndex;
    private bool started;
    private bool disposed;

    public IReadOnlyList<InitiativeEntry> Participants { get; }
    public IReadOnlyList<Character>? TieOrder { get; }
    public IReadOnlyList<Character> TurnOrder => turnOrder.AsReadOnly();
    public int RoundNumber { get; private set; }
    public Character Current => started ? turnOrder[turnIndex] : throw new InvalidOperationException("Start the encounter first.");
    public int ActionsRemaining { get; private set; }
    public int AttacksRemaining { get; private set; }
    public bool BonusActionAvailable { get; private set; }
    public int MovementRemaining => started && !Surprised(Current) && Current.CurrentSpeed > 0
        ? Math.Max(0, Current.CurrentSpeed * (1 + dashCount) - movementSpent) : 0;

    public Encounter(IEnumerable<InitiativeEntry> participants, IReadOnlyList<Character>? tieOrder = null)
    {
        ArgumentNullException.ThrowIfNull(participants);
        Participants = Array.AsReadOnly(participants.ToArray());
        TieOrder = tieOrder is null ? null : Array.AsReadOnly(tieOrder.ToArray());
        var seen = new HashSet<Character>();
        foreach (var entry in Participants)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Character);
            if (!seen.Add(entry.Character)) throw new ArgumentException("A character cannot participate twice.");
            Validation.Range(entry.Die, 1, 20, nameof(participants));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entry.ActionsPerTurn);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entry.AttacksPerAction);
        }
    }

    public void StartTurn()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) throw new InvalidOperationException("The encounter has already started.");
        if (Participants.Count == 0) throw new InvalidOperationException("An encounter needs participants.");
        var groups = Participants.GroupBy(entry => entry.Die + entry.Character.InitiativeModifier).ToArray();
        var tied = groups.Where(group => group.Count() > 1).SelectMany(group => group.Select(entry => entry.Character)).ToHashSet();
        if (tied.Count > 0 && TieOrder is null) throw new InvalidOperationException("Choose an order for tied initiative.");
        if (TieOrder is not null && (TieOrder.Count != tied.Count || TieOrder.Distinct().Count() != tied.Count ||
                TieOrder.Any(character => !tied.Contains(character))))
            throw new ArgumentException("Tie order must contain exactly the tied characters.");
        var tieIndexes = TieOrder?.Select((character, index) => (character, index)).ToDictionary(pair => pair.character, pair => pair.index);
        turnOrder = Participants.OrderByDescending(entry => entry.Die + entry.Character.InitiativeModifier)
            .ThenBy(entry => tieIndexes?.GetValueOrDefault(entry.Character) ?? 0).Select(entry => entry.Character).ToList();
        started = true;
        foreach (var entry in Participants) entry.Character.LostDodgeBenefits += LoseDodge;
        RoundNumber = 1;
        BeginCurrentTurn();
    }

    private void LoseDodge(Character character) => dodging.Remove(character);

    public void Dispose()
    {
        foreach (var entry in Participants) entry.Character.LostDodgeBenefits -= LoseDodge;
        started = false;
        disposed = true;
        dodging.Clear();
        disengaged.Clear();
    }

    private InitiativeEntry Entry(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return Participants.FirstOrDefault(entry => entry.Character == character) ??
            throw new InvalidOperationException("The character is not in this encounter.");
    }

    private bool Surprised(Character character) => Entry(character).Surprised && !surpriseEnded.Contains(character);

    private void RequireTurn(Character character, bool mustAct = true)
    {
        if (Current != character) throw new InvalidOperationException("It is another character's turn.");
        if (mustAct && (!character.CanAct || Surprised(character))) throw new InvalidOperationException("The character cannot act.");
    }

    private void BeginCurrentTurn()
    {
        var entry = Entry(Current);
        ActionsRemaining = Surprised(Current) ? 0 : entry.ActionsPerTurn;
        AttacksRemaining = 0;
        BonusActionAvailable = !Surprised(Current);
        movementSpent = 0;
        dashCount = 0;
        reactionsSpent.Remove(Current);
        dodging.Remove(Current);
        spellsThisTurn.Clear();
    }

    public void EndTurn(Character character)
    {
        RequireTurn(character, false);
        surpriseEnded.Add(character);
        disengaged.Remove(character);
        turnIndex++;
        if (turnIndex == turnOrder.Count) { turnIndex = 0; RoundNumber++; }
        BeginCurrentTurn();
    }

    public bool IsDodging(Character character)
    {
        _ = Entry(character);
        return started && dodging.Contains(character) && character.CanAct && character.CurrentSpeed > 0;
    }

    public bool IsDisengaged(Character character) { _ = Entry(character); return started && disengaged.Contains(character); }

    public bool ReactionAvailable(Character character)
    {
        _ = Entry(character);
        return started && character.CanAct && !Surprised(character) && !reactionsSpent.Contains(character);
    }

    public void TakeAction(Character character, CombatAction action)
    {
        Validation.Defined(action);
        RequireTurn(character);
        if (ActionsRemaining == 0) throw new InvalidOperationException("No action remains.");
        // Other actions only spend the allowance; their effects are adjudicated manually.
        ActionsRemaining--;
        switch (action)
        {
            case CombatAction.Attack: AttacksRemaining += Entry(character).AttacksPerAction; break;
            case CombatAction.Dash: dashCount++; break;
            case CombatAction.Dodge: dodging.Add(character); break;
            case CombatAction.Disengage: disengaged.Add(character); break;
        }
    }

    public void Move(Character character, int feet, bool movesCloserToFearSource = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(feet);
        RequireTurn(character, false);
        if (Surprised(character) || character.CurrentSpeed == 0) throw new InvalidOperationException("The character cannot move.");
        if (movesCloserToFearSource && character.Has(Condition.Frightened))
            throw new InvalidOperationException("A frightened creature cannot willingly approach the fear source.");
        var cost = checked(feet * (character.Has(Condition.Prone) ? 2 : 1));
        if (cost > MovementRemaining) throw new InvalidOperationException("Not enough movement remains.");
        movementSpent += cost;
    }

    public void StandUp(Character character)
    {
        RequireTurn(character, false);
        if (Surprised(character) || character.CurrentSpeed == 0) throw new InvalidOperationException("The character cannot stand.");
        if (!character.Has(Condition.Prone)) throw new InvalidOperationException("The character is not prone.");
        var cost = character.CurrentSpeed / 2;
        if (MovementRemaining < cost) throw new InvalidOperationException("Standing requires half your speed.");
        movementSpent += cost;
        character.RemoveCondition(Condition.Prone);
    }

    public void UseBonusAction(Character character, bool eligible)
    {
        RequireTurn(character);
        if (!eligible || !BonusActionAvailable) throw new InvalidOperationException("No eligible bonus action.");
        BonusActionAvailable = false;
    }

    public void UseReaction(Character character, bool eligibleTrigger)
    {
        if (!eligibleTrigger || !ReactionAvailable(character)) throw new InvalidOperationException("No eligible reaction.");
        reactionsSpent.Add(character);
    }

    private AttackContext AttackContextWithDodge(Character target, AttackContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return dodging.Contains(target) && IsDodging(target) && context.TargetCanSeeAttacker
            ? context with { DisadvantageSources = context.DisadvantageSources + 1 } : context;
    }

    public AttackResult Attack(Character character, Character target, Equipment? weapon,
        DiceThrow roll, AttackContext context, Ability? finesseAbility = null)
    {
        RequireTurn(character);
        if (AttacksRemaining == 0) throw new InvalidOperationException("Take the Attack action first.");
        var result = Combat.Attack(character, target, weapon, roll, AttackContextWithDodge(target, context), finesseAbility);
        AttacksRemaining--;
        return result;
    }

    public CastResult Cast(Character character, Spell spell, int castingLevel, bool eligibleReactionTrigger = false)
    {
        ArgumentNullException.ThrowIfNull(spell);
        _ = Entry(character);
        if (!started) throw new InvalidOperationException("Start the encounter first.");
        if (spell.CastingTime == CastingTime.Reaction)
        {
            if (!eligibleReactionTrigger || !ReactionAvailable(character)) throw new InvalidOperationException("No eligible reaction.");
        }
        else
        {
            RequireTurn(character);
            if (spell.CastingTime == CastingTime.Action && ActionsRemaining == 0) throw new InvalidOperationException("No action remains.");
            if (spell.CastingTime == CastingTime.BonusAction && !BonusActionAvailable) throw new InvalidOperationException("No bonus action remains.");
        }
        if (character == Current)
        {
            if (spellsThisTurn.Any(previous => previous.CastingTime == CastingTime.BonusAction) &&
                !(spell.Level == 0 && spell.CastingTime == CastingTime.Action))
                throw new InvalidOperationException("After a bonus action spell, only an action cantrip can be cast on this turn.");
            if (spell.CastingTime == CastingTime.BonusAction &&
                spellsThisTurn.Any(previous => previous.Level != 0 || previous.CastingTime != CastingTime.Action))
                throw new InvalidOperationException("Other spells this turn prevent a bonus action spell.");
        }
        var magic = character.Spellcasting ?? throw new InvalidOperationException("The character has no spellcasting.");
        var result = magic.Cast(character, spell, castingLevel, eligibleReactionTrigger);
        switch (spell.CastingTime)
        {
            case CastingTime.Action: ActionsRemaining--; break;
            case CastingTime.BonusAction: BonusActionAvailable = false; break;
            case CastingTime.Reaction: reactionsSpent.Add(character); break;
        }
        if (character == Current) spellsThisTurn.Add(spell);
        return result;
    }

    public CheckResult Save(Character character, Ability ability, DiceThrow? roll, int dc, RollContext? context = null)
    {
        if (!started) throw new InvalidOperationException("Start the encounter first.");
        _ = Entry(character);
        context ??= new();
        if (ability == Ability.Dexterity && IsDodging(character))
            context = context with { AdvantageSources = context.AdvantageSources + 1 };
        return character.Save(ability, roll, dc, context);
    }
}
