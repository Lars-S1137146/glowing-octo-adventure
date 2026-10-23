// PHB 7: checks, skills, proficiency, contests, saves and advantage.
namespace LongRest.Domain.Tests.ProficiencyAndChecks;

public class ToolCheckTests
{
    [Test]
    [Arguments(Training.None, 14)]
    [Arguments(Training.Proficient, 16)]
    [Arguments(Training.Expertise, 18)]
    public async Task ToolTrainingUsesSelectedAbilityAndExpertise(Training training, int expected)
    {
        // Arrange
        var tools = new Equipment("Thieves' tools", EquipmentKind.Tool);
        var hero = Hero(new Stats(dexterity: 16), proficiencies: new(tools: new Dictionary<Equipment, Training> { [tools] = training }));

        // Act
        var result = hero.Check(Ability.Dexterity, new NormalDiceThrow(11), 15, tool: tools);

        // Assert
        await Assert.That(result.Total).IsEqualTo(expected);
    }

    [Test]
    public async Task OverlappingSkillAndToolTrainingDoNotStack()
    {
        // Arrange
        var tools = new Equipment("Thieves' tools", EquipmentKind.Tool);
        var hero = Hero(new Stats(dexterity: 16), proficiencies: new(
            skills: new Dictionary<Skill, Training> { [Skill.SleightOfHand] = Training.Proficient },
            tools: new Dictionary<Equipment, Training> { [tools] = Training.Proficient }));

        // Act
        var result = hero.Check(Ability.Dexterity, new NormalDiceThrow(11), 15, Skill.SleightOfHand, tool: tools);

        // Assert
        await Assert.That(result.Total).IsEqualTo(16);
    }

}



