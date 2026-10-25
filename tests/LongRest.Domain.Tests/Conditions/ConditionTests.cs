// PHB Appendix A: conditions. Exhaustion is excluded; battlefield facts come from the caller.
namespace LongRest.Domain.Tests.Conditions;

public class ConditionTests
{
    [Test]
    [Arguments(Condition.Blinded)]
    [Arguments(Condition.Charmed)]
    [Arguments(Condition.Deafened)]
    [Arguments(Condition.Frightened)]
    [Arguments(Condition.Grappled)]
    [Arguments(Condition.Incapacitated)]
    [Arguments(Condition.Invisible)]
    [Arguments(Condition.Paralyzed)]
    [Arguments(Condition.Petrified)]
    [Arguments(Condition.Poisoned)]
    [Arguments(Condition.Prone)]
    [Arguments(Condition.Restrained)]
    [Arguments(Condition.Stunned)]
    [Arguments(Condition.Unconscious)]
    public async Task NamedConditionCanBeAddedAndRemoved(Condition condition)
    {
        // Arrange
        var hero = Hero();
        var source = Hero();

        // Act
        hero.AddCondition(condition, source);
        var present = hero.ActiveConditions.Contains(condition);
        hero.RemoveCondition(condition);

        // Assert
        await Assert.That(present).IsTrue();
        await Assert.That(hero.ActiveConditions.Contains(condition)).IsFalse();
    }

    [Test]
    public async Task RepeatedConditionHasOneEntry()
    {
        // Arrange
        var hero = Hero();

        // Act
        hero.AddCondition(Condition.Poisoned);
        hero.AddCondition(Condition.Poisoned);

        // Assert
        await Assert.That(hero.ActiveConditions.Count(x => x == Condition.Poisoned)).IsEqualTo(1);
    }

