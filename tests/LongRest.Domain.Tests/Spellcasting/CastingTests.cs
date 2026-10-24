// PHB 10: generic casting, slots and concentration. Project convention: no class progression or individual spell effects.
namespace LongRest.Domain.Tests.Spellcasting;

public class CastingTests
{
    [Test]
    public async Task CantripUsesNoSlot()
    {
        // Arrange
        var spell = new Spell("Light", 0);
        var magic = Magic(cantrips: [spell], slots: []);
        var caster = Hero(spells: magic);

        // Act
        var result = magic.Cast(caster, spell, 0);

        // Assert
        await Assert.That(result.CastingLevel).IsEqualTo(0);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(0);
    }

    [Test]
    public async Task UpcastingConsumesOnlySelectedSlot()
    {
        // Arrange
        var spell = new Spell("Magic missile", 1);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);

        // Act
        var result = magic.Cast(caster, spell, 3);

        // Assert
        await Assert.That(result.CastingLevel).IsEqualTo(3);
        await Assert.That(magic.RemainingSlots(3)).IsEqualTo(0);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    [Arguments(SpellAvailability.Known)]
    [Arguments(SpellAvailability.Prepared)]
    public async Task KnownAndPreparedModesHaveIndependentAvailability(SpellAvailability mode)
    {
        // Arrange
        var available = new Spell("Bless", 1);
        var magic = Magic([available], mode: mode);
        var caster = Hero(spells: magic);

        // Act
        var result = magic.Cast(caster, available, 1);

        // Assert
        await Assert.That(result.Spell).IsEqualTo(available);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
    }

    [Test]
    [Arguments(SpellAvailability.Known)]
    [Arguments(SpellAvailability.Prepared)]
    public async Task UnavailableSpellDoesNotConsumeSlot(SpellAvailability mode)
    {
        // Arrange
        var magic = Magic(mode: mode);
        var caster = Hero(spells: magic);

        // Act
        Action act = () => magic.Cast(caster, new Spell("Unknown spell", 1), 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task CannotCastUsingLowerSlot()
    {
        // Arrange
        var spell = new Spell("Hold person", 2);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);

        // Act
        Action act = () => magic.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task CantripCannotBeUpcast()
    {
        // Arrange
        var spell = new Spell("Light", 0);
        var magic = Magic(cantrips: [spell]);
        var caster = Hero(spells: magic);

        // Act
        Action act = () => magic.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task NoSlotCastLeavesSlotsAndConcentrationUnchanged()
    {
        // Arrange
        var first = new Spell("Bless", 1, concentration: true);
        var second = new Spell("Hold person", 2, concentration: true);
        var magic = Magic([first, second], slots: new() { [1] = 1, [2] = 0 });
        var caster = Hero(spells: magic);
        magic.Cast(caster, first, 1);

        // Act
        Action act = () => magic.Cast(caster, second, 2);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.ConcentratingOn).IsEqualTo(first);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(0);
        await Assert.That(magic.RemainingSlots(2)).IsEqualTo(0);
    }

    [Test]
    public async Task ConfiguredAbilityDeterminesAttackAndSaveDc()
    {
        // Arrange
        var magic = Magic();
        var caster = Hero(new Stats(intelligence: 16), level: 5, spells: magic);

        // Act
        var attack = magic.AttackModifier(caster);
        var dc = magic.SaveDc(caster);

        // Assert
        await Assert.That(attack).IsEqualTo(6);
        await Assert.That(dc).IsEqualTo(14);
    }

    [Test]
    public async Task SpellcastingAbilityIsNotHardcodedToIntelligence()
    {
        // Arrange
        var magic = new LongRest.Domain.Spellcasting(Ability.Wisdom, SpellAvailability.Prepared, [], [], new Dictionary<int, int>(), saveDcModifier: 1);
        var caster = Hero(new Stats(wisdom: 18), spells: magic);

        // Act
        var attack = magic.AttackModifier(caster);
        var dc = magic.SaveDc(caster);

        // Assert
        await Assert.That(attack).IsEqualTo(6);
        await Assert.That(dc).IsEqualTo(15);
    }

    [Test]
    public async Task UnproficientArmorRejectsCastWithoutSpendingSlot()
    {
        // Arrange
        var spell = new Spell("Bless", 1);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        Own(caster, Armor());

        // Act
        Action act = () => magic.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task LongRestRestoresGenericSlots()
    {
        // Arrange
        var spell = new Spell("Magic missile", 1);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        caster.LongRest();

        // Assert
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task LongerCastingTimeIsExplicitlyUnsupported()
    {
        // Arrange
        var spell = new Spell("Long ritual", 1, CastingTime.Longer);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);

        // Act
        Action act = () => magic.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    public async Task ReactionCastRequiresReportedTrigger()
    {
        // Arrange
        var spell = new Spell("Shield", 1, CastingTime.Reaction);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);

        // Act
        Action act = () => magic.Cast(caster, spell, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(10)]
    public async Task InvalidSpellLevelIsRejected(int level)
    {
        // Arrange
        

        // Act
        Action act = () => new Spell("Invalid", level);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

}



