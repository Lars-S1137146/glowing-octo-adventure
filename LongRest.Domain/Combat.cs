namespace LongRest.Domain;

public sealed record AttackContext(
    int Distance,
    bool AttackerCanSeeTarget,
    bool TargetCanSeeAttacker,
    bool HostileWithinFiveFeetCanSeeAttacker,
    Cover Cover = Cover.None,
    bool Thrown = false,
    bool TwoHands = false,
    bool? FearSourceVisible = null,
    int AdvantageSources = 0,
    int DisadvantageSources = 0,
    int Modifier = 0);

public sealed record AttackResult(bool Hit, bool Critical, int Total, int Die, bool? Melee = null);
public sealed record DamageDice(int Count, int Sides);

public static class Combat
{
    internal static int CoverBonus(Cover cover) => cover switch
    {
        Cover.Half => 2,
        Cover.ThreeQuarters => 5,
        _ => 0
    };

}

