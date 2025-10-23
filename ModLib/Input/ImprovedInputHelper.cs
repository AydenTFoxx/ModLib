using System.Runtime.CompilerServices;
using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    private static readonly CustomInputData?[] InputData = new CustomInputData[CustomInputExt.MaxPlayers];

    public static CustomInputData? GetInputListener(int playerNumber) => InputData[playerNumber];

    public static void SetInputListener(int playerNumber, bool enable) =>
        InputData[playerNumber] = enable ? new CustomInputData(playerNumber) : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsKeyDown(Player player, Keybind keybind) => IsKeyDown(player, (PlayerKeybind)keybind);
    public static bool IsKeyDown(Player player, PlayerKeybind playerKeybind) => player.IsPressed(playerKeybind);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsKeyDown(int playerNumber, Keybind keybind) => IsKeyDown(playerNumber, (PlayerKeybind)keybind);
    public static bool IsKeyDown(int playerNumber, PlayerKeybind playerKeybind) =>
        InputData[playerNumber]?.IsPressed(playerKeybind) ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WasKeyJustPressed(Player player, Keybind keybind) => WasKeyJustPressed(player, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(Player player, PlayerKeybind playerKeybind) => player.JustPressed(playerKeybind);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WasKeyJustPressed(int playerNumber, Keybind keybind) => WasKeyJustPressed(playerNumber, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(int playerNumber, PlayerKeybind playerKeybind) =>
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