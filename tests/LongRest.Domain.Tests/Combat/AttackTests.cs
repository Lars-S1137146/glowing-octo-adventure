// PHB 9: attacks, cover, damage and critical hits. Project convention: computed damage has a minimum of zero.
namespace LongRest.Domain.Tests.Combat;

public class AttackTests
{
    [Test]
    public async Task ProficientLongswordUsesStrengthAndTraining()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16), proficiencies: new(weapons: [WeaponCategory.Martial]));
        var sword = Own(attacker, Sword());
        var target = Hero();

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(12), Nearby());

        // Assert
        await Assert.That(result.Total).IsEqualTo(17);
        await Assert.That(result.Hit).IsTrue();
    }

    [Test]
    public async Task UnproficientWeaponOmitsProficiency()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(12), Nearby());

        // Assert
        await Assert.That(result.Total).IsEqualTo(15);
    }

    [Test]
    [Arguments(1, false, false)]
    [Arguments(20, true, true)]
    public async Task NaturalOneAlwaysMissesAndTwentyAlwaysCrits(int die, bool hit, bool critical)
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 30));
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(die), Nearby());

        // Assert
        await Assert.That(result.Hit).IsEqualTo(hit);
        await Assert.That(result.Critical).IsEqualTo(critical);
    }

    [Test]
    public async Task SelectedAdvantageDieControlsCritical()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new AdvantageDiceThrow(1, 20), Nearby() with { AdvantageSources = 1 });

        // Assert
        await Assert.That(result.Die).IsEqualTo(20);
        await Assert.That(result.Critical).IsTrue();
    }

    [Test]
    public async Task AttackEqualToArmorClassHits()
    {
        // Arrange
        var attacker = Hero();
        var target = Hero();
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(10), Nearby());

        // Assert
        await Assert.That(result.Hit).IsTrue();
    }

    [Test]
    public async Task FinesseCanExplicitlyUseDexterity()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 8, dexterity: 18));
        var rapier = Own(attacker, new Equipment("Rapier", EquipmentKind.Weapon) { DamageSides = 8, Properties = WeaponProperties.Finesse });

        // Act
        var result = CombatRules.Attack(attacker, Hero(), rapier, new NormalDiceThrow(10), Nearby(), Ability.Dexterity);

        // Assert
        await Assert.That(result.Total).IsEqualTo(14);
    }

    [Test]
    public async Task RangedWeaponUsesDexterity()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 8, dexterity: 18));
        var bow = Own(attacker, Bow());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), bow, new NormalDiceThrow(10), Nearby(100));

        // Assert
        await Assert.That(result.Total).IsEqualTo(14);
    }

    [Test]
    public async Task ThrownMeleeWeaponUsesStrength()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16, dexterity: 8));
        var javelin = Own(attacker, new Equipment("Javelin", EquipmentKind.Weapon) { Properties = WeaponProperties.Thrown, ShortRange = 30, LongRange = 120 });

        // Act
        var result = CombatRules.Attack(attacker, Hero(), javelin, new NormalDiceThrow(10), Nearby(30) with { Thrown = true });

        // Assert
        await Assert.That(result.Total).IsEqualTo(13);
    }

    [Test]
    public async Task LongRangeImposesDisadvantage()
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), bow, new DisadvantageDiceThrow(4, 17), Nearby(151));

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    public async Task BeyondLongRangeIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), bow, new NormalDiceThrow(17), Nearby(601));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task BeyondMeleeReachIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(17), Nearby(10));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task HostileAdjacentCreaturePenalizesRangedAttack()
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), bow, new DisadvantageDiceThrow(4, 17), Nearby(100) with { HostileWithinFiveFeetCanSeeAttacker = true });

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    [Arguments(Cover.Half, 11, false)]
    [Arguments(Cover.Half, 12, true)]
    [Arguments(Cover.ThreeQuarters, 14, false)]
    [Arguments(Cover.ThreeQuarters, 15, true)]
    public async Task CoverAddsSingleArmorBonus(Cover cover, int die, bool hit)
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(die), Nearby(5, cover));

        // Assert
        await Assert.That(result.Hit).IsEqualTo(hit);
    }

    [Test]
    public async Task TotalCoverForbidsDirectAttack()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(20), Nearby(5, Cover.Total));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TwoHandedWeaponMayBeHeldWithShieldButCannotAttack()
    {
        // Arrange
        var attacker = Hero();
        var bow = Own(attacker, Bow());
        Own(attacker, Shield());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), bow, new NormalDiceThrow(17), Nearby(100));

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(attacker.Inventory.HeldWeapons.Contains(bow)).IsTrue();
    }

    [Test]
    public async Task UnseenTargetImposesDisadvantage()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new DisadvantageDiceThrow(4, 17), Nearby() with { AttackerCanSeeTarget = false });

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    public async Task ResolvingAttackDoesNotChangeHitPoints()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 16));
        var sword = Own(attacker, Sword());
        var target = Hero();
        var before = HealthState(target);

        // Act
        CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(20), Nearby());

        // Assert
        await Assert.That(HealthState(target)).IsEqualTo(before);
    }

    [Test]
    public async Task InvalidAttackDoesNotMutateEitherCharacter()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());
        var target = Hero();
        var attackerBefore = HealthState(attacker);
        var targetBefore = HealthState(target);

        // Act
        Action act = () => CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(21), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(HealthState(attacker)).IsEqualTo(attackerBefore);
        await Assert.That(HealthState(target)).IsEqualTo(targetBefore);
        await Assert.That(attacker.Inventory.Quantity(sword)).IsEqualTo(1);
    }

    [Test]
    public async Task InvalidAttackDiceCountIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new DisadvantageDiceThrow(10, 12), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task EquippedShieldChangesAttackOutcome()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());
        var target = Hero();
        Own(target, Armor());
        var shield = Own(target, Shield());
        var withShield = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(17), Nearby());

        // Act
        target.Inventory.Unequip(shield);
        var withoutShield = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(17), Nearby());

        // Assert
        await Assert.That(withShield.Hit).IsFalse();
        await Assert.That(withoutShield.Hit).IsTrue();
    }

}



