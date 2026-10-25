// PHB 9: combat order, actions and movement. Project convention: caller-selected initiative ties; no map.
namespace LongRest.Domain.Tests.Encounters;

public class EncounterValidationTests
{
    [Test]
    public async Task EmptyEncounterCannotStart()
    {
        // Arrange
        var encounter = new Encounter(Array.Empty<InitiativeEntry>());

        // Act
        Action act = () => encounter.StartTurn();

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    [Arguments(0, 1)]
    [Arguments(1, 0)]
    [Arguments(-1, 1)]
    public async Task ConfiguredAttackAndActionAllowancesMustBePositive(int actions, int attacks)
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => { var encounter = new Encounter([new(hero, 15, ActionsPerTurn: actions, AttacksPerAction: attacks)]); encounter.StartTurn(); };

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task NegativeMovementDoesNotIncreaseAllowance()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);

        // Act
        Action act = () => encounter.Move(hero, -1);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(encounter.MovementRemaining).IsEqualTo(30);
    }

    [Test]
    public async Task NormalTurnRestoresActionBonusAndMovement()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dash);
        encounter.UseBonusAction(hero, true);
        encounter.Move(hero, 60);

        // Act
        encounter.EndTurn(hero);

        // Assert
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
        await Assert.That(encounter.BonusActionAvailable).IsTrue();
        await Assert.That(encounter.MovementRemaining).IsEqualTo(30);
    }

    [Test]
    public async Task InitiativeUsesExplicitCharacterModifier()
    {
        // Arrange
        var first = Hero();
        first.SetInitiativeBonus(4);
        var second = Hero();
        var encounter = new Encounter([new(first, 10), new(second, 12)]);

        // Act
        encounter.StartTurn();

        // Assert
        await Assert.That(encounter.Current).IsEqualTo(first);
    }

}



