namespace LongRest.Domain;

public sealed record CastResult(Spell Spell, int CastingLevel);

public sealed class Spellcasting
{
    private readonly List<Spell> availableSpells;
    private readonly List<Spell> cantrips;
    private readonly List<Spell> knownSpells;
    private readonly Dictionary<int, int> maximumSlots;
    private readonly Dictionary<int, int> remainingSlots;
    private readonly Queue<int> pendingSaves = [];
    private Spell? concentratingOn;
    public bool RecoverOnShortRest { get; }

    public Ability Ability { get; }
    public SpellAvailability Availability { get; }
    public IReadOnlyList<Spell> AvailableSpells => availableSpells.AsReadOnly();
    public IReadOnlyList<Spell> Cantrips => cantrips.AsReadOnly();
    public IReadOnlyList<Spell> KnownSpells => knownSpells.AsReadOnly();
    public IReadOnlyDictionary<int, int> MaximumSlots => new System.Collections.ObjectModel.ReadOnlyDictionary<int, int>(maximumSlots);
    public int SaveDcModifier { get; }
    public Spell? ConcentratingOn => concentratingOn;
    public IReadOnlyList<int> PendingConcentrationSaves => Array.AsReadOnly(pendingSaves.ToArray());

    public Spellcasting(Ability ability, SpellAvailability availability,
        IEnumerable<Spell> availableSpells, IEnumerable<Spell> cantrips,
        IReadOnlyDictionary<int, int> slots, int saveDcModifier = 0, bool recoverOnShortRest = false)
    {
        Validation.Defined(ability);
        Validation.Defined(availability);
        ArgumentNullException.ThrowIfNull(availableSpells);
        ArgumentNullException.ThrowIfNull(cantrips);
        ArgumentNullException.ThrowIfNull(slots);
        this.availableSpells = availableSpells.Distinct().ToList();
        knownSpells = this.availableSpells.ToList();
        this.cantrips = cantrips.Distinct().ToList();
        foreach (var spell in this.availableSpells)
        {
            ArgumentNullException.ThrowIfNull(spell);
            if (spell.Level == 0) throw new ArgumentException("Put cantrips in the cantrip list.");
        }
        foreach (var spell in this.cantrips)
        {
            ArgumentNullException.ThrowIfNull(spell);
            if (spell.Level != 0) throw new ArgumentException("Cantrip level must be zero.");
        }
        maximumSlots = slots.ToDictionary();
        foreach (var (level, count) in maximumSlots)
        {
            Validation.Range(level, 1, 9, nameof(slots));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
        }
        remainingSlots = maximumSlots.ToDictionary();
        Ability = ability;
        Availability = availability;
        SaveDcModifier = saveDcModifier;
        RecoverOnShortRest = recoverOnShortRest;
    }

    public int RemainingSlots(int level)
    {
        Validation.Range(level, 1, 9, nameof(level));
        return remainingSlots.GetValueOrDefault(level);
    }

    public int AttackModifier(Character caster)
    {
        ArgumentNullException.ThrowIfNull(caster);
        return caster.ProficiencyBonus + caster.Stats.GetModifier(Ability);
    }

    public int SaveDc(Character caster) => 8 + AttackModifier(caster) + SaveDcModifier;

    public CastResult Cast(Character caster, Spell spell, int castingLevel, bool eligibleReactionTrigger = false)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        Validation.Range(castingLevel, 0, 9, nameof(castingLevel));
        if (caster.Spellcasting != this) throw new InvalidOperationException("These spell resources belong to another caster.");
        if (!caster.CanCast) throw new InvalidOperationException("The caster cannot cast while incapacitated or wearing untrained armor.");
        if (spell.CastingTime == CastingTime.Longer) throw new InvalidOperationException("Longer casting times require manual adjudication.");
        if (spell.CastingTime == CastingTime.Reaction && !eligibleReactionTrigger)
            throw new InvalidOperationException("A reaction spell requires its trigger.");
        if (spell.Level == 0)
        {
            if (!cantrips.Contains(spell) || castingLevel != 0) throw new InvalidOperationException("Unavailable cantrip or invalid casting level.");
        }
        else
        {
            if (!availableSpells.Contains(spell)) throw new InvalidOperationException("The spell is not known or prepared.");
            if (castingLevel < spell.Level || RemainingSlots(castingLevel) == 0)
                throw new InvalidOperationException("No suitable spell slot remains.");
        }
        if (spell.Level > 0) remainingSlots[castingLevel]--;
        if (spell.Concentration)
        {
            EndConcentration();
            concentratingOn = spell;
        }
        return new(spell, castingLevel);
    }

    public void LearnSpell(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        var list = spell.Level == 0 ? cantrips : knownSpells;
        if (!list.Contains(spell)) list.Add(spell);
        if (spell.Level > 0 && Availability == SpellAvailability.Known && !availableSpells.Contains(spell)) availableSpells.Add(spell);
    }

    public void PrepareSpell(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if (Availability != SpellAvailability.Prepared || spell.Level == 0)
            throw new InvalidOperationException("Only leveled spells can be prepared by a prepared caster.");
        if (!availableSpells.Contains(spell)) availableSpells.Add(spell);
    }

    public void UnprepareSpell(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if (Availability != SpellAvailability.Prepared) throw new InvalidOperationException("This caster uses known spells.");
        availableSpells.Remove(spell);
    }

    public void RemoveSpell(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        availableSpells.Remove(spell);
        cantrips.Remove(spell);
        knownSpells.Remove(spell);
    }

    public void EndConcentration()
    {
        concentratingOn = null;
        pendingSaves.Clear();
    }

    internal void QueueConcentrationSave(int dc) => pendingSaves.Enqueue(dc);

    public CheckResult ResolveConcentrationSave(Character caster, DiceThrow? roll)
    {
        ArgumentNullException.ThrowIfNull(caster);
        if (caster.Spellcasting != this) throw new InvalidOperationException("Wrong caster.");
        if (pendingSaves.Count == 0) throw new InvalidOperationException("There is no pending concentration save.");
        var result = caster.Save(Ability.Constitution, roll, pendingSaves.Peek());
        if (result.Success) pendingSaves.Dequeue();
        else EndConcentration();
        return result;
    }

    internal void RestoreSlots()
    {
        foreach (var (level, count) in maximumSlots) remainingSlots[level] = count;
    }

    /// <summary>Restores established resources without casting spells or consuming slots.</summary>
    public void RestoreResources(IReadOnlyDictionary<int, int> slots, Spell? concentration, IEnumerable<int> pending)
    {
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentNullException.ThrowIfNull(pending);
        if (slots.Count != maximumSlots.Count || slots.Keys.Any(level => !maximumSlots.ContainsKey(level)))
            throw new ArgumentException("Supply remaining counts for exactly the configured slot levels.");
        foreach (var (level, count) in slots) Validation.Range(count, 0, maximumSlots[level], nameof(slots));
        var saves = pending.ToArray();
        foreach (var dc in saves) ArgumentOutOfRangeException.ThrowIfLessThan(dc, 10);
        if ((concentration is not null && !concentration.Concentration) || (concentration is null && saves.Length > 0))
            throw new ArgumentException("Pending saves require a concentration spell.");
        foreach (var (level, count) in slots) remainingSlots[level] = count;
        concentratingOn = concentration;
        pendingSaves.Clear();
        foreach (var dc in saves) pendingSaves.Enqueue(dc);
    }
}
