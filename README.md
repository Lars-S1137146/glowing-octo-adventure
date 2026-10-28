# LongRest Console

A small D&D 5e character manager using **PHB 2014** rules, with local SQLite persistence. It supports shared mechanics and level-one character creation. It does not automate the entire handbook.

## Run

Install the .NET 10 SDK, then run these commands from this directory:

```powershell
dotnet restore LongRest.Console.slnx
dotnet build LongRest.Console.slnx --no-restore
dotnet run --project LongRest.Console --no-build --no-restore
```

Register a username and password. Registration logs you in. Password entry is hidden in an interactive terminal; stored passwords use salted PBKDF2 hashes. Accounts and characters are loaded at startup and saved automatically after each command, including logout and quit.

The application creates its database and directory on first startup. On Windows the default file is **%LOCALAPPDATA%\\LongRest\\longrest.db**, independent of the build output or working directory. The startup message displays its full path. Reopening the app lets you log in and select your existing characters.

To choose another database file:

```powershell
dotnet run --project LongRest.Console -- --database ./longrest.db
```

To back up your data, close the application and copy the database file. Saved state includes character identities, stats, training, inventory/equipment, conditions and their sources, HP/death saves, Hit Dice, spellbooks/prepared spells, remaining slots and pending concentration saves. A failed save rolls back its transaction; unreadable saved data is reported instead of resetting the database.

Type `help` at any time. Names are case insensitive; multiword names can be quoted.

## Try it

Run `character create`. Select Human, Wizard, and Sage by name or number. Assign the standard array in STR / DEX / CON / INT / WIS / CHA order, for example:

```text
8 10 13 15 14 12
```

Press Enter for the suggested class skills, and enter `Dagger, Quarterstaff` for equipment. Creation applies racial ability increases, starting HP, Hit Dice, speed, background/class/racial skill training, saving throws, weapon/armor training, and starting class spell slots. Equipment is owned when added; equip it separately.

Then try:

```text
sheet
equip Dagger
spell learn "Magic Missile"
spell prepare "Magic Missile"
spell learn "Fog Cloud"
spell prepare "Fog Cloud"
spell learn Light
spells
cast "Fog Cloud"
damage 2 fire
concentration save
rest short
rest long
stats set strength 14
logout
login
character select <your character's name>
```

Concentration saves prompt for d20 faces. Short rests prompt for one Hit Die at a time, letting you decide whether to spend another after seeing the healing. Rest commands ask you to confirm the game-time requirements; the application trusts that declaration rather than tracking a game clock.

Spell selection commands edit the character sheet. Clerics and druids prepare directly from their class lists. Wizards learn into a spellbook before preparing; known casters learn directly. Preparation limits, starting known-spell/cantrip limits, class lists, and available slot levels are checked. Preparing or replacing choices still requires the relevant PHB time and circumstances to be adjudicated by the player/DM.

## Commands

| Area | Commands |
|---|---|
| Accounts | `register [username]`, `login [username]`, `logout`, `quit` |
| Characters | `character create`, `character list`, `character select <name>`, `character rename <name>` |
| Statistics | `sheet`, `stats`, `stats set <ability> <score>` |
| Spells | `spells`, `spells catalog [level]`, `spell show <name>` |
| Spell choices | `spell learn <name>`, `spell forget <name>`, `spell prepare <name>`, `spell unprepare <name>` |
| Casting | `cast <name> [slot level]`, `concentration`, `concentration save`, `concentration end` |
| Equipment | `inventory`, `inventory catalog`, `inventory add <item>`, `inventory remove <item>`, `equip <item>`, `unequip <item>` |
| Health | `damage <amount> <type>`, `heal <amount>`, `temp <amount>`, `deathsave` |
| Checks | `check <ability> <DC> [skill]`, `save <ability> <DC>` |
| Conditions | `condition add <condition>`, `condition remove <condition>` |
| Recovery | `rest short`, `rest long` |

Equipment commands add/remove one owned item at a time; they do not simulate purchases. Stats are explicit sheet edits; changing Constitution updates maximum/current HP for existing levels without reviving a creature at zero HP.

