namespace LongRest.Domain;

public sealed partial class Character
{
    private Spellcasting MagicFor(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        var magic = Spellcasting ?? throw new InvalidOperationException("This character has no spellcasting.");
        if (!spell.Classes.Contains(Class.Name)) throw new InvalidOperationException("This spell is not on your class spell list.");
        if (spell.Level > 0 && !magic.MaximumSlots.Any(slot => slot.Key >= spell.Level && slot.Value > 0))
            throw new InvalidOperationException("You cannot learn or prepare spells above your available slot level.");
        return magic;
    }

    public void LearnSpell(Spell spell)
    {
        var magic = MagicFor(spell);
        if (spell.Level == 0)
        {
            if (!magic.Cantrips.Contains(spell) && magic.Cantrips.Count >= Class.StartingCantrips)
                throw new InvalidOperationException("Your starting cantrip choices are already filled.");
        }
        else
        {
            if (Class.Name is "Cleric" or "Druid") throw new InvalidOperationException("Your class prepares spells directly from its class list.");
            if (magic.Availability == SpellAvailability.Known && !magic.KnownSpells.Contains(spell) &&
                magic.KnownSpells.Count >= Class.StartingSpells)
                throw new InvalidOperationException("Your starting known spell choices are already filled.");
        }
        magic.LearnSpell(spell);
    }

    public void PrepareSpell(Spell spell)
    {
        var magic = MagicFor(spell);
        if (magic.Availability != SpellAvailability.Prepared || spell.Level == 0)
            throw new InvalidOperationException("This spell cannot be prepared.");
        if (Class.Name == "Wizard" && !magic.KnownSpells.Contains(spell))
            throw new InvalidOperationException("Learn the spell in your spellbook first.");
        var limit = Math.Max(1, Level + Stats.GetModifier(magic.Ability));
        if (!magic.AvailableSpells.Contains(spell) && magic.AvailableSpells.Count >= limit)
            throw new InvalidOperationException($"You can prepare at most {limit} spells.");
        magic.PrepareSpell(spell);
    }
}
