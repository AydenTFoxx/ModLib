using RainMeadow;

namespace ModLib.Objects.Meadow;

internal sealed class OnlineProtectionSnapshot : Serializer.ICustomSerializable
{
    public int Target;
    public WorldCoordinate? SafePos;
    public ushort Lifespan;
    public byte SaveCooldown;
    public byte SavingThrows;
    public byte RevivalsLeft;
    public bool IsPersistent;

    public OnlineProtectionSnapshot()
    {
    }

    public OnlineProtectionSnapshot(int target, WorldCoordinate? safePos, ushort lifespan, byte saveCooldown, byte savingThrows, byte revivalsLeft, bool isPersistent)
    {
        Target = target;
        SafePos = safePos;
        Lifespan = lifespan;
        SaveCooldown = saveCooldown;
        SavingThrows = savingThrows;
        RevivalsLeft = revivalsLeft;
        IsPersistent = isPersistent;
    }

    public OnlineProtectionSnapshot(DeathProtection protection)
        : this(protection.Target.abstractCreature.ID.number, protection.SafePos, protection.Lifespan, protection.SaveCooldown, protection.SavingThrows, protection.RevivalsLeft, protection.IsPersistent)
    {
    }

    public void CustomSerialize(Serializer serializer)
    {
        serializer.Serialize(ref Target);
        serializer.SerializeNullable(ref SafePos);

        serializer.Serialize(ref Lifespan);

        serializer.Serialize(ref SaveCooldown);
        serializer.Serialize(ref SavingThrows);

        serializer.Serialize(ref RevivalsLeft);
    }

    public static implicit operator DeathProtection.ProtectionSnapshot(OnlineProtectionSnapshot self)
    {
        return new(self.Target, self.SafePos, self.Lifespan, self.SaveCooldown, self.SavingThrows, self.RevivalsLeft, self.IsPersistent);
    }

    public static explicit operator OnlineProtectionSnapshot(in DeathProtection.ProtectionSnapshot self)
    {
        return new(self.Target, self.SafePos, self.Lifespan, self.SaveCooldown, self.SavingThrows, self.RevivalsLeft, self.IsPersistent);
    }
}