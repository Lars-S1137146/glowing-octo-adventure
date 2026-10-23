namespace LongRest.Domain;

public sealed class Background
{
    public string Name { get; init; } = "";
    public IReadOnlyList<Skill> Skills { get; init; } = [];
}
