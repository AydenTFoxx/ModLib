using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    private static readonly CustomInputData?[] InputData = new CustomInputData[CustomInputExt.MaxPlayers];

    public static CustomInputData? GetInputListener(int playerNumber) => InputData[playerNumber];

    public static void SetInputListener(int playerNumber, bool enable) =>
        InputData[playerNumber] = enable ? new CustomInputData(playerNumber) : null;

    public static bool IsKeyDown(Player player, Keybind playerKeybind) => player.IsPressed(playerKeybind);
    public static bool IsKeyDown(int playerNumber, Keybind playerKeybind) =>
        InputData[playerNumber]?.IsPressed(playerKeybind) ?? false;

    public static bool WasKeyJustPressed(Player player, Keybind playerKeybind) => player.JustPressed(playerKeybind);
    public static bool WasKeyJustPressed(int playerNumber, Keybind playerKeybind) =>
        InputData[playerNumber]?.JustPressed(playerKeybind) ?? false;

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