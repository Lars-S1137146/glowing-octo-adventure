// PHB 9: attacks, cover, damage and critical hits. Project convention: computed damage has a minimum of zero.
namespace LongRest.Domain.Tests.Combat;

public class DamageRollTests
{
    [Test]
    public async Task LongswordAddsAbilityModifierOnce()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));
        var sword = Sword();

        // Act
        var damage = CombatRules.RollDamage(attacker, sword, new(true, false, 17, 12), [new DiceRoll(8, 5)]);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(8);
        await Assert.That(damage.Type).IsEqualTo(DamageType.Slashing);
    }

    [Test]
    public async Task CriticalDoublesDiceNotFlatModifiers()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));

        // Act
        var damage = CombatRules.RollDamage(attacker, Sword(), new(true, true, 25, 20), [new DiceRoll(8, 5, 6)], modifier: 2);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(16);
    }

    [Test]
    public async Task CriticalDoublesExplicitExtraDice()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));

        // Act
        var damage = CombatRules.RollDamage(attacker, Sword(), new(true, true, 25, 20), [new DiceRoll(8, 5, 6), new DiceRoll(6, 2, 3)], extraDice: [new(1, 6)]);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(19);
    }

    [Test]
    public async Task VersatileUsesLargerDieOnlyWithTwoHands()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));

        // Act
        var damage = CombatRules.RollDamage(attacker, Sword(), new(true, false, 17, 12), [new DiceRoll(10, 10)], twoHands: true);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(13);
    }

    [Test]
    public async Task CriticalRequiresDoubledDiceCount()
    {
        // Arrange
        var attacker = Hero();

        // Act
        Action act = () => CombatRules.RollDamage(attacker, Sword(), new(true, true, 20, 20), [new DiceRoll(8, 5)]);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task EveryDamageDieMustBeInRange()
    {
        // Arrange
        var attacker = Hero();

        // Act
        Action act = () => CombatRules.RollDamage(attacker, Sword(), new(true, true, 20, 20), [new DiceRoll(8, 5, 9)]);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task MissCannotProduceWeaponHitDamage()
    {
        // Arrange
        var attacker = Hero();

        // Act
        Action act = () => CombatRules.RollDamage(attacker, Sword(), new(false, false, 1, 1), [new DiceRoll(8, 5)]);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task UnarmedStrikeIsProficientAndNeedsNoDamageDice()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));

        // Act
        var attack = CombatRules.Attack(attacker, Hero(), null, new NormalDiceThrow(12), Nearby());
        var damage = CombatRules.RollDamage(attacker, null, attack, []);

        // Assert
        await Assert.That(attack.Total).IsEqualTo(17);
        await Assert.That(damage.Amount).IsEqualTo(4);
        await Assert.That(damage.Type).IsEqualTo(DamageType.Bludgeoning);
    }

    [Test]
    public async Task WeakUnarmedDamageHasProjectFloorOfZero()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 1));

        // Act
        var damage = CombatRules.RollDamage(attacker, null, new(true, false, 10, 15), []);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(0);
    }

    [Test]
    public async Task AttackAndDamageAreSeparateOperations()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));
        var target = Hero();
        var sword = Own(attacker, Sword());
        var hit = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(12), Nearby());
        var before = target.Health.Value;
        var damage = CombatRules.RollDamage(attacker, sword, hit, [new DiceRoll(8, 5)]);

        // Act
        target.TakeDamage(damage);

        // Assert
        await Assert.That(before).IsEqualTo(20);
        await Assert.That(target.Health.Value).IsEqualTo(12);
    }

    [Test]
    public async Task CriticalThenMassiveDamageKills()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));
        var target = Hero(hp: 6);
        var sword = Own(attacker, Sword());
        var hit = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(20), Nearby());

        // Act
        target.TakeDamage(CombatRules.RollDamage(attacker, sword, hit, [new DiceRoll(8, 5, 6)]));

        // Assert
        await Assert.That(target.Health.IsDead).IsTrue();
    }

}



