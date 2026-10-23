namespace LongRest.Domain;

public sealed class Health
{
    public int Value { get; private set; }
    public int TempHealth { get; private set; }
    public int Max { get; private set; }
    public bool IsDowned => Value == 0 && !IsDead;
    public bool IsDead { get; private set; }
    public bool IsUnconscious => Value == 0;
    public int DeathSuccesses { get; private set; }
    public int DeathFailures { get; private set; }
    public bool IsStable { get; private set; }

    // Character observes zero HP even when setup or a death save changes Health directly.
    internal event Action? Changed;

    public Health(int current = 10, int maximum = 10, int temporary = 0)
    {
        Set(current, maximum, temporary);
    }

    public void Set(int current, int maximum, int temporary)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        Validation.Range(current, 0, maximum, nameof(current));
        ArgumentOutOfRangeException.ThrowIfNegative(temporary);
        Value = current;
        Max = maximum;
        TempHealth = temporary;
        IsDead = false;
        ResetDeathSaves();
        Changed?.Invoke();
    }

    public void Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (IsDead) throw new InvalidOperationException("Ordinary healing cannot revive the dead.");
        if (amount == 0) return;
        Value = (int)Math.Min(Max, (long)Value + amount);
        ResetDeathSaves();
        Changed?.Invoke();
    }

    internal void ChangeMaximum(int difference)
    {
        var newMaximum = (int)Math.Clamp((long)Max + difference, 1, int.MaxValue);
        var adjustment = newMaximum - Max;
        Max = newMaximum;
        if (Value > 0) Value = Math.Clamp(Value + adjustment, 0, Max);
        Changed?.Invoke();
    }

    internal void ApplyDamage(int amount, bool critical, bool nonlethal)
    {
        if (amount == 0 || IsDead) return;
        var absorbed = Math.Min(amount, TempHealth);
        TempHealth -= absorbed;
        var remaining = amount - absorbed;
        if (remaining == 0)
        {
            if (Value == 0) FailDeathSave(critical);
            return;
        }
        var previous = Value;
        Value = Math.Max(0, Value - remaining);
        if (Value == 0 && nonlethal && previous > 0)
        {
            Stabilize();
        }
        else if (remaining - previous >= Max)
        {
            MarkDead();
        }
        else if (previous == 0)
        {
            FailDeathSave(critical);
        }
        Changed?.Invoke();
    }

    private void FailDeathSave(bool critical)
    {
        IsStable = false;
        DeathFailures = Math.Min(3, DeathFailures + (critical ? 2 : 1));
        if (DeathFailures == 3) MarkDead();
    }

    public void OfferTemporary(int amount, bool accept)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (accept) TempHealth = amount;
    }

    public void MarkDead()
    {
        IsDead = true;
        Value = 0;
        IsStable = false;
        Changed?.Invoke();
    }

    public void DeathSave(DiceThrow roll)
    {
        ArgumentNullException.ThrowIfNull(roll);
        roll.Require(ThrowType.Normal);
        if (!IsDowned || IsStable) throw new InvalidOperationException("Death saves require an unstable creature at zero HP.");
        if (roll.Value == 20) { Heal(1); return; }
        if (roll.Value >= 10) DeathSuccesses++;
        else DeathFailures = Math.Min(3, DeathFailures + (roll.Value == 1 ? 2 : 1));
        if (DeathFailures >= 3) MarkDead();
        else if (DeathSuccesses >= 3) Stabilize();
    }

    public void Stabilize()
    {
        if (!IsDowned) throw new InvalidOperationException("Only a living creature at zero HP can be stabilized.");
        ResetDeathSaves();
        IsStable = true;
    }

    public void RecoverFromStable()
    {
        if (!IsStable || !IsDowned) throw new InvalidOperationException("Stable recovery requires a stable creature at zero HP.");
        Heal(1);
    }

    private void ResetDeathSaves()
    {
        DeathSuccesses = 0;
        DeathFailures = 0;
        IsStable = false;
    }
}
