using ImprovedInput;

namespace ModLib.Input;

/// <summary>
///     Extension methods for evaluating the input data of non-player objects.
/// </summary>
public static class CustomInputDataExts
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public static bool IsKeyBound(this CustomInputData self, PlayerKeybind key) => key.Bound(self.playerNumber);


    public static bool IsKeyUnbound(this CustomInputData self, PlayerKeybind key) => !self.IsKeyBound(key);

    public static bool IsPressed(this CustomInputData self, PlayerKeybind key) => self.Input()[key];

    public static bool JustPressed(this CustomInputData self, PlayerKeybind key)
    {
        CustomInput[] array = self.InputHistory();
        return array[0][key] && !array[1][key];
    }

    public static CustomInput Input(this CustomInputData self) => self.input[0];

    public static CustomInput RawInput(this CustomInputData self) => self.rawInput[0];

    public static CustomInput[] InputHistory(this CustomInputData self) => self.input;

    public static CustomInput[] RawInputHistory(this CustomInputData self) => self.rawInput;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}