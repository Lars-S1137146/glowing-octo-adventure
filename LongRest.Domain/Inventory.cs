namespace LongRest.Domain;

public sealed class Inventory
{
    private readonly Dictionary<Equipment, int> items = [];
    private readonly List<Equipment> heldWeapons = [];
    public IReadOnlyDictionary<Equipment, int> Items => new System.Collections.ObjectModel.ReadOnlyDictionary<Equipment, int>(items);
    public Equipment? Armor { get; private set; }
    public Equipment? Shield { get; private set; }
    public IReadOnlyList<Equipment> HeldWeapons => heldWeapons.AsReadOnly();

    public int Quantity(Equipment item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return items.GetValueOrDefault(item);
    }

    public void Add(Equipment item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        items[item] = checked(Quantity(item) + quantity);
    }

    public void Remove(Equipment item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var remaining = Quantity(item) - quantity;
        if (remaining < 0) throw new InvalidOperationException("Cannot remove more than owned.");
        if (remaining == 0 && (Armor == item || Shield == item || heldWeapons.Contains(item)))
            throw new InvalidOperationException("Unequip the item before removing its last instance.");
        if (remaining == 0) items.Remove(item);
        else items[item] = remaining;
    }

    public void Equip(Equipment item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Quantity(item) == 0) throw new InvalidOperationException("You do not own this item.");
        switch (item.Kind)
        {
            case EquipmentKind.Armor:
                Validation.Defined(item.ArmorCategory);
                if (item.ArmorCategory == ArmorCategory.Shield) throw new ArgumentException("Armor cannot use the shield category.");
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.BaseArmorClass);
                ArgumentOutOfRangeException.ThrowIfNegative(item.StrengthRequirement);
                Armor = item;
                break;
            case EquipmentKind.Shield:
                if (heldWeapons.Count > 1) throw new InvalidOperationException("A shield requires a free hand.");
                Shield = item;
                break;
            case EquipmentKind.Weapon:
                if (!heldWeapons.Contains(item))
                {
                    if (heldWeapons.Count >= (Shield is null ? 2 : 1)) throw new InvalidOperationException("No free hand for another weapon.");
                    heldWeapons.Add(item);
                }
                break;
            default:
                throw new InvalidOperationException("Only armor, shields and weapons can be equipped.");
        }
    }

    public void Unequip(Equipment item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Armor == item) Armor = null;
        if (Shield == item) Shield = null;
        heldWeapons.Remove(item);
    }

    internal void DropHeldItems()
    {
        heldWeapons.Clear();
        Shield = null;
    }
}
