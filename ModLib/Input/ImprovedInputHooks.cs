using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHooks
{
    public static void ApplyHooks() => On.UpdatableAndDeletable.Update += UpdateInput;
    public static void RemoveHooks() => On.UpdatableAndDeletable.Update -= UpdateInput;

    private static void UpdateInput(On.UpdatableAndDeletable.orig_Update orig, UpdatableAndDeletable self, bool eu)
    {
        if (self is Player || !CustomInputHolder.TryGetInputHolder(self, out CustomInputHolder inputHolder)) return;

        CustomInputData data = inputHolder.GetInputData();

        for (int num = data.input.Length - 1; num > 0; num--)
        {
            data.input[num] = data.input[num - 1];
        }

        for (int num2 = data.rawInput.Length - 1; num2 > 0; num2--)
        {
            data.rawInput[num2] = data.rawInput[num2 - 1];
        }

        int num3 = ModManager.MSC && self.room?.world.game.GetArenaGameSession?.chMeta is not null
            ? 0
            : data.playerNumber;

        if (num3 < 0 || num3 >= CustomInputExt.MaxPlayers)
        {
            orig.Invoke(self, eu);
            return;
        }

        data.rawInput[0] = CustomInput.GetRawInput(num3);
        data.input[0] = self is not Creature creature || (!creature.dead && creature.stun == 0)
            ? data.rawInput[0].Clone()
            : new CustomInput();

        orig.Invoke(self, eu);
    }
}