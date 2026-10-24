// PHB 8: resting. Project convention: the caller declares duration requirements satisfied; Hit Dice are configured directly.
namespace LongRest.Domain.Tests.Resting;

public class RecoveryScenarioTests
{
    [Test]
    public async Task DamageShortRestThenLongRestRestoresResources()
    {
        // Arrange
        var spell = new Spell("Magic missile", 1);
        var magic = Magic([spell]);
        var hero = Hero(new Stats(constitution: 14), hp: 40, hitDice: new(4, 10), spells: magic);
        magic.Cast(hero, spell, 1);
        hero.TakeDamage(new(30, DamageType.Slashing));
        hero.ShortRest(new DiceRoll(hero.HitDice.Sides, 6, 3));
        var afterShortRest = hero.Health.Value;
        hero.Health.OfferTemporary(8, true);

        // Act
        hero.LongRest();

        // Assert
        await Assert.That(afterShortRest).IsEqualTo(23);
        await Assert.That(hero.Health.Value).IsEqualTo(40);
        await Assert.That(hero.HitDice.Spent).IsEqualTo(0);
        await Assert.That(hero.Health.TempHealth).IsEqualTo(0);
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(2);
    }

}



