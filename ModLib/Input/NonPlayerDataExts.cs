using ImprovedInput;

namespace ModLib.Input;

/// <summary>
///     Extension methods for evaluating the input data of non-player objects.
/// </summary>
public static class NonPlayerDataExts
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public static bool IsKeyBound(this NonPlayerData self, PlayerKeybind key) => key.Bound(self.playerNumber);


    public static bool IsKeyUnbound(this NonPlayerData self, PlayerKeybind key) => !self.IsKeyBound(key);

    public static bool IsPressed(this NonPlayerData self, PlayerKeybind key) => self.Input()[key];

    public static bool JustPressed(this NonPlayerData self, PlayerKeybind key)
    {
        CustomInput[] array = self.InputHistory();
        return array[0][key] && !array[1][key];
    }

    public static CustomInput Input(this NonPlayerData self) => self.input[0];

    public static CustomInput RawInput(this NonPlayerData self) => self.rawInput[0];

    public static CustomInput[] InputHistory(this NonPlayerData self) => self.input;

    public static CustomInput[] RawInputHistory(this NonPlayerData self) => self.rawInput;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}