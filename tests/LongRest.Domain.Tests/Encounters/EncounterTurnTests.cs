// PHB 9: combat order, actions and movement. Project convention: caller-selected initiative ties; no map.
namespace LongRest.Domain.Tests.Encounters;

public class EncounterTurnTests
{
    [Test]
    public async Task InitiativeAddsDexterityAndOrdersDescending()
    {
        // Arrange
        var first = Hero(new Stats(dexterity: 14));
        var second = Hero();
        var third = Hero(new Stats(dexterity: 12));
        var encounter = new Encounter([new(first, 15), new(second, 12), new(third, 20)]);

        // Act
        encounter.StartTurn();

        // Assert
        await Assert.That(encounter.TurnOrder.SequenceEqual(new[] { third, first, second })).IsTrue();
        await Assert.That(encounter.RoundNumber).IsEqualTo(1);
    }

    [Test]
    public async Task TiedInitiativeUsesCallerOrder()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        // Project convention: the caller chooses tie order instead of a hidden reroll.
        var encounter = new Encounter([new(first, 15), new(second, 15)], [second, first]);

        // Act
        encounter.StartTurn();

        // Assert
        await Assert.That(encounter.Current).IsEqualTo(second);
    }

    [Test]
    public async Task TiedInitiativeWithoutExplicitOrderIsRejected()
    {
        // Arrange
        var encounter = new Encounter([new(Hero(), 15), new(Hero(), 15)]);

        // Act
        Action act = () => encounter.StartTurn();

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.RoundNumber).IsEqualTo(0);
    }

    [Test]
    public async Task TieOrderMustContainExactlyTiedCharacters()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        var encounter = new Encounter([new(first, 15), new(second, 15)], [first, first]);

        // Act
        Action act = () => encounter.StartTurn();

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
        await Assert.That(encounter.RoundNumber).IsEqualTo(0);
    }

    [Test]
    public async Task DuplicateParticipantsAreRejected()
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => { var encounter = new Encounter([new(hero, 15), new(hero, 10)]); encounter.StartTurn(); };

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task InvalidInitiativeDieIsRejected()
    {
        // Arrange
        

        // Act
        Action act = () => { var encounter = new Encounter([new(Hero(), 21)]); encounter.StartTurn(); };

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RoundsPreserveInitiativeOrder()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        var encounter = Fight(first, second);

        // Act
        encounter.EndTurn(first);
        encounter.EndTurn(second);

        // Assert
        await Assert.That(encounter.RoundNumber).IsEqualTo(2);
        await Assert.That(encounter.Current).IsEqualTo(first);
    }

    [Test]
    public async Task WrongParticipantCannotEndTurn()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        var encounter = Fight(first, second);
        var actions = encounter.ActionsRemaining;

        // Act
        Action act = () => encounter.EndTurn(second);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.Current).IsEqualTo(first);
        await Assert.That(encounter.RoundNumber).IsEqualTo(1);
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(actions);
    }

    [Test]
    public async Task ActionCannotBeTakenTwice()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Attack);

        // Act
        Action act = () => encounter.TakeAction(hero, CombatAction.Attack);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
    }

    [Test]
    public async Task OutOfTurnActionDoesNotSpendResources()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        var encounter = Fight(first, second);

        // Act
        Action act = () => encounter.TakeAction(second, CombatAction.Dash);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
        await Assert.That(encounter.MovementRemaining).IsEqualTo(30);
    }

    [Test]
    public async Task ExtraAttackDoesNotGrantAnotherAction()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero, attacks: 2);
        encounter.TakeAction(hero, CombatAction.Attack);

        // Act
        encounter.Attack(hero, Hero(), null, new NormalDiceThrow(10), Nearby());
        encounter.Attack(hero, Hero(), null, new NormalDiceThrow(10), Nearby());

        // Assert
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
        await Assert.That(encounter.AttacksRemaining).IsEqualTo(0);
        await Assert.That(() => encounter.Attack(hero, Hero(), null, new NormalDiceThrow(10), Nearby())).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task AdditionalConfiguredActionIsAvailable()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero, actions: 2);

        // Act
        encounter.TakeAction(hero, CombatAction.Dash);
        encounter.TakeAction(hero, CombatAction.Disengage);

        // Assert
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(0);
    }

    [Test]
    public async Task DashAddsCurrentSpeedAndMovementCanBeSplit()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.Move(hero, 10);

        // Act
        encounter.TakeAction(hero, CombatAction.Dash);
        encounter.Move(hero, 50);

        // Assert
        await Assert.That(encounter.MovementRemaining).IsEqualTo(0);
    }

    [Test]
    public async Task CannotMoveBeyondAllowance()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);

        // Act
        Action act = () => encounter.Move(hero, 31);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.MovementRemaining).IsEqualTo(30);
    }

    [Test]
    public async Task DisengageExpiresAtEndOfTurn()
    {
        // Arrange
        var hero = Hero();
        var other = Hero();
        var encounter = Fight(hero, other);
        encounter.TakeAction(hero, CombatAction.Disengage);
        var duringTurn = encounter.IsDisengaged(hero);

        // Act
        encounter.EndTurn(hero);

        // Assert
        await Assert.That(duringTurn).IsTrue();
        await Assert.That(encounter.IsDisengaged(hero)).IsFalse();
    }

    [Test]
    public async Task ReactionRefreshesAtStartOfNextOwnTurn()
    {
        // Arrange
        var first = Hero();
        var second = Hero();
        var encounter = Fight(first, second);
        encounter.UseReaction(second, eligibleTrigger: true);
        var spent = encounter.ReactionAvailable(second);

        // Act
        encounter.EndTurn(first);

        // Assert
        await Assert.That(spent).IsFalse();
        await Assert.That(encounter.ReactionAvailable(second)).IsTrue();
    }

    [Test]
    public async Task ReactionCannotBeUsedTwiceBeforeRefresh()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        encounter.UseReaction(hero, true);

        // Act
        Action act = () => encounter.UseReaction(hero, true);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ReactionAvailable(hero)).IsFalse();
    }

    [Test]
    public async Task ReactionNeedsEligibleTrigger()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);

        // Act
        Action act = () => encounter.UseReaction(hero, false);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ReactionAvailable(hero)).IsTrue();
    }

    [Test]
    public async Task BonusActionNeedsEligibilityAndCanBeUsedOnce()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);

        // Act
        await Assert.That(() => encounter.UseBonusAction(hero, false)).ThrowsExactly<InvalidOperationException>();
        encounter.UseBonusAction(hero, true);

        // Assert
        await Assert.That(encounter.BonusActionAvailable).IsFalse();
        await Assert.That(() => encounter.UseBonusAction(hero, true)).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task SurprisePreventsFirstTurnActionsMovementAndReactions()
    {
        // Arrange
        var hero = Hero();
        var other = Hero();
        var encounter = Fight(hero, other, surprised: true);

        // Act
        await Assert.That(() => encounter.TakeAction(hero, CombatAction.Dash)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => encounter.Move(hero, 1)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => encounter.UseReaction(hero, true)).ThrowsExactly<InvalidOperationException>();
        encounter.EndTurn(hero);

        // Assert
        await Assert.That(encounter.ReactionAvailable(hero)).IsTrue();
    }

    [Test]
    public async Task SurpriseDoesNotPreventLaterTurns()
    {
        // Arrange
        var hero = Hero();
        var other = Hero();
        var encounter = Fight(hero, other, surprised: true);
        encounter.EndTurn(hero);
        encounter.EndTurn(other);

        // Act
        encounter.TakeAction(hero, CombatAction.Dash);

        // Assert
        await Assert.That(encounter.MovementRemaining).IsEqualTo(60);
    }

    [Test]
    public async Task InvalidEncounterAttackDoesNotSpendAttackAllowance()
    {
        // Arrange
        var hero = Hero();
        var target = Hero();
        var encounter = Fight(hero);
        encounter.TakeAction(hero, CombatAction.Attack);
        var before = HealthState(target);

        // Act
        Action act = () => encounter.Attack(hero, target, null, new NormalDiceThrow(21), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(encounter.AttacksRemaining).IsEqualTo(1);
        await Assert.That(HealthState(target)).IsEqualTo(before);
    }

}



