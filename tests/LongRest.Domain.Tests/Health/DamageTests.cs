// PHB 9: damage, healing, temporary HP and dropping to zero. All creatures use the same death rules by project convention.
namespace LongRest.Domain.Tests.Health;

public class DamageTests
{
    [Test]
    public async Task TemporaryHitPointsAbsorbDamageFirst()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(20, 20, 5);

        // Act
        var result = hero.TakeDamage(new(7, DamageType.Slashing));

        // Assert
        await Assert.That(hero.Health.TempHealth).IsEqualTo(0);
        await Assert.That(hero.Health.Value).IsEqualTo(18);
        await Assert.That(result.TemporaryAbsorbed).IsEqualTo(5);
        await Assert.That(result.DamageTaken).IsEqualTo(7);
    }

    [Test]
    public async Task ResistanceRoundsDown()
    {
        // Arrange
        var hero = Hero(resistances: [DamageType.Fire, DamageType.Fire]);

        // Act
        hero.TakeDamage(new(11, DamageType.Fire));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(15);
    }

    [Test]
    public async Task VulnerabilityDoublesDamage()
    {
        // Arrange
        var hero = Hero(hp: 30, vulnerabilities: [DamageType.Fire, DamageType.Fire]);

        // Act
        hero.TakeDamage(new(11, DamageType.Fire));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(8);
    }

    [Test]
    public async Task ResistanceThenVulnerabilityPreservesOddRounding()
    {
        // Arrange
        var hero = Hero(resistances: [DamageType.Fire], vulnerabilities: [DamageType.Fire]);

        // Act
        hero.TakeDamage(new(11, DamageType.Fire));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(10);
    }

    [Test]
    public async Task ImmunityPreventsDamage()
    {
        // Arrange
        var hero = Hero(immunities: [DamageType.Poison], vulnerabilities: [DamageType.Poison]);

        // Act
        hero.TakeDamage(new(40, DamageType.Poison));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(20);
    }

    [Test]
    public async Task FlatModifierAppliesBeforeResistance()
    {
        // Arrange
        var hero = Hero(resistances: [DamageType.Fire]);

        // Act
        hero.TakeDamage(new(11, DamageType.Fire, FlatModifier: -2));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(16);
    }

    [Test]
    public async Task ResistanceIsSpecificToDamageType()
    {
        // Arrange
        var hero = Hero(resistances: [DamageType.Fire]);

        // Act
        hero.TakeDamage(new(11, DamageType.Cold));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(9);
    }

    [Test]
    public async Task InvalidDamageLeavesFullHealthStateUnchanged()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(10, 20, 5);
        var before = HealthState(hero);
        var successes = hero.Health.DeathSuccesses;
        var failures = hero.Health.DeathFailures;

        // Act
        Action act = () => hero.TakeDamage(new(-1, DamageType.Fire));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(successes);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(failures);
    }

    [Test]
    public async Task ZeroDamageDoesNotChangeHealth()
    {
        // Arrange
        var hero = Hero();
        var before = HealthState(hero);

        // Act
        hero.TakeDamage(new(0, DamageType.Fire));

        // Assert
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

    [Test]
    [Arguments(18, true)]
    [Arguments(17, false)]
    public async Task MassiveDamageKillsAtExactOverflowThreshold(int damage, bool dead)
    {
        // Arrange
        var hero = Hero(hp: 12);
        hero.Health.Set(6, 12, 0);

        // Act
        hero.TakeDamage(new(damage, DamageType.Slashing));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(0);
        await Assert.That(hero.Health.IsDead).IsEqualTo(dead);
    }

    [Test]
    public async Task TemporaryHitPointsReduceMassiveDamageOverflow()
    {
        // Arrange
        var hero = Hero(hp: 12);
        hero.Health.Set(6, 12, 2);

        // Act
        hero.TakeDamage(new(18, DamageType.Slashing));

        // Assert
        await Assert.That(hero.Health.IsDead).IsFalse();
    }

    [Test]
    public async Task AcceptedTemporaryPoolCanBeSmaller()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(20, 20, 8);

        // Act
        hero.Health.OfferTemporary(3, accept: true);

        // Assert
        await Assert.That(hero.Health.TempHealth).IsEqualTo(3);
    }

    [Test]
    public async Task CallerMayKeepExistingTemporaryPool()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(20, 20, 3);

        // Act
        hero.Health.OfferTemporary(8, accept: false);

        // Assert
        await Assert.That(hero.Health.TempHealth).IsEqualTo(3);
    }

    [Test]
    public async Task TemporaryHitPointsCanExceedMaximum()
    {
        // Arrange
        var hero = Hero();

        // Act
        hero.Health.OfferTemporary(100, accept: true);

        // Assert
        await Assert.That(hero.Health.TempHealth).IsEqualTo(100);
    }

    [Test]
    public async Task HealingCapsAtMaximumWithoutHealingTemporaryPool()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(14, 20, 5);

        // Act
        hero.Heal(8);

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(20);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(5);
    }

    [Test]
    public async Task NegativeHealingLeavesStateUnchanged()
    {
        // Arrange
        var hero = Hero();
        var before = HealthState(hero);

        // Act
        Action act = () => hero.Heal(-1);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

    [Test]
    public async Task ZeroHealingIsNoOp()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(8, 20, 3);

        // Act
        hero.Heal(0);

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(8);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(3);
    }

    [Test]
    public async Task DeadCharacterCannotBeHealedNormally()
    {
        // Arrange
        var hero = Hero();
        hero.Health.MarkDead();
        var before = HealthState(hero);

        // Act
        Action act = () => hero.Heal(10);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

}



