// PHB 9: combat order, actions and movement. Project convention: caller-selected initiative ties; no map.
namespace LongRest.Domain.Tests.Encounters;

public class EncounterSpellTests
{
    [Test]
    public async Task BonusActionSpellAllowsActionCantrip()
    {
        // Arrange
        var bonus = new Spell("Healing word", 1, CastingTime.BonusAction);
        var cantrip = new Spell("Light", 0);
        var magic = Magic([bonus], [cantrip]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);

        // Act
        encounter.Cast(caster, bonus, 1);
        encounter.Cast(caster, cantrip, 0);

        // Assert
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
        await Assert.That(encounter.BonusActionAvailable).IsFalse();
    }

    [Test]
    public async Task BonusActionSpellForbidsActionLeveledSpell()
    {
        // Arrange
        var bonus = new Spell("Healing word", 1, CastingTime.BonusAction);
        var action = new Spell("Magic missile", 1);
        var magic = Magic([bonus, action]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);
        encounter.Cast(caster, bonus, 1);

        // Act
        Action act = () => encounter.Cast(caster, action, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
    }

    [Test]
    public async Task LeveledActionSpellAlsoForbidsLaterBonusSpell()
    {
        // Arrange
        var bonus = new Spell("Healing word", 1, CastingTime.BonusAction);
        var action = new Spell("Magic missile", 1);
        var magic = Magic([bonus, action]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);
        encounter.Cast(caster, action, 1);

        // Act
        Action act = () => encounter.Cast(caster, bonus, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
        await Assert.That(encounter.BonusActionAvailable).IsTrue();
    }

    [Test]
    public async Task SecondBonusActionSpellDoesNotConsumeSlot()
    {
        // Arrange
        var spell = new Spell("Healing word", 1, CastingTime.BonusAction);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);
        encounter.Cast(caster, spell, 1);

        // Act
        Action act = () => encounter.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
    }

    [Test]
    public async Task FailedCastDoesNotSpendAction()
    {
        // Arrange
        var spell = new Spell("Magic missile", 1);
        var magic = Magic([spell], slots: []);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);

        // Act
        Action act = () => encounter.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
    }

    [Test]
    public async Task OutOfTurnCastLeavesAllResourcesUnchanged()
    {
        // Arrange
        var spell = new Spell("Magic missile", 1);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        var other = Hero();
        var encounter = Fight(other, caster);

        // Act
        Action act = () => encounter.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
        await Assert.That(encounter.Current).IsEqualTo(other);
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
    }

    [Test]
    public async Task ReactionSpellConsumesReactionOutsideOwnTurn()
    {
        // Arrange
        var spell = new Spell("Shield", 1, CastingTime.Reaction);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        var encounter = Fight(Hero(), caster);

        // Act
        encounter.Cast(caster, spell, 1, eligibleReactionTrigger: true);

        // Assert
        await Assert.That(encounter.ReactionAvailable(caster)).IsFalse();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
    }

    [Test]
    public async Task OwnTurnReactionSpellIsForbiddenAfterBonusSpell()
    {
        // Arrange
        var bonus = new Spell("Healing word", 1, CastingTime.BonusAction);
        var reaction = new Spell("Shield", 1, CastingTime.Reaction);
        var magic = Magic([bonus, reaction]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster);
        encounter.Cast(caster, bonus, 1);

        // Act
        Action act = () => encounter.Cast(caster, reaction, 1, true);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ReactionAvailable(caster)).IsTrue();
    }

    [Test]
    public async Task BonusSpellRestrictionExpiresWhenTurnEnds()
    {
        // Arrange
        var bonus = new Spell("Healing word", 1, CastingTime.BonusAction);
        var reaction = new Spell("Shield", 1, CastingTime.Reaction);
        var magic = Magic([bonus, reaction]);
        var caster = Hero(spells: magic);
        var encounter = Fight(caster, Hero());
        encounter.Cast(caster, bonus, 1);
        encounter.EndTurn(caster);

        // Act
        encounter.Cast(caster, reaction, 1, true);

        // Assert
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(0);
    }

}



