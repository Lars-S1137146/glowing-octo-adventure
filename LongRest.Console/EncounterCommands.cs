using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed partial class ConsoleApplication
{
    private Encounter RequireEncounter() => activeEncounter ?? throw new InvalidOperationException("No encounter in progress. Use encounter start.");

    private void EncounterCommand(string[] words)
    {
        if (words.Length == 1) { ShowEncounterStatus(); return; }
        switch (words[1].ToLowerInvariant())
        {
            case "start": StartEncounterCommand(words); break;
            case "status": ShowEncounterStatus(); break;
            case "action": EncounterActionCommand(words); break;
            case "attack": EncounterAttackCommand(words); break;
            case "cast": EncounterCastCommand(words); break;
            case "move": EncounterMoveCommand(words); break;
            case "stand": EncounterStandCommand(words); break;
            case "bonus": EncounterBonusCommand(words); break;
            case "reaction": EncounterReactionCommand(words); break;
            case "save": EncounterSaveCommand(words); break;
            case "endturn" or "next": EncounterEndTurnCommand(words); break;
            case "end" or "stop":
                if (activeEncounter is null) throw new InvalidOperationException("No active encounter.");
                activeEncounter.Dispose();
                activeEncounter = null;
                output.WriteLine("Encounter ended.");
                break;
            default:
                throw new ArgumentException("Unknown encounter command. Use encounter start, status, action, attack, cast, move, stand, bonus, reaction, save, endturn, or end.");
        }
    }

    private void StartEncounterCommand(string[] words)
    {
        if (activeEncounter is not null)
            throw new InvalidOperationException("An encounter is already in progress. Use encounter end to stop it first.");

        List<string> participantNames;
        if (words.Length > 2)
        {
            var raw = string.Join(" ", words.Skip(2));
            if (raw.Contains(','))
                participantNames = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            else
                participantNames = words.Skip(2).Select(w => w.TrimEnd(',')).Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        }
        else
        {
            var prompt = Prompt("Participant character names (comma separated)");
            participantNames = prompt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        if (participantNames.Count == 0)
            throw new ArgumentException("An encounter needs participants.");

        var entries = new List<InitiativeEntry>();
        foreach (var name in participantNames)
        {
            var character = CurrentUser!.FindCharacter(name);
            var rollText = Prompt($"Initiative d20 roll for {character.Name} (modifier {character.InitiativeModifier:+0;-0;0})");
            var die = Number(rollText);
            var surprisedText = Prompt($"Is {character.Name} surprised? [yes/no] (Enter = no)");
            var surprised = !string.IsNullOrWhiteSpace(surprisedText) && surprisedText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
            entries.Add(new InitiativeEntry(character, die, Surprised: surprised));
        }

        var groups = entries.GroupBy(e => e.Die + e.Character.InitiativeModifier).Where(g => g.Count() > 1).ToArray();
        List<Character>? tieOrder = null;
        if (groups.Length > 0)
        {
            tieOrder = [];
            foreach (var group in groups)
            {
                var tiedCharacters = group.Select(e => e.Character).ToList();
                output.WriteLine("Tied initiative for total " + group.Key + ": " + string.Join(", ", tiedCharacters.Select(c => c.Name)));
                var orderText = Prompt($"Specify tie-break order for {string.Join(", ", tiedCharacters.Select(c => c.Name))} (comma separated names)");
                var orderedNames = orderText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                foreach (var orderedName in orderedNames)
                {
                    var found = tiedCharacters.FirstOrDefault(c => c.Name.Equals(orderedName, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException($"'{orderedName}' is not among the tied characters.");
                    if (tieOrder.Contains(found)) throw new ArgumentException($"Character '{found.Name}' listed multiple times in tie order.");
                    tieOrder.Add(found);
                }
            }
        }

        activeEncounter = new Encounter(entries, tieOrder);
        activeEncounter.StartTurn();
        output.WriteLine($"Encounter started! Round {activeEncounter.RoundNumber}.");
        ShowEncounterStatus();
    }

    private void ShowEncounterStatus()
    {
        if (activeEncounter is null)
        {
            output.WriteLine("No active encounter. Use encounter start <character1> <character2> ... to begin.");
            return;
        }

        output.WriteLine($"--- Encounter: Round {activeEncounter.RoundNumber} ---");
        var order = activeEncounter.TurnOrder.Select(c => (c == activeEncounter.Current ? $"* {c.Name}" : $"  {c.Name}") + $" (HP {c.Health.Value}/{c.Health.Max})");
        output.WriteLine("Turn order:\n" + string.Join("\n", order));
        var current = activeEncounter.Current;
        output.WriteLine($"Current turn: {current.Name}");
        output.WriteLine($"Actions: {activeEncounter.ActionsRemaining} | Attacks remaining: {activeEncounter.AttacksRemaining} | Bonus action: {(activeEncounter.BonusActionAvailable ? "available" : "used")} | Movement: {activeEncounter.MovementRemaining} ft");
        var states = new List<string>();
        if (activeEncounter.IsDodging(current)) states.Add("Dodging");
        if (activeEncounter.IsDisengaged(current)) states.Add("Disengaged");
        if (activeEncounter.ReactionAvailable(current)) states.Add("Reaction available");
        if (states.Count > 0) output.WriteLine("States: " + string.Join(", ", states));
    }

    private void EncounterActionCommand(string[] words)
    {
        var enc = RequireEncounter();
        if (words.Length < 3) throw new ArgumentException("Use encounter action <attack|dash|dodge|disengage|help|hide|ready|search|useobject>.");
        var action = Choice<CombatAction>(words[2]);
        enc.TakeAction(enc.Current, action);
        output.WriteLine($"{enc.Current.Name} took action: {action}.");
        output.WriteLine($"Actions remaining: {enc.ActionsRemaining} | Attacks remaining: {enc.AttacksRemaining}");
    }

    private void EncounterAttackCommand(string[] words)
    {
        var enc = RequireEncounter();
        var targetAndWeaponWords = words[0].Equals("encounter", StringComparison.OrdinalIgnoreCase) ? words.Skip(2).ToArray() : words.Skip(1).ToArray();
        PerformAttack(enc.Current, targetAndWeaponWords, inEncounter: true);
    }

    private void AttackCommand(string[] words)
    {
        if (activeEncounter is not null)
        {
            EncounterAttackCommand(words);
            return;
        }
        PerformAttack(Hero(), words.Skip(1).ToArray(), inEncounter: false);
    }

    private void EncounterCastCommand(string[] words)
    {
        var enc = RequireEncounter();
        if (words.Length < 3) throw new ArgumentException("Use encounter cast <name> [slot level].");
        int? level = null;
        var nameWords = words.Skip(2).ToList();
        if (nameWords.Count > 1 && int.TryParse(nameWords[^1], out var selectedLevel))
        {
            level = selectedLevel;
            nameWords.RemoveAt(nameWords.Count - 1);
        }
        var spell = SpellCatalog.Find(string.Join(" ", nameWords));
        var triggered = spell.CastingTime == CastingTime.Reaction && Yes("Has this spell's reaction trigger occurred?");
        var result = enc.Cast(enc.Current, spell, level ?? spell.Level, triggered);
        output.WriteLine($"{enc.Current.Name} cast {result.Spell.Name} at level {result.CastingLevel}.");
        if (result.CastingLevel > 0 && enc.Current.Spellcasting is { } magic)
            output.WriteLine($"Level {result.CastingLevel} slots left: {magic.RemainingSlots(result.CastingLevel)}");
        output.WriteLine($"Actions: {enc.ActionsRemaining} | Bonus action: {(enc.BonusActionAvailable ? "available" : "used")}");
    }

    private void EncounterMoveCommand(string[] words)
    {
        var enc = RequireEncounter();
        Exact(words, 3, "encounter move <feet>");
        var feet = Number(words[2]);
        bool closerToFear = false;
        if (enc.Current.ActiveConditions.Contains(Condition.Frightened))
            closerToFear = Yes("Is this movement closer to your fear source?");
        enc.Move(enc.Current, feet, closerToFear);
        output.WriteLine($"{enc.Current.Name} moved {feet} ft. Movement remaining: {enc.MovementRemaining} ft.");
    }

    private void EncounterStandCommand(string[] words)
    {
        var enc = RequireEncounter();
        enc.StandUp(enc.Current);
        output.WriteLine($"{enc.Current.Name} stood up. Movement remaining: {enc.MovementRemaining} ft.");
    }

    private void EncounterBonusCommand(string[] words)
    {
        var enc = RequireEncounter();
        enc.UseBonusAction(enc.Current, true);
        output.WriteLine($"{enc.Current.Name} used bonus action.");
    }

    private void EncounterReactionCommand(string[] words)
    {
        var enc = RequireEncounter();
        Character character = words.Length > 2
            ? (CurrentUser?.FindCharacter(NameAfter(words, 2)) ?? enc.Participants.Select(p => p.Character).First(c => c.Name.Equals(NameAfter(words, 2), StringComparison.OrdinalIgnoreCase)))
            : enc.Current;
        enc.UseReaction(character, true);
        output.WriteLine($"{character.Name} used reaction.");
    }

    private void EncounterSaveCommand(string[] words)
    {
        var enc = RequireEncounter();
        if (words.Length != 4) throw new ArgumentException("Use encounter save <character> <ability> <DC>.");
        var character = CurrentUser?.FindCharacter(words[1])
            ?? enc.Participants.Select(p => p.Character).First(c => c.Name.Equals(words[1], StringComparison.OrdinalIgnoreCase));
        var ability = Choice<Ability>(words[2]);
        var dc = Number(words[3]);
        var circumstance = Prompt("External roll circumstances: Normal, Advantage, Disadvantage (Enter = Normal)");
        var mode = string.IsNullOrWhiteSpace(circumstance) ? ThrowType.Normal : Choice<ThrowType>(circumstance);
        var context = new RollContext(mode == ThrowType.Advantage ? 1 : 0, mode == ThrowType.Disadvantage ? 1 : 0);
        var saveContext = ability == Ability.Dexterity && enc.IsDodging(character) ? context with { AdvantageSources = context.AdvantageSources + 1 } : context;
        var required = character.SaveThrowType(ability, saveContext);
        var roll = ReadThrow(required);
        var result = enc.Save(character, ability, roll, dc, context);
        output.WriteLine($"{character.Name} save: {result.Total} vs DC {dc}: {(result.Success ? "success" : "failure")}");
    }

    private void EncounterEndTurnCommand(string[] words)
    {
        var enc = RequireEncounter();
        var prev = enc.Current;
        enc.EndTurn(prev);
        output.WriteLine($"Ended turn for {prev.Name}.");
        output.WriteLine($"Current turn: {enc.Current.Name} (Round {enc.RoundNumber})");
    }

    private void PerformAttack(Character attacker, string[] targetAndWeaponWords, bool inEncounter)
    {
        if (targetAndWeaponWords.Length == 0)
            throw new ArgumentException("Use attack <target> [weapon] or encounter attack <target> [weapon].");

        var targetName = targetAndWeaponWords[0];
        Character target;
        if (activeEncounter is not null)
        {
            target = activeEncounter.Participants.Select(p => p.Character)
                .FirstOrDefault(c => c.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                ?? CurrentUser!.FindCharacter(targetName);
        }
        else
        {
            target = CurrentUser!.FindCharacter(targetName);
        }

        Equipment? weapon = null;
        if (targetAndWeaponWords.Length > 1)
        {
            var weaponName = string.Join(" ", targetAndWeaponWords.Skip(1));
            if (!weaponName.Equals("unarmed", StringComparison.OrdinalIgnoreCase))
            {
                weapon = attacker.Inventory.HeldWeapons.FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase))
                    ?? attacker.Inventory.Items.Keys.FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase))
                    ?? EquipmentCatalog.Find(weaponName);
            }
        }
        else
        {
            weapon = attacker.Inventory.HeldWeapons.FirstOrDefault();
        }

        var distText = Prompt("Distance in feet (Enter = 5)");
        var distance = string.IsNullOrWhiteSpace(distText) ? 5 : Number(distText);

        var coverText = Prompt("Cover: None, Half, ThreeQuarters, Total (Enter = None)");
        var cover = string.IsNullOrWhiteSpace(coverText) ? Cover.None : Choice<Cover>(coverText);

        bool thrown = false;
        if (weapon is not null && weapon.Properties.HasFlag(WeaponProperties.Thrown))
        {
            var thrownText = Prompt("Throw weapon? [yes/no] (Enter = no)");
            thrown = !string.IsNullOrWhiteSpace(thrownText) && thrownText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        bool twoHands = false;
        if (weapon is not null && weapon.Properties.HasFlag(WeaponProperties.Versatile))
        {
            var twoHandsText = Prompt("Two-handed attack? [yes/no] (Enter = no)");
            twoHands = !string.IsNullOrWhiteSpace(twoHandsText) && twoHandsText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
        else if (weapon is not null && weapon.Properties.HasFlag(WeaponProperties.TwoHanded))
        {
            twoHands = true;
        }

        Ability? finesse = null;
        if (weapon is not null && weapon.Properties.HasFlag(WeaponProperties.Finesse))
        {
            var abilityText = Prompt("Finesse ability: Strength, Dexterity (Enter = Dexterity)");
            finesse = string.IsNullOrWhiteSpace(abilityText) ? Ability.Dexterity : Choice<Ability>(abilityText);
        }

        var seeTargetText = Prompt("Attacker can see target? [yes/no] (Enter = yes)");
        var attackerCanSee = string.IsNullOrWhiteSpace(seeTargetText) || seeTargetText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

        var seeAttackerText = Prompt("Target can see attacker? [yes/no] (Enter = yes)");
        var targetCanSee = string.IsNullOrWhiteSpace(seeAttackerText) || seeAttackerText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

        var hostileNearText = Prompt("Hostile within 5 ft who can see attacker? [yes/no] (Enter = no)");
        var hostileNear = !string.IsNullOrWhiteSpace(hostileNearText) && hostileNearText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

        bool? fearVisible = attacker.ActiveConditions.Contains(Condition.Frightened)
            ? Yes("Is your fear source visible?")
            : null;

        var context = new AttackContext(
            Distance: distance,
            AttackerCanSeeTarget: attackerCanSee,
            TargetCanSeeAttacker: targetCanSee,
            HostileWithinFiveFeetCanSeeAttacker: hostileNear,
            Cover: cover,
            Thrown: thrown,
            TwoHands: twoHands,
            FearSourceVisible: fearVisible
        );

        ThrowType requiredThrow;
        if (inEncounter && activeEncounter is not null)
        {
            if (activeEncounter.AttacksRemaining == 0 && activeEncounter.ActionsRemaining > 0)
            {
                activeEncounter.TakeAction(attacker, CombatAction.Attack);
            }
            var dodgeContext = activeEncounter.IsDodging(target) && targetCanSee
                ? context with { DisadvantageSources = context.DisadvantageSources + 1 }
                : context;
            requiredThrow = Combat.AttackThrowType(attacker, target, weapon, dodgeContext, finesse);
        }
        else
        {
            requiredThrow = Combat.AttackThrowType(attacker, target, weapon, context, finesse);
        }

        var roll = ReadThrow(requiredThrow);
        if (roll is null) throw new InvalidOperationException("Attack roll required.");

        AttackResult attackResult;
        if (inEncounter && activeEncounter is not null)
        {
            attackResult = activeEncounter.Attack(attacker, target, weapon, roll, context, finesse);
        }
        else
        {
            attackResult = Combat.Attack(attacker, target, weapon, roll, context, finesse);
        }

        var hitText = attackResult.Hit ? (attackResult.Critical ? "CRITICAL HIT!" : "HIT!") : "MISS!";
        output.WriteLine($"Attack: {attackResult.Total} (d20: {attackResult.Die}) vs AC {target.ArmorClass + Combat.CoverBonus(cover)} -> {hitText}");

        if (attackResult.Hit)
        {
            var groups = new List<DamageDice>();
            if (weapon is not null && weapon.FixedDamage is null)
            {
                var sides = twoHands && weapon.Properties.HasFlag(WeaponProperties.Versatile) ? weapon.VersatileSides : weapon.DamageSides;
                groups.Add(new DamageDice(weapon.DamageDice, sides));
            }

            var damageRolls = ReadDamageRolls(groups, attackResult.Critical);
            var damage = Combat.RollDamage(attacker, weapon, attackResult, damageRolls, twoHands, finesse);
            var healthResult = target.TakeDamage(damage);
            output.WriteLine($"Dealt {damage.Amount} {damage.Type} damage to {target.Name}.");
            output.WriteLine($"Target HP: {target.Health.Value}/{target.Health.Max} (Temp: {target.Health.TempHealth})");
            if (healthResult.ConcentrationSaveDc is not null)
                output.WriteLine($"Concentration save pending: DC {healthResult.ConcentrationSaveDc}. Use concentration save.");
            if (target.Health.IsDead) output.WriteLine($"{target.Name} is dead.");
            else if (target.Health.IsDowned) output.WriteLine($"{target.Name} is unconscious at zero HP.");
        }

        if (inEncounter && activeEncounter is not null)
        {
            output.WriteLine($"Attacks remaining this turn: {activeEncounter.AttacksRemaining}");
        }
    }

    private IReadOnlyList<DiceRoll> ReadDamageRolls(IReadOnlyList<DamageDice> groups, bool critical)
    {
        var rolls = new List<DiceRoll>();
        var multiplier = critical ? 2 : 1;
        foreach (var group in groups)
        {
            var count = group.Count * multiplier;
            var text = Prompt($"Damage: enter {count} d{group.Sides} face(s), space separated");
            var faces = text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(Number).ToArray();
            if (faces.Length != count) throw new ArgumentException($"Supply {count} face(s).");
            rolls.Add(new DiceRoll(group.Sides, faces));
        }
        return rolls;
    }
}