    [Test]
    public async Task InvalidConditionDoesNotChangeExistingConditions()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Poisoned);

        // Act
        Action act = () => hero.AddCondition((Condition)999);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.ActiveConditions.SequenceEqual(new[] { Condition.Poisoned })).IsTrue();
    }

    [Test]
    [Arguments(Condition.Charmed)]
    [Arguments(Condition.Frightened)]
    public async Task SourceDependentConditionsRequireSource(Condition condition)
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => hero.AddCondition(condition);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
        await Assert.That(hero.ActiveConditions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BlindedSightCheckAutomaticallyFailsWithoutDie()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Blinded);

        // Act
        var result = hero.Check(Ability.Wisdom, null, 10, Skill.Perception, new(RequiresSight: true));

        // Assert
        await Assert.That(result.AutomaticFailure).IsTrue();
        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task DeafenedHearingCheckAutomaticallyFailsWithoutDie()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Deafened);

        // Act
        var result = hero.Check(Ability.Wisdom, null, 10, Skill.Perception, new(RequiresHearing: true));

        // Assert
        await Assert.That(result.AutomaticFailure).IsTrue();
        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task PoisonedPenalizesChecksButNotOrdinarySaves()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Poisoned);

        // Act
        var check = hero.Check(Ability.Wisdom, new DisadvantageDiceThrow(4, 17), 10);
        var save = hero.Save(Ability.Wisdom, new NormalDiceThrow(17), 10);

        // Assert
        await Assert.That(check.Die).IsEqualTo(4);
        await Assert.That(save.Die).IsEqualTo(17);
    }

    [Test]
    [Arguments(Condition.Grappled)]
    [Arguments(Condition.Restrained)]
    public async Task GrappledAndRestrainedSetSpeedToZero(Condition condition)
    {
        // Arrange
        var hero = Hero();

        // Act
        hero.AddCondition(condition);

        // Assert
        await Assert.That(hero.CurrentSpeed).IsEqualTo(0);
    }

    [Test]
    [Arguments(Condition.Incapacitated, true)]
    [Arguments(Condition.Paralyzed, false)]
    [Arguments(Condition.Petrified, false)]
    [Arguments(Condition.Stunned, true)]
    [Arguments(Condition.Unconscious, false)]
    public async Task IncapacitatingConditionsBlockActionsAndSpeechWhereApplicable(Condition condition, bool canSpeak)
    {
        // Arrange
        var hero = Hero();

        // Act
        hero.AddCondition(condition);

        // Assert
        await Assert.That(hero.CanAct).IsFalse();
        await Assert.That(hero.CanCast).IsFalse();
        await Assert.That(hero.CanSpeak).IsEqualTo(canSpeak);
    }

    [Test]
    [Arguments(Condition.Paralyzed)]
    [Arguments(Condition.Petrified)]
    [Arguments(Condition.Stunned)]
    [Arguments(Condition.Unconscious)]
    public async Task ParalyzedPetrifiedStunnedAndUnconsciousAutoFailPhysicalSaves(Condition condition)
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(condition);

        // Act
        var strength = hero.Save(Ability.Strength, null, 5);
        var dexterity = hero.Save(Ability.Dexterity, null, 5);

        // Assert
        await Assert.That(strength.AutomaticFailure).IsTrue();
        await Assert.That(dexterity.AutomaticFailure).IsTrue();
    }

    [Test]
    public async Task RestrainedPenalizesDexteritySaves()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Restrained);

        // Act
        var result = hero.Save(Ability.Dexterity, new DisadvantageDiceThrow(4, 17), 10);

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    public async Task PetrifiedResistsDamageAndIsImmuneToPoison()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Petrified);

        // Act
        hero.TakeDamage(new(11, DamageType.Fire));
        hero.TakeDamage(new(40, DamageType.Poison));

        // Assert
        await Assert.That(hero.Health.Value).IsEqualTo(15);
    }

    [Test]
    public async Task FrightenedCheckDependsOnSourceVisibility()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Frightened, Hero());

        // Act
        var hidden = hero.Check(Ability.Wisdom, new NormalDiceThrow(17), 10, context: new(FearSourceVisible: false));
        var visible = hero.Check(Ability.Wisdom, new DisadvantageDiceThrow(4, 17), 10, context: new(FearSourceVisible: true));

        // Assert
        await Assert.That(hidden.Die).IsEqualTo(17);
        await Assert.That(visible.Die).IsEqualTo(4);
    }

    [Test]
    public async Task FrightenedNeedsVisibilityContext()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Frightened, Hero());

        // Act
        Action act = () => hero.Check(Ability.Wisdom, new NormalDiceThrow(17), 10);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task CharmSourceHasAdvantageOnSocialCheck()
    {
        // Arrange
        var source = Hero();
        var charmed = Hero();
        charmed.AddCondition(Condition.Charmed, source);

        // Act
        var result = source.Check(Ability.Charisma, new AdvantageDiceThrow(4, 17), 10, Skill.Persuasion, new(SocialTarget: charmed));

        // Assert
        await Assert.That(result.Die).IsEqualTo(17);
    }

    [Test]
    public async Task IncapacitatedCannotUseActionOrReaction()
    {
        // Arrange
        var hero = Hero();
        var encounter = Fight(hero);
        hero.AddCondition(Condition.Incapacitated);

        // Act
        Action action = () => encounter.TakeAction(hero, CombatAction.Dash);
        Action reaction = () => encounter.UseReaction(hero, true);

        // Assert
        await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        await Assert.That(reaction).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.ActionsRemaining).IsEqualTo(1);
    }

    [Test]
    public async Task FrightenedCannotWillinglyMoveCloserToSource()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Frightened, Hero());
        var encounter = Fight(hero);

        // Act
        Action act = () => encounter.Move(hero, 5, movesCloserToFearSource: true);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(encounter.MovementRemaining).IsEqualTo(30);
    }

    [Test]
    public async Task StandingUsesHalfCurrentSpeed()
    {
        // Arrange
        var hero = Hero();
        hero.AddCondition(Condition.Prone);
        var encounter = Fight(hero);
        encounter.Move(hero, 5);

        // Act
        encounter.StandUp(hero);

        // Assert
        await Assert.That(encounter.MovementRemaining).IsEqualTo(5);
        await Assert.That(hero.ActiveConditions.Contains(Condition.Prone)).IsFalse();
    }

    [Test]
    public async Task HpUnconsciousnessDropsHeldItemsAndLeavesProneAfterHealing()
    {
        // Arrange
        var hero = Hero();
        var sword = Own(hero, Sword());

        // Act
        hero.TakeDamage(new(20, DamageType.Fire));
        var heldAfterDropping = hero.Inventory.HeldWeapons.Contains(sword);
        hero.Heal(1);

        // Assert
        await Assert.That(heldAfterDropping).IsFalse();
        await Assert.That(hero.Health.IsUnconscious).IsFalse();
        await Assert.That(hero.ActiveConditions.Contains(Condition.Prone)).IsTrue();
    }

}



