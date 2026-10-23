// PHB 1 and 7: ability scores, modifiers and advancement.
namespace LongRest.Domain.Tests.AbilityScores;

public class ProficiencyBonusTests
{
    [Test]
    [Arguments(1,2)]
    [Arguments(4,2)]
    [Arguments(5,3)]
    [Arguments(8,3)]
    [Arguments(9,4)]
    [Arguments(12,4)]
    [Arguments(13,5)]
    [Arguments(16,5)]
    [Arguments(17,6)]
    [Arguments(20,6)]
    public async Task PlayerLevelUsesAdvancementTable(int level, int expected)
    {
        // Arrange
        var character = Hero(level: level);

        // Act
        var result = character.ProficiencyBonus;

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0)]
    [Arguments(21)]
    public async Task InvalidPlayerLevelIsRejected(int level)
    {
        // Arrange
        

        // Act
        Action act = () => Hero(level: level);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task MonsterNeedsNoPlayerClassOrLevel()
    {
        // Arrange
        

        // Act
        var monster = new Character("Ogre", new Stats(19, 8, 16, 5, 7, 7), level: null, proficiencyBonus: 2);

        // Assert
        await Assert.That(monster.PlayerLevel).IsNull();
        await Assert.That(monster.ProficiencyBonus).IsEqualTo(2);
        await Assert.That(monster.Stats.Strength).IsEqualTo(19);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task ExplicitProficiencyMustBePositive(int bonus)
    {
        // Arrange
        

        // Act
        Action act = () => new Character("Monster", new Stats(), level: null, proficiencyBonus: bonus);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CannotCombinePlayerLevelAndStatBlockProficiency()
    {
        // Arrange
        

        // Act
        Action act = () => new Character("Ambiguous", new Stats(), level: 3, proficiencyBonus: 4);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task StatisticsCannotBeNull()
    {
        // Arrange
        

        // Act
        Action act = () => new Character("Missing", (Stats)null!);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
    }

}



