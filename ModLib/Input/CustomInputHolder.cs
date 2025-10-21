using System.Runtime.CompilerServices;

namespace ModLib.Input;

public class CustomInputHolder
{
    private static readonly ConditionalWeakTable<UpdatableAndDeletable, CustomInputHolder> Instances = new();

    public static CustomInputHolder GetInputHolder(UpdatableAndDeletable self) => Instances.GetOrCreateValue(self);

    public static bool TryGetInputHolder(UpdatableAndDeletable self, out CustomInputHolder inputHolder) => Instances.TryGetValue(self, out inputHolder);

    public static explicit operator CustomInputHolder(UpdatableAndDeletable self)
    {
        return GetInputHolder(self);
    }
}