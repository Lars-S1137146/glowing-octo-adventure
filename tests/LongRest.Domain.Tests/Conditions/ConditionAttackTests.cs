// PHB Appendix A: conditions. Exhaustion is excluded; battlefield facts come from the caller.
namespace LongRest.Domain.Tests.Conditions;

public class ConditionAttackTests
{
    [Test]
    [Arguments(Condition.Blinded)]
    [Arguments(Condition.Poisoned)]
    [Arguments(Condition.Prone)]
    [Arguments(Condition.Restrained)]
    public async Task AttackerConditionsImposeDisadvantage(Condition condition)
    {
        // Arrange
        var attacker = Hero();
        attacker.AddCondition(condition);
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new DisadvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    [Arguments(Condition.Blinded)]
    [Arguments(Condition.Paralyzed)]
    [Arguments(Condition.Petrified)]
    [Arguments(Condition.Restrained)]
    [Arguments(Condition.Stunned)]
    [Arguments(Condition.Unconscious)]
    public async Task VulnerableTargetConditionsGrantAdvantage(Condition condition)
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());
        var target = Hero();
        target.AddCondition(condition);

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new AdvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(17);
    }

    [Test]
    public async Task PoisonedAttackerAndBlindedDefenderCancel()
    {
        // Arrange
        var attacker = Hero();
        attacker.AddCondition(Condition.Poisoned);
        attacker.AddCondition(Condition.Prone);
        var sword = Own(attacker, Sword());
        var target = Hero();
        target.AddCondition(Condition.Blinded);

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(12), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(12);
    }

    [Test]
    public async Task InvisibleAttackerHasAdvantage()
    {
        // Arrange
        var attacker = Hero();
        attacker.AddCondition(Condition.Invisible);
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new AdvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(17);
    }

    [Test]
    public async Task InvisibleDefenderImposesDisadvantage()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());
        var target = Hero();
        target.AddCondition(Condition.Invisible);

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new DisadvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    [Arguments(5, 17)]
    [Arguments(20, 4)]
    public async Task ProneDefenderUsesMeasuredDistance(int distance, int expected)
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());
        var target = Hero();
        target.AddCondition(Condition.Prone);

        // Act
        var result = CombatRules.Attack(attacker, target, bow, distance <= 5 ? new AdvantageDiceThrow(4, 17) : new DisadvantageDiceThrow(4, 17), Nearby(distance));

        // Assert
        await Assert.That(result.Die).IsEqualTo(expected);
    }

    [Test]
    [Arguments(Condition.Paralyzed, 5, true)]
    [Arguments(Condition.Unconscious, 5, true)]
    [Arguments(Condition.Paralyzed, 20, false)]
    [Arguments(Condition.Unconscious, 20, false)]
    public async Task ParalyzedOrUnconsciousCriticalRequiresHitWithinFiveFeet(Condition condition, int distance, bool critical)
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());
        var target = Hero();
        target.AddCondition(condition);
        if (condition == Condition.Unconscious) target.AddCondition(Condition.Prone);

        // Act
        // At range, Prone cancels the advantage from Unconscious.
        DiceThrow dice = condition == Condition.Unconscious && distance > 5 ? new NormalDiceThrow(17) : new AdvantageDiceThrow(4, 17);
        var result = CombatRules.Attack(attacker, target, bow, dice, Nearby(distance));

        // Assert
        await Assert.That(result.Hit).IsTrue();
        await Assert.That(result.Critical).IsEqualTo(critical);
    }

    [Test]
    public async Task CharmedCreatureCannotAttackCharmSource()
    {
        // Arrange
        var attacker = Hero();
        var source = Hero();
        attacker.AddCondition(Condition.Charmed, source);
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, source, sword, new NormalDiceThrow(17), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task FrightenedAttackDependsOnVisibleSource()
    {
        // Arrange
        var attacker = Hero();
        attacker.AddCondition(Condition.Frightened, Hero());
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new DisadvantageDiceThrow(4, 17), Nearby() with { FearSourceVisible = true });

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

}



