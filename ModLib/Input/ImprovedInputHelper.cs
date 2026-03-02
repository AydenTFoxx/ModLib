using System;
using ImprovedInput;
using UnityEngine;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    private static NonPlayerData?[] InputData = new NonPlayerData[1];

    public static NonPlayerData? GetInputListener(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= Keybind.TotalMaxPlayers)
            throw new ArgumentOutOfRangeException(nameof(playerNumber), $"Player index must be a value between 0 and {Keybind.TotalMaxPlayers - 1}.");

        if (playerNumber >= InputData.Length)
            Array.Resize(ref InputData, InputData.Length * 2);

        return InputData[playerNumber];
    }

    public static void AddInputListener(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= Keybind.TotalMaxPlayers)
            throw new ArgumentOutOfRangeException(nameof(playerNumber), $"Player index must be a value between 0 and {Keybind.TotalMaxPlayers - 1}.");

        if (playerNumber >= InputData.Length)
            Array.Resize(ref InputData, InputData.Length * 2);

        InputData[playerNumber] ??= new NonPlayerData(playerNumber);
    }

    public static KeyCode KeyCodeFromKeybind(Keybind keybind, int playerIndex) => KeyCodeFromKeybind((PlayerKeybind)keybind, playerIndex);

    public static KeyCode KeyCodeFromKeybind(PlayerKeybind keybind, int playerIndex) =>
        RWCustom.Custom.rainWorld?.options?.controls[playerIndex].KeyCodeFromAction(keybind.gameAction, 0, keybind.axisPositive) ?? 0;

    public static bool IsKeyDown(Player player, Keybind keybind, bool rawInput) =>
        rawInput
            ? player.RawInput()[keybind]
            : player.IsPressed(keybind);

    public static bool IsKeyDown(int playerNumber, Keybind keybind, bool rawInput)
    {
        NonPlayerData? data = InputData[playerNumber];

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
        NonPlayerData? data = InputData[playerNumber];

        if (data is null) return false;

        CustomInput[] history = rawInput ? data.RawInputHistory() : data.InputHistory();

        return history[0][keybind] && !history[1][keybind];
    }

    public static void RegisterKeybind(Keybind keybind)
    {
        if (PlayerKeybind.Get(keybind.Id) is not null) return;

        PlayerKeybind.Register(keybind.Id, keybind.Mod, keybind.Name, keybind.KeyboardKey, keybind.GamepadKey, keybind.XboxKey);
    }

    public static void UpdateInput()
    {
        if (InputData.Length == 0) return;

        foreach (NonPlayerData? data in InputData)
        {
            if (data is null) continue;

            data.UpdateInput();
        }
    }
}