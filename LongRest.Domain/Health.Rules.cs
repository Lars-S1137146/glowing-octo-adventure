namespace LongRest.Domain;

public sealed record Damage(
    int Amount,
    DamageType Type,
    int FlatModifier = 0,
    bool Critical = false,
    bool Nonlethal = false,
    bool Melee = false);

public sealed record DamageResult(
    int DamageTaken,
    int TemporaryAbsorbed,
    int HitPointsLost,
    int? ConcentrationSaveDc = null);

