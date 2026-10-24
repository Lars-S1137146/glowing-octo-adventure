// PHB 9: damage, healing, temporary HP and dropping to zero. All creatures use the same death rules by project convention.
namespace LongRest.Domain.Tests.Health;

public class DeathSaveTests
{
    [Test]
    [Arguments(9, 0, 1)]
    [Arguments(10, 1, 0)]
    [Arguments(19, 1, 0)]
    [Arguments(1, 0, 2)]
    public async Task DeathSaveTenOrMoreSucceeds(int die, int successes, int failures)
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);

        // Act
        hero.Health.DeathSave(new NormalDiceThrow(die));

        // Assert
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(successes);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(failures);
    }

    [Test]
    public async Task NaturalTwentyRestoresOneHitPointAndResetsCounters()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.DeathSave(new NormalDiceThrow(2));

        // Act
        hero.Health.DeathSave(new NormalDiceThrow(20));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(1);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(0);
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(0);
        await Assert.That(hero.Health.IsUnconscious).IsFalse();
    }

    [Test]
    public async Task ThirdSuccessStabilizesWithoutHealing()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.DeathSave(new NormalDiceThrow(10));
        hero.Health.DeathSave(new NormalDiceThrow(2));
        hero.Health.DeathSave(new NormalDiceThrow(11));

        // Act
        hero.Health.DeathSave(new NormalDiceThrow(12));

        // Assert
        await Assert.That(hero.Health.IsStable).IsTrue();
        await Assert.That(hero.Health.Value).IsEqualTo(0);
        await Assert.That(hero.Health.IsUnconscious).IsTrue();
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(0);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(0);
    }

    [Test]
    public async Task ThirdFailureKills()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.DeathSave(new NormalDiceThrow(1));

        // Act
        hero.Health.DeathSave(new NormalDiceThrow(2));

        // Assert
        await Assert.That(hero.Health.IsDead).IsTrue();
    }

    [Test]
    public async Task HealingResetsDeathSaves()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.DeathSave(new NormalDiceThrow(1));

        // Act
        hero.Heal(1);

        // Assert
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(0);
        await Assert.That(hero.Health.IsUnconscious).IsFalse();
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 2)]
    public async Task DamageAtZeroAddsFailures(bool critical, int expected)
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);

        // Act
        hero.TakeDamage(new(1, DamageType.Slashing, Critical: critical));

        // Assert
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(expected);
    }

    [Test]
    public async Task DamageAtZeroEqualToMaximumKillsInstantly()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);

        // Act
        hero.TakeDamage(new(20, DamageType.Slashing));

        // Assert
        await Assert.That(hero.Health.IsDead).IsTrue();
    }

    [Test]
    public async Task DamageMakesStableCreatureUnstable()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.Stabilize();

        // Act
        hero.TakeDamage(new(1, DamageType.Fire));

        // Assert
        await Assert.That(hero.Health.IsStable).IsFalse();
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(1);
    }

    [Test]
    public async Task NonlethalMeleeDamageStabilizes()
    {
        // Arrange
        var hero = Hero();

        // Act
        hero.TakeDamage(new(20, DamageType.Bludgeoning, Nonlethal: true, Melee: true));

        // Assert
        await Assert.That(hero.Health.IsStable).IsTrue();
        await Assert.That(hero.Health.Value).IsEqualTo(0);
    }

    [Test]
    public async Task MedicineAtDcTenStabilizes()
    {
        // Arrange
        var healer = Hero();
        var target = Hero();
        target.Health.Set(0, 20, 0);

        // Act
        var success = healer.TryStabilize(target, new NormalDiceThrow(10));

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(target.Health.IsStable).IsTrue();
    }

    [Test]
    public async Task FailedMedicineDoesNotStabilize()
    {
        // Arrange
        var healer = Hero();
        var target = Hero();
        target.Health.Set(0, 20, 0);

        // Act
        var success = healer.TryStabilize(target, new NormalDiceThrow(9));

        // Assert
        await Assert.That(success).IsFalse();
        await Assert.That(target.Health.IsStable).IsFalse();
    }

    [Test]
    public async Task ExplicitStableRecoveryRestoresOneHitPoint()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.Stabilize();

        // Act
        hero.Health.RecoverFromStable();

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(1);
    }

    [Test]
    public async Task InvalidDeathSaveDoesNotChangeCounters()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(0, 20, 0);
        hero.Health.DeathSave(new NormalDiceThrow(10));

        // Act
        Action act = () => hero.Health.DeathSave(new NormalDiceThrow(21));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(1);
        await Assert.That(hero.Health.DeathFailures).IsEqualTo(0);
    }

    [Test]
    public async Task ConsciousCreatureDoesNotMakeDeathSaves()
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => hero.Health.DeathSave(new NormalDiceThrow(10));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

}



