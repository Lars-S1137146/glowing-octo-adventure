// PHB 9: damage, healing, temporary HP and dropping to zero. All creatures use the same death rules by project convention.
namespace LongRest.Domain.Tests.Health;

public class HealthConfigurationTests
{
    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(-1, 20, 0)]
    [Arguments(21, 20, 0)]
    [Arguments(10, 20, -1)]
    public async Task InvalidHealthConfigurationIsRejected(int current, int maximum, int temporary)
    {
        // Arrange
        

        // Act
        Action act = () => new LongRest.Domain.Health(current, maximum, temporary);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task InvalidTemporaryOfferDoesNotChangeEitherPool()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(10, 20, 3);
        var before = HealthState(hero);

        // Act
        Action act = () => hero.Health.OfferTemporary(-1, true);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

    [Test]
    public async Task InvalidDamageTypeDoesNotChangeState()
    {
        // Arrange
        var hero = Hero();
        var before = HealthState(hero);

        // Act
        Action act = () => hero.TakeDamage(new(4, (DamageType)999));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

    [Test]
    public async Task NonlethalDeclarationRequiresMeleeAttack()
    {
        // Arrange
        var hero = Hero();
        var before = HealthState(hero);

        // Act
        Action act = () => hero.TakeDamage(new(20, DamageType.Piercing, Nonlethal: true, Melee: false));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

    [Test]
    public async Task NullDamageIsRejected()
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => hero.TakeDamage(null!);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
    }

}



