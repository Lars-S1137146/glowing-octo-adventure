// PHB 1 and 7: ability scores, modifiers and advancement.
namespace LongRest.Domain.Tests.AbilityScores;

public class AbilityModifierTests
{
    [Test]
    
    [Arguments(1,-5)]
    [Arguments(8,-1)]
    [Arguments(9,-1)]
    [Arguments(10,0)]
    [Arguments(11,0)]
    [Arguments(16,3)]
    [Arguments(20,5)]
    [Arguments(30,10)]
    public async Task ScoresUseFloorIncludingNegativeHalves(int score, int expected)
    {
        // Arrange
        

        // Act
        var result = Stats.Modifier(score);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task SixAbilitiesRemainDistinct()
    {
        // Arrange
        var stats = new Stats(1, 8, 12, 16, 20, 30);

        // Act
        var scores = Enum.GetValues<Ability>().Select(stats.GetScore).ToArray();

        // Assert
        await Assert.That(scores.SequenceEqual(new[] { 1, 8, 12, 16, 20, 30 })).IsTrue();
    }

    [Test]
    [Arguments(-2)]
    [Arguments(0)]
    [Arguments(31)]
    public async Task InvalidScoreIsRejected(int score)
    {
        // Arrange
        

        // Act
        Action act = () => new Stats(strength: score);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(-2)]
    [Arguments(0)]
    [Arguments(31)]
    public async Task InvalidModifierInputIsRejected(int score)
    {
        // Arrange
        

        // Act
        Action act = () => Stats.Modifier(score);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task InvalidScoreEditLeavesAllAbilitiesUnchanged()
    {
        // Arrange
        var stats = new Stats(16, 12);

        // Act
        Action act = () => stats.Set(18, 40, 10, 10, 10, 10);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(stats.Strength).IsEqualTo(16);
        await Assert.That(stats.Dexterity).IsEqualTo(12);
    }

    [Test]
    public async Task AllowedAbilityEditUpdatesDerivedModifier()
    {
        // Arrange
        var stats = new Stats();

        // Act
        stats.Set(9, 10, 10, 10, 10, 10);

        // Assert
        await Assert.That(stats.GetModifier(Ability.Strength)).IsEqualTo(-1);
    }

    [Test]
    public async Task InvalidConstitutionEditDoesNotChangeOtherAbilitiesOrHealth()
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => hero.Stats.Set(18, 18, 99, 18, 18, 18);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.Stats.Strength).IsEqualTo(10);
        await Assert.That(hero.Level).IsEqualTo(1);
        await Assert.That(hero.Health.Max).IsEqualTo(20);
    }

}

