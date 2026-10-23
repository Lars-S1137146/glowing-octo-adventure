// PHB 1: Variant: Customizing Ability Scores (deliberately included).
namespace LongRest.Domain.Tests.PointBuy;

public class PointBuyTests
{
    [Test]
    public async Task StandardExampleUsesAllTwentySevenPoints()
    {
        // Arrange
        

        // Act
        var result = LongRest.Domain.PointBuy.Validate(15, 14, 13, 12, 10, 8);

        // Assert
        await Assert.That(result.Spent).IsEqualTo(27);
        await Assert.That(result.Remaining).IsEqualTo(0);
    }

    [Test]
    public async Task PointBuyAllowsUnusedPoints()
    {
        // Arrange
        

        // Act
        var result = LongRest.Domain.PointBuy.Validate(8, 8, 8, 8, 8, 8);

        // Assert
        await Assert.That(result.Spent).IsEqualTo(0);
        await Assert.That(result.Remaining).IsEqualTo(27);
    }

    [Test]
    [Arguments(8,0)]
    [Arguments(9,1)]
    [Arguments(10,2)]
    [Arguments(11,3)]
    [Arguments(12,4)]
    [Arguments(13,5)]
    [Arguments(14,7)]
    [Arguments(15,9)]
    public async Task EachScoreUsesThe2014CostTable(int score, int cost)
    {
        // Arrange
        

        // Act
        var result = LongRest.Domain.PointBuy.Validate(score, 8, 8, 8, 8, 8);

        // Assert
        await Assert.That(result.Spent).IsEqualTo(cost);
    }

    [Test]
    [Arguments(7)]
    [Arguments(16)]
    [Arguments(18)]
    public async Task PointBuyHasNarrowerRangeThanCreatureScores(int score)
    {
        // Arrange
        

        // Act
        Action act = () => LongRest.Domain.PointBuy.Validate(score, 8, 8, 8, 8, 8);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TotalCostAboveBudgetDoesNotChangeCharacter()
    {
        // Arrange
        var hero = Hero();
        var before = hero.Stats.Strength;

        // Act
        Action act = () => LongRest.Domain.PointBuy.Validate(15, 15, 15, 15, 15, 15);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
        await Assert.That(hero.Stats.Strength).IsEqualTo(before);
    }

    [Test]
    [Arguments(0)]
    [Arguments(5)]
    [Arguments(7)]
    public async Task ExactlySixScoresAreRequired(int count)
    {
        // Arrange
        

        // Act
        Action act = () => LongRest.Domain.PointBuy.Validate(new int[count]);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task NullScoresAreRejected()
    {
        // Arrange
        

        // Act
        Action act = () => LongRest.Domain.PointBuy.Validate(null!);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
    }

}




