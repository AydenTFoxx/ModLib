using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    private static readonly CustomInputData?[] InputData = new CustomInputData[CustomInputExt.MaxPlayers];

    public static CustomInputData? GetInputListener(int playerNumber) => InputData[playerNumber];

    public static void SetInputListener(int playerNumber, bool enable) =>
        InputData[playerNumber] = enable ? new CustomInputData(playerNumber) : null;

    public static bool IsKeyDown(Player player, Keybind keybind, bool rawInput) =>
        rawInput
            ? player.RawInput()[keybind]
            : player.IsPressed(keybind);

    public static bool IsKeyDown(int playerNumber, Keybind keybind, bool rawInput)
    {
        CustomInputData? data = InputData[playerNumber];

        if (data is null) return false;

        CustomInput input = rawInput ? data.RawInput() : data.Input();

        return input[keybind];
    }

    public static bool WasKeyJustPressed(Player player, Keybind keybind, bool rawInput)
    {
        if (rawInput)
        {
            CustomInput[] history = player.RawInputHistory();

            return history[0][keybind] && !history[1][keybind];
        }

        return player.JustPressed(keybind);
    }

    public static bool WasKeyJustPressed(int playerNumber, Keybind keybind, bool rawInput)
    {
        CustomInputData? data = InputData[playerNumber];

        if (data is null) return false;

        CustomInput[] history = rawInput ? data.RawInputHistory() : data.InputHistory();

        return history[0][keybind] && !history[1][keybind];
    }

    public static void RegisterKeybind(Keybind keybind)
    {
        if (PlayerKeybind.Get(keybind.Id) is not null) return;

        PlayerKeybind.Register(keybind.Id, keybind.Mod, keybind.Name, keybind.KeyboardPreset, keybind.GamepadPreset, keybind.XboxPreset);
    }

    public static void UpdateInput()
    {
        foreach (CustomInputData? data in InputData)
        {
            if (data is null) continue;

            data.UpdateInput();
        }
    }
}