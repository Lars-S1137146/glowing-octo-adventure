namespace LongRest.Domain;

public class Proficiencies
{
    private readonly Dictionary<Skill, Training> skills;
    private readonly HashSet<Ability> saves;
    private readonly HashSet<WeaponCategory> weapons;
    private readonly HashSet<ArmorCategory> armor;
    private readonly Dictionary<Equipment, Training> tools;
    private readonly HashSet<string> specificWeapons;

    public Proficiencies(IReadOnlyDictionary<Skill, Training>? skills = null,
        IEnumerable<Ability>? saves = null, IEnumerable<WeaponCategory>? weapons = null,
        IEnumerable<ArmorCategory>? armor = null, IReadOnlyDictionary<Equipment, Training>? tools = null,
        IEnumerable<string>? specificWeapons = null)
    {
        this.skills = skills?.ToDictionary() ?? [];
        this.saves = saves?.ToHashSet() ?? [];
        this.weapons = weapons?.ToHashSet() ?? [];
        this.armor = armor?.ToHashSet() ?? [];
        this.tools = tools?.ToDictionary() ?? [];
        this.specificWeapons = new(specificWeapons ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var (skill, training) in this.skills) { Validation.Defined(skill); Validation.Defined(training); }
        foreach (var save in this.saves) Validation.Defined(save);
        foreach (var weapon in this.weapons) Validation.Defined(weapon);
        foreach (var category in this.armor) Validation.Defined(category);
        foreach (var (tool, training) in this.tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            if (tool.Kind != EquipmentKind.Tool) throw new ArgumentException("Tool proficiency requires a tool.");
            Validation.Defined(training);
        }
    }

    public Training ForSkill(Skill skill) { Validation.Defined(skill); return skills.GetValueOrDefault(skill); }
    public bool ForSave(Ability ability) { Validation.Defined(ability); return saves.Contains(ability); }
    public bool ForWeapon(WeaponCategory category) { Validation.Defined(category); return weapons.Contains(category); }
    public bool ForWeapon(Equipment weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        return ForWeapon(weapon.WeaponCategory) || specificWeapons.Contains(weapon.Name);
    }
    public bool ForArmor(ArmorCategory category) { Validation.Defined(category); return armor.Contains(category); }
    public Training ForTool(Equipment tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        return tools.GetValueOrDefault(tool);
    }
}
