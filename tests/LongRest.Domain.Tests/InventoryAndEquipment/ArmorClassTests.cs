// PHB 5: armor, shields and weapons. Project contract: unequip before removing the final owned item.
namespace LongRest.Domain.Tests.InventoryAndEquipment;

public class ArmorClassTests
{
    [Test]
    public async Task UnarmoredArmorClassIncludesDexterity()
    {
        // Arrange
        var hero = Hero(new Stats(dexterity: 16));

        // Act
        var ac = hero.ArmorClass;

        // Assert
        await Assert.That(ac).IsEqualTo(13);
    }

    [Test]
    [Arguments(ArmorCategory.Light, 11, 16, 14)]
    [Arguments(ArmorCategory.Medium, 14, 18, 16)]
    [Arguments(ArmorCategory.Medium, 14, 8, 13)]
    [Arguments(ArmorCategory.Heavy, 18, 20, 18)]
    public async Task ArmorUsesItsCategoryFormula(ArmorCategory category, int baseAc, int dexterity, int expected)
    {
        // Arrange
        var hero = Hero(new Stats(dexterity: dexterity));

        // Act
        Own(hero, Armor(category, baseAc));

        // Assert
        await Assert.That(hero.ArmorClass).IsEqualTo(expected);
    }

    [Test]
    public async Task ShieldAddsTwoAndUnequippingUpdatesArmorClass()
    {
        // Arrange
        var hero = Hero();
        Own(hero, Armor());
        var shield = Own(hero, Shield());
        var equippedAc = hero.ArmorClass;

        // Act
        hero.Inventory.Unequip(shield);

        // Assert
        await Assert.That(equippedAc).IsEqualTo(18);
        await Assert.That(hero.ArmorClass).IsEqualTo(16);
    }

    [Test]
    public async Task InsufficientStrengthReducesSpeedByTen()
    {
        // Arrange
        var hero = Hero();

        // Act
        Own(hero, new Equipment("Chain mail", EquipmentKind.Armor) { ArmorCategory = ArmorCategory.Heavy, BaseArmorClass = 16, StrengthRequirement = 13 });

        // Assert
        await Assert.That(hero.CurrentSpeed).IsEqualTo(20);
    }

    [Test]
    public async Task UnproficientArmorCanBeWornButPreventsCasting()
    {
        // Arrange
        var hero = Hero();

        // Act
        Own(hero, Armor());

        // Assert
        await Assert.That(hero.Inventory.Armor).IsNotNull();
        await Assert.That(hero.CanCast).IsFalse();
    }

    [Test]
    public async Task ArmorTrainingPermitsCasting()
    {
        // Arrange
        var hero = Hero(proficiencies: new(armor: [ArmorCategory.Heavy]));

        // Act
        Own(hero, Armor());

        // Assert
        await Assert.That(hero.CanCast).IsTrue();
    }

    [Test]
    [Arguments(Ability.Strength)]
    [Arguments(Ability.Dexterity)]
    public async Task UnproficientArmorPenalizesStrengthAndDexterityChecksAndSaves(Ability ability)
    {
        // Arrange
        var hero = Hero();
        Own(hero, Armor());

        // Act
        var check = hero.Check(ability, new DisadvantageDiceThrow(4, 17), 10);
        var save = hero.Save(ability, new DisadvantageDiceThrow(4, 17), 10);

        // Assert
        await Assert.That(check.Die).IsEqualTo(4);
        await Assert.That(save.Die).IsEqualTo(4);
    }

    [Test]
    public async Task ArmorStealthPenaltyAppliesEvenWhenProficient()
    {
        // Arrange
        var hero = Hero(proficiencies: new(armor: [ArmorCategory.Heavy]));
        Own(hero, new Equipment("Chain mail", EquipmentKind.Armor) { ArmorCategory = ArmorCategory.Heavy, BaseArmorClass = 16, StealthDisadvantage = true });

        // Act
        var result = hero.Check(Ability.Dexterity, new DisadvantageDiceThrow(4, 17), 10, Skill.Stealth);

        // Assert
        await Assert.That(result.Die).IsEqualTo(4);
    }

}



