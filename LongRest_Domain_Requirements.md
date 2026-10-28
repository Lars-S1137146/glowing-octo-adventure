# LongRest console rules contract

This replaces the source project's unfinished learning exercise. The console implements its shared PHB 2014 mechanics, changes the dice API, and adds level-one character creation with local SQLite persistence. It is not a claim that every PHB rule or feature is automated.

## Sources and edition

Use the supplied **Player's Handbook (2014)**, especially:

| Area | PHB reading |
|---|---|
| Creation and ability generation | Chapter 1, Step-by-Step Characters |
| Races, classes and backgrounds | Chapters 2–4, starting statistics and choices |
| Equipment | Chapter 5, Armor and Shields, Weapons |
| Checks and saves | Chapter 7, Using Ability Scores |
| Recovery | Chapter 8, Resting |
| Health, combat and turns | Chapter 9, Combat |
| Spell resources and concentration | Chapter 10, Spellcasting |
| Spell catalog | Chapter 11, original class spell lists and spell metadata |
| Conditions | Appendix A, excluding Exhaustion |

Ignore later optional class features and expanded spell lists included in the supplied Markdown. Spell effects and full feature execution are outside this release.

## Design

- Ordinary, readable synchronous C# classes and collections.
- One Character model for adventurers, monsters and NPCs.
- Race, Class, Background, Equipment and Spell are definitions, not subclasses for each named option.
- DiceThrow has NormalDiceThrow, AdvantageDiceThrow and DisadvantageDiceThrow subclasses. Each validates its own d20 faces and exposes Value.
- The character/combat rules determine the required throw type before the caller supplies it. Both advantage and disadvantage cancel to Normal regardless of the number of sources.
- DiceRoll represents supplied damage/healing dice of a particular size. Mixed damage has multiple DiceRoll groups.
- Invalid input and forbidden actions fail before spending resources.
- Rule-governed state is changed through methods. Collections returned to callers cannot directly mutate owned resources.
- No domain console calls, authentication, storage, randomness or general-purpose effects interpreter.

## Required behavior

1. Six scores from 1–30; modifiers round down. Point buy uses six base scores from 8–15 and a budget of 27.
2. Player proficiency follows levels 1–20. Stat blocks may supply an explicit positive proficiency bonus without a player level.
3. Ability checks and saves add appropriate modifiers and proficiency once; expertise doubles proficiency. Natural 1/20 do not automatically determine ordinary check/save success.
4. Checks support explicitly selected ability, skill and tool proficiency. Contests compare totals and preserve ties.
5. Health validates normal/maximum/temporary pools, applies damage reductions and resistance before vulnerability, supports healing, death saves, stability, massive damage and nonlethal melee knockouts.
6. At zero HP, positive damage causes death-save failures even when temporary HP absorb it. Temporary HP do not restore consciousness or stability. They also do not prevent concentration saves.
7. HP-based unconsciousness drops held items and makes the creature Prone. Healing ends HP-based unconsciousness, leaving Prone in place.
8. Inventory tracks definition identity and quantities, equipment state and hand availability. The last equipped item cannot be removed until unequipped. A two-handed weapon can be held with a shield but cannot be used for a two-handed attack then.
9. AC follows armor category and shield rules. Strength requirements affect heavy-armor speed. Untrained armor affects physical checks/saves, attacks and casting. Stealth penalties remain effective with proficiency.
10. Weapon attacks honor training, Strength/Dexterity/finesse, range, cover, visibility, conditions and natural 1/20. Hits and damage are separate. Critical hits double dice, including supplied extra dice, but never flat modifiers.
11. Short rests spend supplied Hit Dice with Constitution added to each die, minimum zero healing. Long rests restore HP, generic slots and up to half the total Hit Dice, rounded down with minimum one, and clear temporary HP.
12. Spellcasting tracks known/prepared availability, cantrips, slots, upcasting and concentration. Damage queues separate Constitution saves in event order. Failed saves, incapacitation and death end concentration.
13. Encounters validate participants and caller-selected initiative ties, maintain rounds/turns, action and attack allowances, movement, bonus actions and reactions. They support Attack, Dash, Dodge and Disengage. Other named actions spend an allowance, with effects adjudicated manually.
14. Reactions refresh at the start of the character's own turn. Surprise blocks first-turn actions/movement and reactions until that first turn ends.
15. Bonus-action spell restrictions apply in either casting order and last only for the current turn, including own-turn reaction spells.
16. Apply deterministic effects of the 14 non-exhaustion PHB conditions. Require source/visibility/distance context where needed. Dodge is lost when incapacitated or speed falls to zero; removing that impairment does not restore the old Dodge.
17. Character creation applies base-score validation, racial increases, optional racial choices, starting class/background proficiencies, maximum HP, Hit Dice and slots. Overlapping racial/background skills require replacements.
18. The console supports accounts, isolated character ownership, creation/selection, sheet display, stat edits, spell choices/casting, inventory, checks/saves, conditions, damage/healing and rests.

## Explicit conventions and limits

- A local SQLite database and its directory are created if missing. Accounts and characters load on startup and save automatically after each command. Passwords remain salted hashes. The domain has no SQLite or JSON dependency.
- Saves use transactions. Invalid saved data or an unsupported schema version is reported without replacing existing data. Character editing assumes one running console instance.
- Creation is level one. Existing domain fixtures may configure established higher-level characters and monsters, but no leveling workflow or automatic higher-level class progression exists.
- Spell choices are sheet configuration. The player/DM adjudicates preparation time, replacing known choices, spellbook copying costs and circumstances.
- Starting class spell limits and prepared-spell limits are enforced. Full spell/class progression, domains and subclass lists are not.
- A level-one Warlock has a short-rest Pact slot. This does not implement higher-level Pact Magic or Mystic Arcanum.
- Rest durations, interruptions and the once-per-24-hours restriction are declared by the caller. Console commands require an explicit confirmation of eligibility, rather than using wall-clock time for fictional time.
- Generic characters can be configured without a class, race or background. Do not infer monster HP or features from a player class.
- Computed damage has a minimum of zero. All creatures share the same death-save behavior.
- Special weapon effects, ammunition/loading tracking, opportunity triggers, battlefield geometry and individual spell effects remain manual.
- Petrification's weight, aging and disease details are descriptive, not simulated.
- Conditions are deduplicated by type; independent timed sources and automatic expiry are not modeled.
- The console exposes character management; encounter orchestration remains a domain API.

## Acceptance

- The entire solution builds on .NET 10.
- Copied domain tests retain their behavioral coverage with explicit throw objects.
- Rules, cross-mechanic interactions, invalid operations and complete console journeys pass TUnit tests.
- No NotImplementedException remains in shipped domain or console code.
- The original web application remains unchanged.
- README documents working commands and clearly distinguishes automated rules from manual adjudication.

