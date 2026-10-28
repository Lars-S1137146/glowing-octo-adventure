namespace LongRest.Domain;

public sealed record AttackContext(
    int Distance,
    bool AttackerCanSeeTarget,
    bool TargetCanSeeAttacker,
    bool HostileWithinFiveFeetCanSeeAttacker,
    Cover Cover = Cover.None,
    bool Thrown = false,
    bool TwoHands = false,
    bool? FearSourceVisible = null,
    int AdvantageSources = 0,
    int DisadvantageSources = 0,
    int Modifier = 0);

public sealed record AttackResult(bool Hit, bool Critical, int Total, int Die, bool? Melee = null);
public sealed record DamageDice(int Count, int Sides);

public static class Combat
{
    public static int CoverBonus(Cover cover) => cover switch
    {
        Cover.Half => 2,
        Cover.ThreeQuarters => 5,
        _ => 0
    };

    private static void ValidateWeapon(Equipment? weapon)
    {
        if (weapon is null) return;
        if (weapon.Kind != EquipmentKind.Weapon) throw new ArgumentException("Supply a weapon.");
        Validation.Defined(weapon.WeaponCategory);
        Validation.Defined(weapon.WeaponKind);
        Validation.Defined(weapon.DamageType);
        const WeaponProperties allowed = WeaponProperties.Finesse | WeaponProperties.Thrown | WeaponProperties.Light |
            WeaponProperties.Versatile | WeaponProperties.TwoHanded | WeaponProperties.Heavy;
        if ((weapon.Properties & ~allowed) != 0) throw new ArgumentOutOfRangeException(nameof(weapon.Properties));
        if (weapon.FixedDamage is not null) ArgumentOutOfRangeException.ThrowIfNegative(weapon.FixedDamage.Value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weapon.DamageDice);
        Validation.Range(weapon.DamageSides, 2, 100, nameof(weapon.DamageSides));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weapon.Reach);
        if (weapon.Properties.HasFlag(WeaponProperties.Versatile))
            Validation.Range(weapon.VersatileSides, 2, 100, nameof(weapon.VersatileSides));
        if (weapon.WeaponKind == WeaponKind.Ranged || weapon.Properties.HasFlag(WeaponProperties.Thrown))
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weapon.ShortRange);
            if (weapon.LongRange < weapon.ShortRange) throw new ArgumentOutOfRangeException(nameof(weapon.LongRange));
        }
    }

    private static Ability AttackAbility(Equipment? weapon, Ability? selected)
    {
        if (selected is not null)
        {
            if (selected is not (Ability.Strength or Ability.Dexterity) ||
                weapon?.Properties.HasFlag(WeaponProperties.Finesse) != true)
                throw new ArgumentException("Only finesse weapons allow a choice of Strength or Dexterity.");
            return selected.Value;
        }
        return weapon?.WeaponKind == WeaponKind.Ranged ? Ability.Dexterity : Ability.Strength;
    }

    public static ThrowType AttackThrowType(Character attacker, Character target, Equipment? weapon,
        AttackContext context, Ability? finesseAbility = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(context);
        Validation.Defined(context.Cover);
        ArgumentOutOfRangeException.ThrowIfNegative(context.Distance);
        ArgumentOutOfRangeException.ThrowIfNegative(context.AdvantageSources);
        ArgumentOutOfRangeException.ThrowIfNegative(context.DisadvantageSources);
        ValidateWeapon(weapon);
        _ = AttackAbility(weapon, finesseAbility);
        if (!attacker.CanAct) throw new InvalidOperationException("The attacker cannot act.");
        if (attacker.ConditionSource(Condition.Charmed) == target) throw new InvalidOperationException("You cannot attack your charm source.");
        if (context.Cover == Cover.Total) throw new InvalidOperationException("Total cover prevents a direct attack.");
        if (weapon is not null && !attacker.Inventory.HeldWeapons.Contains(weapon))
            throw new InvalidOperationException("The weapon must be owned and held.");
        if (context.Thrown && weapon?.Properties.HasFlag(WeaponProperties.Thrown) != true)
            throw new InvalidOperationException("This weapon is not throwable.");
        var twoHanded = context.TwoHands || weapon?.Properties.HasFlag(WeaponProperties.TwoHanded) == true;
        if (twoHanded && (attacker.Inventory.Shield is not null || attacker.Inventory.HeldWeapons.Count > 1))
            throw new InvalidOperationException("A two-handed attack requires both hands.");
        if (context.TwoHands && weapon is null) throw new ArgumentException("An unarmed strike does not use a two-handed weapon.");
        var ranged = context.Thrown || weapon?.WeaponKind == WeaponKind.Ranged;
        var reach = ranged ? weapon!.LongRange : weapon?.Reach ?? 5;
        if (context.Distance > reach) throw new InvalidOperationException("The target is out of range.");

        var advantage = context.AdvantageSources;
        var disadvantage = context.DisadvantageSources;
        if (ranged && (context.Distance > weapon!.ShortRange || context.HostileWithinFiveFeetCanSeeAttacker)) disadvantage++;
        if (!context.AttackerCanSeeTarget || target.Has(Condition.Invisible)) disadvantage++;
        // The supplied visibility fact also handles exceptions such as blindsight.
        if (!context.TargetCanSeeAttacker || attacker.Has(Condition.Invisible)) advantage++;
        if (attacker.HasAny(Condition.Blinded, Condition.Poisoned, Condition.Prone, Condition.Restrained) || attacker.HasUntrainedArmor)
            disadvantage++;
        if (attacker.Race.Small && weapon?.Properties.HasFlag(WeaponProperties.Heavy) == true) disadvantage++;
        if (attacker.Has(Condition.Frightened))
        {
            if (context.FearSourceVisible is null) throw new ArgumentException("Supply fear-source visibility.");
            if (context.FearSourceVisible == true) disadvantage++;
        }
        if (target.HasAny(Condition.Blinded, Condition.Paralyzed, Condition.Petrified, Condition.Restrained,
                Condition.Stunned, Condition.Unconscious)) advantage++;
        if (target.Has(Condition.Prone))
        {
            if (context.Distance <= 5) advantage++;
            else disadvantage++;
        }
        return DiceThrow.RequiredType(advantage, disadvantage);
    }

    public static AttackResult Attack(Character attacker, Character target, Equipment? weapon,
        DiceThrow roll, AttackContext context, Ability? finesseAbility = null)
    {
        var required = AttackThrowType(attacker, target, weapon, context, finesseAbility);
        ArgumentNullException.ThrowIfNull(roll);
        roll.Require(required);
        var ability = AttackAbility(weapon, finesseAbility);
        var training = weapon is null || attacker.Proficiencies.ForWeapon(weapon) ? attacker.ProficiencyBonus : 0;
        var total = roll.Value + attacker.Stats.GetModifier(ability) + training + context.Modifier;
        var hit = roll.Value == 20 || (roll.Value != 1 && total >= target.ArmorClass + CoverBonus(context.Cover));
        var critical = hit && (roll.Value == 20 || (context.Distance <= 5 && target.HasAny(Condition.Paralyzed, Condition.Unconscious)));
        return new(hit, critical, total, roll.Value, !context.Thrown && weapon?.WeaponKind != WeaponKind.Ranged);
    }

    public static Damage RollDamage(Character attacker, Equipment? weapon, AttackResult attack,
        IReadOnlyList<DiceRoll> rolls, bool twoHands = false, Ability? finesseAbility = null,
        int modifier = 0, IReadOnlyList<DamageDice>? extraDice = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(rolls);
        if (!attack.Hit) throw new InvalidOperationException("A miss cannot cause hit damage.");
        ValidateWeapon(weapon);
        var ability = AttackAbility(weapon, finesseAbility);
        var groups = new List<DamageDice>();
        if (weapon is not null && weapon.FixedDamage is null)
        {
            var sides = twoHands && weapon.Properties.HasFlag(WeaponProperties.Versatile) ? weapon.VersatileSides : weapon.DamageSides;
            groups.Add(new(weapon.DamageDice, sides));
        }
        if (extraDice is not null) groups.AddRange(extraDice);
        foreach (var group in groups)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(group.Count);
            Validation.Range(group.Sides, 2, 100, nameof(extraDice));
        }
        var multiplier = attack.Critical ? 2 : 1;
        if (rolls.Count != groups.Count) throw new ArgumentException("Supply one roll for each damage dice group.");
        for (var i = 0; i < groups.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(rolls[i]);
            if (rolls[i].Sides != groups[i].Sides || rolls[i].Faces.Count != groups[i].Count * multiplier)
                throw new ArgumentException("Incorrect damage dice size or count.");
        }
        var amount = Math.Max(0, checked(rolls.Sum(roll => roll.Total) + (weapon?.FixedDamage ?? (weapon is null ? 1 : 0)) + attacker.Stats.GetModifier(ability) + modifier));
        return new(amount, weapon?.DamageType ?? DamageType.Bludgeoning, Critical: attack.Critical,
            Melee: attack.Melee ?? weapon?.WeaponKind != WeaponKind.Ranged);
    }
}