## Code

There are three projects:

- `LongRest.Domain`: synchronous rules and ordinary C# data definitions.
- `LongRest.Console`: accounts, prompts, command parsing, display and SQLite persistence.
- `tests/LongRest.Domain.Tests`: copied and migrated TUnit tests, plus creation, interaction and console journeys.

The original web application was not copied or changed. The domain has no database, network, authentication, console calls, random dice generation, DI setup, or generic feature engine.

`LongRest.Console/Persistence` contains the SQLite store and plain character snapshot objects. Microsoft.Data.Sqlite is the only database package. Account rows contain password salts/hashes and a JSON snapshot of their character graphs. Restoration preserves catalog references and links conditions back to the correct saved characters without replaying game actions. The database has an explicit schema version; unsupported versions fail without resetting saved data. Character editing currently assumes one running console instance.

### Dice

D20 throws are domain objects:

```csharp
DiceThrow normal = new NormalDiceThrow(13);
DiceThrow advantage = new AdvantageDiceThrow(4, 17);
DiceThrow disadvantage = new DisadvantageDiceThrow(4, 17);

var hero = new Character("Ada", new Stats(strength: 16));
var result = hero.Check(Ability.Strength, normal, dc: 15);
```

Each throw validates and retains its faces and exposes its selected `Value`. There is no `Dice.ResolveD20` API. The domain determines the required throw type from circumstances and conditions through `CheckThrowType`, `SaveThrowType`, or `Combat.AttackThrowType`. Supplying the wrong kind is rejected. Automatic failures use a null throw and require no fictional die roll.

Damage and Hit Dice use `DiceRoll`, which records a die size and validated faces. Damage with different die sizes uses one roll per dice group. Attacks calculate hits separately from damage and HP changes.

Use `Encounter` in a `using` statement: it tracks direct character references, initiative, turns, actions, movement, reactions, Dodge and spell action restrictions, and observes character conditions during its lifetime. Encounter operations are available in the domain; the console currently focuses on character management rather than a combat UI.

## Rules and scope

The supplied Player's Handbook (2014) Markdown was used as the rules reference. The spell catalog includes metadata for 361 spells. Original chapter 11 class lists are used instead of the later expanded class lists also present in that file. Spell descriptions are not copied.

Implemented shared mechanics include checks/saves/contests, point buy, HP and death saves, typed damage, equipment and AC, weapon attacks and damage, rests, generic casting and concentration, encounter action economy, and the 14 conditions other than exhaustion. Creation provides all 12 starting classes, 14 race/subrace choices and 13 backgrounds. A level-one Warlock's Pact slot recovers on short rests.

These remain manual or outside this release:

- Level advancement, multiclassing, subclasses and class/racial feature execution (including Unarmored Defense, racial spellcasting, Lucky, Rage, Sneak Attack and domains).
- Languages, tools granted by character backgrounds/classes, starting equipment packages, currency and encumbrance.
- Individual spell effects, material costs, rituals, longer casting times, duration countdowns and spell-target geometry.
- Exhaustion, survival, exploration, maps, opportunity-attack triggers, grappling/shoving and special weapon actions such as nets and lances.
- Multiplayer.

The catalog's display metadata does not imply those effects are automated. See [the rules contract](LongRest_Domain_Requirements.md) for the boundary and conventions.

## Tests

```powershell
dotnet run --project tests/LongRest.Domain.Tests --no-build --no-restore -- --progress off

# A single existing milestone:
dotnet run --project tests/LongRest.Domain.Tests --no-build --no-restore -- --treenode-filter '/*/LongRest.Domain.Tests.Health/*/*'
```

Rebuild after edits. The tests use TUnit 1.76.0 and Microsoft's testing platform; the executable test project is run with `dotnet run`.

The migrated suite retains the original behavior checks. Two source contradictions were corrected: negative creature ability scores are invalid, and an attacker unseen by a dodging defender gains advantage. Tests now use explicit throws instead of integer lists.

