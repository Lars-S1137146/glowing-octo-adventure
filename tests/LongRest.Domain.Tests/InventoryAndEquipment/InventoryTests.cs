// PHB 5: armor, shields and weapons. Project contract: unequip before removing the final owned item.
namespace LongRest.Domain.Tests.InventoryAndEquipment;

public class InventoryTests
{
    [Test]
    public async Task AddingAndRemovingTracksOwnedQuantity()
    {
        // Arrange
        var hero = Hero();
        var rope = new Equipment("Rope");
        hero.Inventory.Add(rope, 3);

        // Act
        hero.Inventory.Remove(rope, 2);

        // Assert
        await Assert.That(hero.Inventory.Quantity(rope)).IsEqualTo(1);
    }

    [Test]
    public async Task SameNameDoesNotMergeDistinctDefinitions()
    {
        // Arrange
        var hero = Hero();
        var first = Sword();
        var second = Sword();

        // Act
        hero.Inventory.Add(first, 2);
        hero.Inventory.Add(second);

        // Assert
        await Assert.That(hero.Inventory.Quantity(first)).IsEqualTo(2);
        await Assert.That(hero.Inventory.Quantity(second)).IsEqualTo(1);
    }

    [Test]
    public async Task CannotRemoveMoreThanOwned()
    {
        // Arrange
        var hero = Hero();
        var sword = Own(hero, Sword());

        // Act
        Action act = () => hero.Inventory.Remove(sword, 2);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Inventory.Quantity(sword)).IsEqualTo(1);
        await Assert.That(hero.Inventory.HeldWeapons.Contains(sword)).IsTrue();
    }

    [Test]
    public async Task LastEquippedInstanceCannotBeRemoved()
    {
        // Arrange
        var hero = Hero();
        var shield = Own(hero, Shield());
        var ac = hero.ArmorClass;

        // Act
        Action act = () => hero.Inventory.Remove(shield);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Inventory.Quantity(shield)).IsEqualTo(1);
        await Assert.That(hero.Inventory.Shield).IsEqualTo(shield);
        await Assert.That(hero.ArmorClass).IsEqualTo(ac);
    }

    [Test]
    public async Task UnequippingAllowsRemovingFinalInstance()
    {
        // Arrange
        var hero = Hero();
        var sword = Own(hero, Sword());

        // Act
        hero.Inventory.Unequip(sword);
        hero.Inventory.Remove(sword);

        // Assert
        await Assert.That(hero.Inventory.Quantity(sword)).IsEqualTo(0);
        await Assert.That(hero.Inventory.HeldWeapons.Contains(sword)).IsFalse();
    }

    [Test]
    public async Task CannotEquipUnownedItemAndArmorClassStaysUnchanged()
    {
        // Arrange
        var hero = Hero();
        var before = hero.ArmorClass;

        // Act
        Action act = () => hero.Inventory.Equip(Shield());

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.ArmorClass).IsEqualTo(before);
        await Assert.That(hero.Inventory.Shield).IsNull();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task QuantitiesMustBePositive(int quantity)
    {
        // Arrange
        var hero = Hero();
        var item = new Equipment("Rope");

        // Act
        Action act = () => hero.Inventory.Add(item, quantity);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(hero.Inventory.Quantity(item)).IsEqualTo(0);
    }

    [Test]
    public async Task NullItemIsRejected()
    {
        // Arrange
        var hero = Hero();

        // Act
        Action act = () => hero.Inventory.Add(null!);

        // Assert
        await Assert.That(act).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OrdinaryItemCannotBeEquipped()
    {
        // Arrange
        var hero = Hero();
        var rope = Own(hero, new Equipment("Rope"), equip: false);

        // Act
        Action act = () => hero.Inventory.Equip(rope);

        // Assert
        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task SecondShieldCannotGiveSecondArmorBonus()
    {
        // Arrange
        var hero = Hero();
        Own(hero, Shield());
        var second = Own(hero, Shield(), equip: false);

        // Act
        // Either replacing or rejecting the second shield is legal; neither may stack.
        try { hero.Inventory.Equip(second); } catch (InvalidOperationException) { }

        // Assert
        await Assert.That(hero.ArmorClass).IsEqualTo(12);
    }

}



