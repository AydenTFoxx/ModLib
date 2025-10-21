using System.Runtime.CompilerServices;
using ImprovedInput;

namespace ModLib.Input;

public static class ImprovedInputExts
{
    private static readonly ConditionalWeakTable<CustomInputHolder, CustomInputData> InputData = new();

    public static CustomInputData GetInputData(this CustomInputHolder self) =>
        InputData.GetOrCreateValue(self);

    public static bool TryGetInputData(this CustomInputHolder self, out CustomInputData inputData) =>
        InputData.TryGetValue(self, out inputData);

    public static bool IsKeyBound(this CustomInputHolder self, PlayerKeybind key) =>
        self.TryGetInputData(out CustomInputData inputData) && key.Bound(inputData.playerNumber);

    public static bool IsKeyUnbound(this CustomInputHolder self, PlayerKeybind key) =>
        !self.IsKeyBound(key);

    public static bool IsPressed(this CustomInputHolder self, PlayerKeybind key) =>
        self.Input()[key];

    public static bool JustPressed(this CustomInputHolder self, PlayerKeybind key)
    {
        CustomInput[] array = self.InputHistory();
        return array[0][key] && !array[1][key];
    }

    public static CustomInput Input(this CustomInputHolder self) =>
        InputData.GetOrCreateValue(self).input[0];

    public static CustomInput RawInput(this CustomInputHolder self) =>
        InputData.GetOrCreateValue(self).rawInput[0];

    public static CustomInput[] InputHistory(this CustomInputHolder self) =>
        InputData.GetOrCreateValue(self).input;

    public static CustomInput[] RawInputHistory(this CustomInputHolder self) =>
        InputData.GetOrCreateValue(self).rawInput;
}