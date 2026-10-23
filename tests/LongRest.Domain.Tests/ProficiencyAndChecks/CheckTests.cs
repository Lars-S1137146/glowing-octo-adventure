// PHB 7: checks, skills, proficiency, contests, saves and advantage.
namespace LongRest.Domain.Tests.ProficiencyAndChecks;

public class CheckTests
{
    [Test]
    [Arguments(Training.None, 14)]
    [Arguments(Training.Proficient, 16)]
    [Arguments(Training.Expertise, 18)]
    public async Task ProficiencyAndExpertiseApplyOnce(Training training, int expected)
    {
        // Arrange
        var hero = Hero(new Stats(strength: 16), proficiencies: new(skills: new Dictionary<Skill, Training> { [Skill.Athletics] = training }));

        // Act
        var result = hero.Check(Ability.Strength, new NormalDiceThrow(11), 16, Skill.Athletics);

        // Assert
        await Assert.That(result.Total).IsEqualTo(expected);
        await Assert.That(result.Success).IsEqualTo(expected >= 16);
    }

    [Test]
    public async Task SkillMayUseDifferentAbility()
    {
        // Arrange
        var hero = Hero(new Stats(strength: 16, charisma: 12), proficiencies: new(skills: new Dictionary<Skill, Training> { [Skill.Intimidation] = Training.Proficient }));

        // Act
        var result = hero.Check(Ability.Strength, new NormalDiceThrow(9), 14, Skill.Intimidation);

        // Assert
        await Assert.That(result.Total).IsEqualTo(14);
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task SaveUsesOnlyConfiguredSavingThrowTraining()
    {
        // Arrange
        var hero = Hero(new Stats(strength: 16), proficiencies: new(saves: [Ability.Strength]));

        // Act
        var trained = hero.Save(Ability.Strength, new NormalDiceThrow(10), 15);
        var untrained = hero.Save(Ability.Wisdom, new NormalDiceThrow(10), 15);

        // Assert
        await Assert.That(trained.Total).IsEqualTo(15);
        await Assert.That(untrained.Total).IsEqualTo(10);
    }

    [Test]
    public async Task NaturalTwentyDoesNotAutomaticallyPassCheck()
    {
        // Arrange
        var hero = Hero(new Stats(strength: 8));

        // Act
        var result = hero.Check(Ability.Strength, new NormalDiceThrow(20), 25);

        // Assert
        await Assert.That(result.Total).IsEqualTo(19);
        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task NaturalOneDoesNotAutomaticallyFailCheck()
    {
        // Arrange
        var hero = new Character("Expert", new Stats(strength: 30), level: null, proficiencyBonus: 6);

        // Act
        var result = hero.Check(Ability.Strength, new NormalDiceThrow(1), 10);

        // Assert
        await Assert.That(result.Total).IsEqualTo(11);
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task NaturalTwentyDoesNotAutomaticallyPassSave()
    {
        // Arrange
        var hero = Hero(new Stats(wisdom: 8));

        // Act
        var result = hero.Save(Ability.Wisdom, new NormalDiceThrow(20), 25);

        // Assert
        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task NaturalOneDoesNotAutomaticallyFailSave()
    {
        // Arrange
        var hero = Hero(new Stats(strength: 30));

        // Act
        var result = hero.Save(Ability.Strength, new NormalDiceThrow(1), 10);

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task ContestTiePreservesExistingSituation()
    {
        // Arrange
        var first = Hero();
        var second = Hero();

        // Act
        var result = first.Contest(second, Ability.Strength, null, new NormalDiceThrow(17), Ability.Dexterity, null, new NormalDiceThrow(17));

        // Assert
        await Assert.That(result.Outcome).IsEqualTo(ContestOutcome.Tie);
    }

    [Test]
    public async Task ContestHigherTotalWins()
    {
        // Arrange
        var first = Hero();
        var second = Hero();

        // Act
        var result = first.Contest(second, Ability.Strength, null, new NormalDiceThrow(18), Ability.Dexterity, null, new NormalDiceThrow(17));

        // Assert
        await Assert.That(result.Outcome).IsEqualTo(ContestOutcome.FirstWins);
    }

    [Test]
    public async Task ContextAddsFlatModifierAndCancelsAdvantage()
    {
        // Arrange
        var hero = Hero();

        // Act
        var result = hero.Check(Ability.Wisdom, new NormalDiceThrow(12), 15, context: new(2, 1, 3));

        // Assert
        await Assert.That(result.Total).IsEqualTo(15);
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    [Arguments(Cover.Half, 12)]
    [Arguments(Cover.ThreeQuarters, 15)]
    public async Task DexteritySaveReceivesCoverBonus(Cover cover, int expected)
    {
        // Arrange
        var hero = Hero();

        // Act
        var result = hero.Save(Ability.Dexterity, new NormalDiceThrow(10), 15, new(Cover: cover));

        // Assert
        await Assert.That(result.Total).IsEqualTo(expected);
    }

    [Test]
    public async Task CoverDoesNotBoostOtherSaves()
    {
        // Arrange
        var hero = Hero();

        // Act
        var result = hero.Save(Ability.Wisdom, new NormalDiceThrow(10), 15, new(Cover: Cover.Half));

        // Assert
        await Assert.That(result.Total).IsEqualTo(10);
    }

    [Test]
    public async Task InvalidCheckLeavesCharacterUnchanged()
    {
        // Arrange
        var hero = Hero();
        var before = HealthState(hero);

        // Act
        Action act = () => hero.Check(Ability.Strength, new NormalDiceThrow(0), 10);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(hero)).IsEqualTo(before);
    }

}



