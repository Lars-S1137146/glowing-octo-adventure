// PHB 8: resting. Project convention: the caller declares duration requirements satisfied; Hit Dice are configured directly.
namespace LongRest.Domain.Tests.Resting;

public class RestTests
{
    [Test]
    public async Task ShortRestAddsConstitutionToEveryDie()
    {
        // Arrange
        var hero = Hero(new Stats(constitution: 14), hitDice: new(4, 10));
        hero.Health.Set(1, 20, 0);

        // Act
        hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 6, 3));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(14);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(2);
    }

    [Test]
    public async Task NegativeConstitutionCannotCauseNegativeHealing()
    {
        // Arrange
        var hero = Hero(new Stats(constitution: 4), hitDice: new(1, 8));
        hero.Health.Set(5, 20, 0);

        // Act
        hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 1));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(5);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(1);
    }

    [Test]
    public async Task RestHealingCapsButStillSpendsDie()
    {
        // Arrange
        var hero = Hero(hitDice: new(1, 8));

        // Act
        hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 8));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(20);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(1);
    }

    [Test]
    public async Task ZeroHitDiceCanBeSpent()
    {
        // Arrange
        var hero = Hero();
        hero.Health.Set(5, 20, 0);

        // Act
        hero.ShortRest(new DiceRoll(hero.HitDice.Sides));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(5);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(0);
    }

    [Test]
    public async Task CannotSpendMoreHitDiceThanRemain()
    {
        // Arrange
        var hero = Hero(hitDice: new(4, 10, 2));
        hero.Health.Set(5, 20, 3);
        var before = HealthState(hero);

        // Act
        Action act = () => hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 1, 2, 3));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(2);
    }

    [Test]
    public async Task AllHitDiceValidateBeforeAnyAreSpent()
    {
        // Arrange
        var hero = Hero(hitDice: new(4, 10));
        hero.Health.Set(5, 20, 3);
        var before = HealthState(hero);

        // Act
        Action act = () => hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 6, 11));

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(0);
    }

    [Test]
    [Arguments(4, 3, 1)]
    [Arguments(1, 1, 0)]
    [Arguments(8, 1, 0)]
    [Arguments(5, 5, 3)]
    public async Task LongRestReturnsHalfTotalRoundedDownWithMinimumOne(int total, int spent, int expectedSpent)
    {
        // Arrange
        var hero = Hero(hitDice: new(total, 8, spent));
        hero.Health.Set(1, 20, 8);

        // Act
        hero.LongRest();

        // Assert
        await Assert.That(hero.HitDice.Spent).IsEqualTo(expectedSpent);
        await Assert.That(hero.Health.Value).IsEqualTo(20);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(0);
    }

    [Test]
    public async Task LongRestAtZeroHitPointsIsAtomic()
    {
        // Arrange
        var magic = Magic();
        var hero = Hero(hitDice: new(4, 8, 3), spells: magic);
        hero.Health.Set(0, 20, 8);
        var before = HealthState(hero);
        var slots = magic.RemainingSlots(1);

        // Act
        Action act = () => hero.LongRest();

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(3);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(slots);
    }

    [Test]
    public async Task LongRestDoesNotRemoveOrdinaryConditions()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Poisoned);

        // Act
        hero.LongRest();

        // Assert
        await Assert.That(hero.ActiveConditions.Contains(Condition.Poisoned)).IsTrue();
    }

    [Test]
    [Arguments(0, 8, 0)]
    [Arguments(1, 7, 0)]
    [Arguments(1, 8, -1)]
    [Arguments(1, 8, 2)]
    public async Task InvalidHitDiceConfigurationIsRejected(int total, int sides, int spent)
    {
        // Arrange
        

        // Act
        Action act = () => new HitDice(total, sides, spent);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

}



