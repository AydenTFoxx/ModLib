using System.Linq;
using ImprovedInput;
using ModLib.Collections;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    private static readonly WeakList<UpdatableAndDeletable> InputListeners = [];

    public static bool IsKeyDown(Player player, Keybind keybind) => IsKeyDown(player, (PlayerKeybind)keybind);
    public static bool IsKeyDown(Player player, PlayerKeybind playerKeybind) => player.IsPressed(playerKeybind);

    public static bool IsKeyDown(int playerNumber, Keybind keybind) => IsKeyDown(playerNumber, (PlayerKeybind)keybind);
    public static bool IsKeyDown(int playerNumber, PlayerKeybind playerKeybind) =>
        ((CustomInputHolder)InputListeners.ElementAtOrDefault(playerNumber)).IsPressed(playerKeybind);

    public static bool WasKeyJustPressed(Player player, Keybind keybind) => WasKeyJustPressed(player, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(Player player, PlayerKeybind playerKeybind) => player.JustPressed(playerKeybind);

    public static bool WasKeyJustPressed(int playerNumber, Keybind keybind) => WasKeyJustPressed(playerNumber, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(int playerNumber, PlayerKeybind playerKeybind) =>
        ((CustomInputHolder)InputListeners.ElementAtOrDefault(playerNumber)).JustPressed(playerKeybind);

    public static void RegisterKeybind(Keybind keybind)
    {
        if (PlayerKeybind.Get(keybind.Id) is not null) return;

        PlayerKeybind.Register(keybind.Id, keybind.Mod, keybind.Name, keybind.KeyboardPreset, keybind.GamepadPreset, keybind.XboxPreset);
    }

    public static void RegisterInputListener(UpdatableAndDeletable listener, int playerNumber) => InputListeners[playerNumber] = listener;
}