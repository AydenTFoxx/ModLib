using System;
using RainMeadow;

namespace ModLib.Objects.Meadow;

internal sealed class OnlineDeathProtection : Serializer.ICustomSerializable
{
    public OnlineCreature Target;

    public WorldCoordinate? SafePos;

    public byte SaveCooldown;
    public byte RevivalsLeft;

    public short Lifespan;

    public bool ForceRevive;

    public OnlineDeathProtection(DeathProtection.ProtectionSnapshot snapshot)
    {
        Target = snapshot.Target?.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException(snapshot.Target is null ? "Cannot serialize the protection of a null creature." : $"Could not retrieve the online representation of {snapshot.Target}.");
        SafePos = snapshot.SafePos;
        SaveCooldown = snapshot.SaveCooldown;
        RevivalsLeft = snapshot.RevivalsLeft;
        Lifespan = snapshot.Lifespan;
        ForceRevive = snapshot.ForceRevive;
    }

    public OnlineDeathProtection()
    {
        Target = null!;
    }

    public void CustomSerialize(Serializer serializer)
    {
        serializer.SerializeEntityById(ref Target);

        serializer.SerializeNullable(ref SafePos);

        serializer.Serialize(ref RevivalsLeft);

        serializer.Serialize(ref Lifespan);

        serializer.Serialize(ref ForceRevive);
    }

    public DeathProtection.ProtectionSnapshot ToSnapshot()
    {
        return new DeathProtection.ProtectionSnapshot()
        {
            Target = Target.realizedCreature,
            SafePos = SafePos,
            SaveCooldown = SaveCooldown,
            RevivalsLeft = RevivalsLeft,
            Lifespan = Lifespan,
            ForceRevive = ForceRevive
        };
    }

    public DeathProtection ToLocalProtection() => DeathProtection.FromSnapshot(ToSnapshot());
}