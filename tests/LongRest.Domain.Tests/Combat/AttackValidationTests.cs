// PHB 9: attacks, cover, damage and critical hits. Project convention: computed damage has a minimum of zero.
namespace LongRest.Domain.Tests.Combat;

public class AttackValidationTests
{
    [Test]
    public async Task NaturalTwentyHitsEvenAgainstHighArmorClass()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 1));
        var sword = Own(attacker, Sword());
        var target = new Character("Configured target", new Stats(), level: null, proficiencyBonus: 2, armorClass: 40);

        // Act
        var result = CombatRules.Attack(attacker, target, sword, new NormalDiceThrow(20), Nearby());

        // Assert
        await Assert.That(result.Hit).IsTrue();
        await Assert.That(result.Critical).IsTrue();
    }

    [Test]
    public async Task WeaponMustBeOwnedAndEquipped()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword(), equip: false);

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(17), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task InvalidFinesseAbilityIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var rapier = Own(attacker, new Equipment("Rapier", EquipmentKind.Weapon) { Properties = WeaponProperties.Finesse });

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), rapier, new NormalDiceThrow(17), Nearby(), Ability.Wisdom);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task OrdinaryMeleeWeaponCannotSelectDexterity()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(17), Nearby(), Ability.Dexterity);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task InvalidWeaponDamageConfigurationIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var weapon = Own(attacker, new Equipment("Broken definition", EquipmentKind.Weapon) { DamageSides = 0 });

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), weapon, new NormalDiceThrow(17), Nearby());

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FinesseDamageUsesSameSelectedAbility()
    {
        // Arrange
        var attacker = Hero(new Stats(strength: 8, dexterity: 18));
        var rapier = new Equipment("Rapier", EquipmentKind.Weapon) { Properties = WeaponProperties.Finesse, DamageSides = 8 };

        // Act
        var damage = CombatRules.RollDamage(attacker, rapier, new(true, false, 14, 10), [new DiceRoll(8, 5)], finesseAbility: Ability.Dexterity);

        // Assert
        await Assert.That(damage.Amount).IsEqualTo(9);
    }

    [Test]
    public async Task UnproficientArmorPenalizesWeaponAttacks()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());
        Own(attacker, Armor());

        // Act
        var result = CombatRules.Attack(attacker, Hero(), sword, new DisadvantageDiceThrow(4, 17), Nearby());

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

    [Test]
    public async Task MissingBattlefieldContextIsRejected()
    {
        // Arrange
        var attacker = Hero();
        var sword = Own(attacker, Sword());

        // Act
        Action act = () => CombatRules.Attack(attacker, Hero(), sword, new NormalDiceThrow(17), null!);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
    }

}



