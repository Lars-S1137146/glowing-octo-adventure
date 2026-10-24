// PHB 10: generic casting, slots and concentration. Project convention: no class progression or individual spell effects.
namespace LongRest.Domain.Tests.Spellcasting;

public class ConcentrationTests
{
    [Test]
    public async Task NewConcentrationReplacesOldSpell()
    {
        // Arrange
        var first = new Spell("Bless", 1, concentration: true);
        var second = new Spell("Fog cloud", 1, concentration: true);
        var magic = Magic([first, second]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, first, 1);

        // Act
        magic.Cast(caster, second, 1);

        // Assert
        await Assert.That(magic.ConcentratingOn).IsEqualTo(second);
    }

    [Test]
    public async Task NonConcentrationSpellPreservesConcentration()
    {
        // Arrange
        var first = new Spell("Bless", 1, concentration: true);
        var second = new Spell("Magic missile", 1);
        var magic = Magic([first, second]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, first, 1);

        // Act
        magic.Cast(caster, second, 1);

        // Assert
        await Assert.That(magic.ConcentratingOn).IsEqualTo(first);
    }

    [Test]
    public async Task ConcentrationMayEndExplicitlyWithoutAction()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        magic.EndConcentration();

        // Assert
        await Assert.That(magic.ConcentratingOn).IsNull();
    }

    [Test]
    [Arguments(8, 10)]
    [Arguments(26, 13)]
    [Arguments(27, 13)]
    public async Task DamageRequiresPendingConcentrationSave(int amount, int expectedDc)
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(hp: 60, spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        var damage = caster.TakeDamage(new(amount, DamageType.Fire));

        // Assert
        await Assert.That(damage.ConcentrationSaveDc).IsEqualTo((int?)expectedDc);
        await Assert.That(magic.PendingConcentrationSaves.SequenceEqual(new[] { expectedDc })).IsTrue();
    }

    [Test]
    public async Task ResistanceAffectsConcentrationDc()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(hp: 60, spells: magic, resistances: [DamageType.Fire]);
        magic.Cast(caster, spell, 1);

        // Act
        var result = caster.TakeDamage(new(26, DamageType.Fire));

        // Assert
        await Assert.That(result.ConcentrationSaveDc).IsEqualTo((int?)10);
    }

    [Test]
    public async Task EachDamageEventNeedsItsOwnSave()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(hp: 60, spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        caster.TakeDamage(new(26, DamageType.Fire));
        caster.TakeDamage(new(2, DamageType.Cold));

        // Assert
        await Assert.That(magic.PendingConcentrationSaves.SequenceEqual(new[] { 13, 10 })).IsTrue();
    }

    [Test]
    public async Task SuccessfulConstitutionSavePreservesConcentration()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(new Stats(constitution: 14), hp: 60, spells: magic);
        magic.Cast(caster, spell, 1);
        caster.TakeDamage(new(26, DamageType.Fire));

        // Act
        var result = magic.ResolveConcentrationSave(caster, new NormalDiceThrow(11));

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(magic.ConcentratingOn).IsEqualTo(spell);
        await Assert.That(magic.PendingConcentrationSaves.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FailedSaveEndsConcentration()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(hp: 60, spells: magic);
        magic.Cast(caster, spell, 1);
        caster.TakeDamage(new(2, DamageType.Fire));

        // Act
        magic.ResolveConcentrationSave(caster, new NormalDiceThrow(9));

        // Assert
        await Assert.That(magic.ConcentratingOn).IsNull();
    }

    [Test]
    public async Task InvalidSaveLeavesPendingRequirementUnchanged()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(hp: 60, spells: magic);
        magic.Cast(caster, spell, 1);
        caster.TakeDamage(new(26, DamageType.Fire));

        // Act
        Action act = () => magic.ResolveConcentrationSave(caster, new NormalDiceThrow(21));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(magic.ConcentratingOn).IsEqualTo(spell);
        await Assert.That(magic.PendingConcentrationSaves.SequenceEqual(new[] { 13 })).IsTrue();
    }

    [Test]
    public async Task IncapacitationEndsConcentration()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        caster.AddCondition(Condition.Incapacitated);

        // Assert
        await Assert.That(magic.ConcentratingOn).IsNull();
    }

    [Test]
    public async Task DeathEndsConcentration()
    {
        // Arrange
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var caster = Hero(spells: magic);
        magic.Cast(caster, spell, 1);

        // Act
        caster.TakeDamage(new(40, DamageType.Fire));

        // Assert
        await Assert.That(magic.ConcentratingOn).IsNull();
    }

}



