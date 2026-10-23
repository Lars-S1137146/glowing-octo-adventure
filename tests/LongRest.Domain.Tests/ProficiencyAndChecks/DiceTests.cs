namespace LongRest.Domain.Tests.ProficiencyAndChecks;

public class DiceTests
{
    [Test]
    public async Task NormalThrowKeepsItsFace()
    {
        DiceThrow roll = new NormalDiceThrow(13);
        await Assert.That(roll.Value).IsEqualTo(13);
        await Assert.That(roll.Faces.Single()).IsEqualTo(13);
    }

    [Test]
    public async Task AdvantageSelectsHigherDie()
    {
        DiceThrow roll = new AdvantageDiceThrow(4, 17);
        await Assert.That(roll.Value).IsEqualTo(17);
    }

    [Test]
    public async Task DisadvantageSelectsLowerDie()
    {
        DiceThrow roll = new DisadvantageDiceThrow(4, 17);
        await Assert.That(roll.Value).IsEqualTo(4);
    }

    [Test]
    [Arguments(0, 0, ThrowType.Normal)]
    [Arguments(2, 0, ThrowType.Advantage)]
    [Arguments(0, 2, ThrowType.Disadvantage)]
    [Arguments(2, 1, ThrowType.Normal)]
    [Arguments(1, 2, ThrowType.Normal)]
    public async Task SourcesDoNotStackAndOpposingSourcesCancel(int advantage, int disadvantage, ThrowType expected)
    {
        await Assert.That(DiceThrow.RequiredType(advantage, disadvantage)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0)]
    [Arguments(21)]
    public async Task InvalidD20FaceIsRejected(int face)
    {
        await Assert.That(() => new NormalDiceThrow(face)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => new AdvantageDiceThrow(20, face)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => new DisadvantageDiceThrow(face, 20)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task WrongThrowTypeIsRejected()
    {
        var hero = Hero();
        await Assert.That(() => hero.Check(Ability.Strength, new AdvantageDiceThrow(4, 17), 10))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => hero.Check(Ability.Strength, new NormalDiceThrow(17), 10, context: new(AdvantageSources: 1)))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => hero.Check(Ability.Strength, new AdvantageDiceThrow(4, 17), 10, context: new(2, 1)))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task MissingThrowIsRejectedWhenARollIsRequired()
    {
        await Assert.That(() => Hero().Check(Ability.Strength, null, 10)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1, 0)]
    [Arguments(0, -1)]
    public async Task NegativeSourceCountsAreRejected(int advantage, int disadvantage)
    {
        await Assert.That(() => DiceThrow.RequiredType(advantage, disadvantage)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task DamageDiceValidateAndCopyTheirFaces()
    {
        int[] faces = [3, 5];
        var roll = new DiceRoll(6, faces);
        faces[0] = 100;
        await Assert.That(roll.Total).IsEqualTo(8);
        await Assert.That(() => new DiceRoll(6, 3, 7)).ThrowsExactly<ArgumentOutOfRangeException>();
    }
}
