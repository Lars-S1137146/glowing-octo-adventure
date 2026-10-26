namespace LongRest.Domain.Tests.Persistence;

public class RestorationValidationTests
{
    [Test]
    public async Task InvalidHealthRestorationDoesNotChangeExistingState()
    {
        var hero = Hero();
        hero.TakeDamage(new(2, DamageType.Fire));
        await Assert.That(() => hero.Health.Restore(10, 20, 0, 1, 0, false, false)).ThrowsExactly<ArgumentException>();
        await Assert.That(hero.Health.Value).IsEqualTo(18);
        await Assert.That(hero.Health.DeathSuccesses).IsEqualTo(0);
        await Assert.That(() => hero.Health.Restore(0, 20, 0, 0, 3, false, false)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.Health.Value).IsEqualTo(18);
    }

    [Test]
    public async Task InvalidSpellResourcesDoNotChangeSlotsOrConcentration()
    {
        var spell = new Spell("Bless", 1, concentration: true);
        var magic = Magic([spell]);
        var hero = Hero(spells: magic);
        magic.Cast(hero, spell, 1);
        hero.TakeDamage(new(2, DamageType.Fire));
        await Assert.That(() => magic.RestoreResources(new Dictionary<int, int> { [1] = 3, [3] = 1 }, null, []))
            .ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => magic.RestoreResources(new Dictionary<int, int> { [1] = 1, [3] = 1 }, null, [10]))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(magic.RemainingSlots(1)).IsEqualTo(1);
        await Assert.That(magic.ConcentratingOn).IsEqualTo(spell);
        await Assert.That(magic.PendingConcentrationSaves.Single()).IsEqualTo(10);
    }
}
