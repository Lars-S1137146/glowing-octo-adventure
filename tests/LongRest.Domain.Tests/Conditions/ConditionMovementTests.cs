// PHB Appendix A: conditions. Exhaustion is excluded; battlefield facts come from the caller.
namespace LongRest.Domain.Tests.Conditions;

public class ConditionMovementTests
{
    [Test]
    [Arguments(Condition.Grappled)]
    [Arguments(Condition.Restrained)]
    [Arguments(Condition.Paralyzed)]
    [Arguments(Condition.Petrified)]
    [Arguments(Condition.Stunned)]
    [Arguments(Condition.Unconscious)]
    public async Task ImmobilizedConditionsPreventMovement(Condition condition)
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(condition);
        var encounter = Fight(hero);

        // Act
        Action act = () => encounter.Move(hero, 1);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.CurrentSpeed).IsEqualTo(0);
    }

    [Test]
    public async Task CrawlingCostsDoubleMovement()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Prone);
        var encounter = Fight(hero);

        // Act
        encounter.Move(hero, 5);

        // Assert
        await Assert.That(encounter.MovementRemaining).IsEqualTo(20);
    }

    [Test]
    public async Task CannotStandWithoutHalfSpeedRemaining()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Prone);
        var encounter = Fight(hero);
        encounter.Move(hero, 10);

        // Act
        Action act = () => encounter.StandUp(hero);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.ActiveConditions.Contains(Condition.Prone)).IsTrue();
        await Assert.That(encounter.MovementRemaining).IsEqualTo(10);
    }

    [Test]
    public async Task InvalidConditionRemovalPreservesCurrentConditions()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Poisoned);

        // Act
        Action act = () => hero.RemoveCondition((Condition)999);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.ActiveConditions.Contains(Condition.Poisoned)).IsTrue();
    }

}



