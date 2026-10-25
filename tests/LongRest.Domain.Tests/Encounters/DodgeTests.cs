// PHB 9: combat order, actions and movement. Project convention: caller-selected initiative ties; no map.
namespace LongRest.Domain.Tests.Encounters;

public class DodgeTests
{
    [Test]
    public async Task DodgePenalizesAttacksFromVisibleAttacker()
    {
        // Arrange
        var defender = Hero();
        var attacker = Hero();
        var encounter = Fight(defender, attacker);
        encounter.TakeAction(defender, CombatAction.Dodge);
        encounter.EndTurn(defender);
        encounter.TakeAction(attacker, CombatAction.Attack);

        // Act
        var result = encounter.Attack(attacker, defender, null, new DisadvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    public async Task DodgeDoesNotPenalizeAttackerDefenderCannotSee()
    {
        // Arrange
        var defender = Hero();
        var attacker = Hero();
        var encounter = Fight(defender, attacker);
        encounter.TakeAction(defender, CombatAction.Dodge);
        encounter.EndTurn(defender);
        encounter.TakeAction(attacker, CombatAction.Attack);

        // Act
        var result = encounter.Attack(attacker, defender, null, new AdvantageDiceThrow(4, 17), Nearby() with { TargetCanSeeAttacker = false });

        // Assert
        await Assert.That(result.Die).IsEqualTo(17);
    }

    [Test]
    public async Task DodgeGrantsDexteritySaveAdvantage()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dodge);

        // Act
        var result = encounter.Save(hero, Ability.Dexterity, new AdvantageDiceThrow(4, 17), 10);

        // Assert
        await Assert.That(result.Die).IsEqualTo(17);
    }

    [Test]
    public async Task DodgeExpiresAtStartOfNextOwnTurn()
    {
        // Arrange
        var hero = Hero();
        var other = Hero();
        var encounter = Fight(hero, other);
        encounter.TakeAction(hero, CombatAction.Dodge);
        encounter.EndTurn(hero);
        var before = encounter.IsDodging(hero);

        // Act
        encounter.EndTurn(other);

        // Assert
        await Assert.That(before).IsTrue();
        await Assert.That(encounter.IsDodging(hero)).IsFalse();
    }

    [Test]
    [Arguments(Condition.Incapacitated)]
    [Arguments(Condition.Grappled)]
    public async Task IncapacitationOrZeroSpeedEndsDodgeBenefit(Condition condition)
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Dodge);

        // Act
        hero.AddCondition(condition);

        // Assert
        await Assert.That(encounter.IsDodging(hero)).IsFalse();
    }

}



