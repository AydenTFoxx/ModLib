using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using UnityEngine;

namespace ModLib.Input;

/// <summary>
///     An immutable representation of a player keybind, compatible with ImprovedInput's PlayerKeybind object.
/// </summary>
public sealed class Keybind
{
    private static readonly List<Keybind> _keybinds = [];

    internal static int TotalMaxPlayers => Extras.IsIICEnabled ? 16 : 4;

    internal static int MaxPlayers { get; set; } = 1;

    /// <summary>
    ///     Returns a read-only list of all registered keybinds.
    /// </summary>
    /// <returns>A read-only list of all registered keybinds.</returns>
    public static ReadOnlyCollection<Keybind> Keybinds { get; } = new(_keybinds);

    static Keybind()
    {
        Core.InputModuleActivated = true;
    }

    /// <summary>
    ///     The unique identifier of this Keybind.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     The name of the mod this Keybind belongs to.
    /// </summary>
    public string Mod { get; }

    /// <summary>
    ///     The user-friendly name of this Keybind.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with a keyboard. Can be set for a temporary override which is not saved to disk.
    /// </summary>
    public KeyCode KeyboardKey
    {
        get => _keyOverrides[0];
        set => _keyOverrides[0] = value;
    }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with a gamepad.  Can be set for a temporary override which is not saved to disk.
    /// </summary>
    public KeyCode GamepadKey
    {
        get => _keyOverrides[1];
        set => _keyOverrides[1] = value;
    }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with an xbox. Can be set for a temporary override which is not saved to disk.
    /// </summary>
    public KeyCode XboxKey
    {
        get => _keyOverrides[2];
        set => _keyOverrides[2] = value;
    }

    private bool[,] _keyPresses = new bool[MaxPlayers, 2];

    private readonly KeyCode[] _keyOverrides = new KeyCode[3];

    private Keybind(string id, string mod, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Id = id;
        Mod = mod;
        Name = name;

        KeyboardKey = keyboardPreset;
        GamepadKey = gamepadPreset;
        XboxKey = xboxPreset;
    }

    /// <summary>
    ///     Determines whether this keybind is currently being pressed.
    /// </summary>
    /// <remarks>
    ///     Note: Avoid calling this method directly; Instead, prefer using the extension methods of the <see cref="InputHandler"/> class.
    /// </remarks>
    /// <param name="playerNumber">The player index whose input will be queried.</param>
    /// <param name="player">
    ///     The actual player instance whose inputs are being queried.
    ///     If specified, input will be ignored while the player is unconscious, dead, or in a cutscene.
    /// </param>
    /// <returns><c>true</c> if this keybind's bound key is being held, <c>false</c> otherwise.</returns>
    public bool IsDown(int playerNumber, Player? player)
    {
        ValidatePlayerNumber(playerNumber);

        return (player is null or { Consious: true, mapInput.mp: false, controller: null }) && _keyPresses[playerNumber, 0];
    }

    /// <summary>
    ///     Determines whether this keybind has just been pressed.
    /// </summary>
    /// <remarks>
    ///     Note: Avoid calling this method directly; Instead, prefer using the extension methods of the <see cref="InputHandler"/> class.
    /// </remarks>
    /// <param name="playerNumber">The player index whose input will be queried.</param>
    /// <param name="player">
    ///     The actual player instance whose inputs are being queried.
    ///     If specified, input will be ignored while the player is unconscious, dead, or in a cutscene.
    /// </param>
    /// <returns><c>true</c> if this keybind's bound key was just pressed, <c>false</c> otherwise.</returns>
    public bool JustPressed(int playerNumber, Player? player)
    {
        ValidatePlayerNumber(playerNumber);

        return (player is null or { Consious: true, mapInput.mp: false, controller: null }) && _keyPresses[playerNumber, 0] && !_keyPresses[playerNumber, 1];
    }

    internal void Update(global::Options? options, int playerIndex)
    {
        _keyPresses[playerIndex, 1] = _keyPresses[playerIndex, 0];

        _keyPresses[playerIndex, 0] = options is not null && (options.controls[playerIndex].controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
            ? options.controls[playerIndex].recentPreset == global::Options.ControlSetup.Preset.XBox
                ? UnityEngine.Input.GetKey(XboxKey)
                : UnityEngine.Input.GetKey(GamepadKey)
            : UnityEngine.Input.GetKey(KeyboardKey));
    }

    internal void ForceInput(int playerIndex, bool value)
    {
        ValidatePlayerNumber(playerIndex);

        _keyPresses[playerIndex, 1] = _keyPresses[playerIndex, 0];
        _keyPresses[playerIndex, 0] = value;
    }

    private void ValidatePlayerNumber(int playerNumber)
    {
        if (playerNumber is < 0 || playerNumber >= TotalMaxPlayers)
            throw new ArgumentOutOfRangeException(nameof(playerNumber), $"Player number {playerNumber} is not within valid range: 0-{TotalMaxPlayers - 1}.");

        if (_keyPresses.GetLength(0) <= playerNumber)
        {
            bool[,] lastKeyPresses = (bool[,])_keyPresses.Clone();

            _keyPresses = new bool[(Math.Min(_keyPresses.GetLength(0) * 2, TotalMaxPlayers)), 2];

            for (int i = 0; i < lastKeyPresses.GetLength(0); i++)
            {
                _keyPresses[i, 0] = lastKeyPresses[i, 0];
                _keyPresses[i, 1] = lastKeyPresses[i, 1];
            }

            MaxPlayers = Math.Max(MaxPlayers, _keyPresses.GetLength(0));

            Core.Logger.LogInfo($"Resizing keyPresses array of {Name} ({Id}) to {_keyPresses.GetLength(0)}");
        }
    }

    /// <summary>
    ///     Returns a string that represents the Keybind object.
    /// </summary>
    /// <returns>A string that represents the Keybind object.</returns>
    public override string ToString() => $"{Name} ({Id}) [{KeyboardKey}|{GamepadKey}{(XboxKey != GamepadKey ? $"|{XboxKey}" : "")}]";

    /// <summary>
    ///     Retrieves the Keybind with the given identifier.
    /// </summary>
    /// <param name="id">The identifier of the Keybind to be retrieved.</param>
    /// <returns>The <see cref="Keybind"/> object whose Id matches the provided argument, or <c>null</c> if none is found.</returns>
    public static Keybind? Get(string id) => _keybinds.Find(k => k.Id == id);

    /// <summary>
    ///     Retrieves the configured key of a player for a given keybind.
    /// </summary>
    /// <param name="keybind">The keybind whose key will be retrieved.</param>
    /// <param name="playerIndex">The index of the player.</param>
    /// <returns>The configured key code of the keybind for the given player.</returns>
    /// <exception cref="ArgumentOutOfRangeException">playerIndex is below zero or greater than the max amount of players.</exception>
    public static KeyCode KeyCodeFromKeybind(Keybind keybind, int playerIndex)
    {
        if (playerIndex is < 0 || playerIndex >= TotalMaxPlayers)
            throw new ArgumentOutOfRangeException(nameof(playerIndex), $"Player number {playerIndex} is not within valid range: 0-{TotalMaxPlayers - 1}.");

        if (Extras.IsIICEnabled)
            return ImprovedInputHelper.KeyCodeFromKeybind(keybind, playerIndex);

        global::Options.ControlSetup? playerControls = RWCustom.Custom.rainWorld?.options?.controls[playerIndex];

        return playerControls is null
            ? KeyCode.None
            : playerControls.controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
                ? playerControls.recentPreset == global::Options.ControlSetup.Preset.XBox
                    ? keybind.XboxKey
                    : keybind.GamepadKey
                : keybind.KeyboardKey;
    }

    /// <inheritdoc cref="Register(string, string, KeyCode, KeyCode, KeyCode)"/>
    public static Keybind Register(string name, KeyCode keyboardPreset = KeyCode.None, KeyCode gamepadPreset = KeyCode.None, KeyCode xboxPreset = KeyCode.None) =>
        RegisterInternal(Assembly.GetCallingAssembly(), null, name, keyboardPreset, gamepadPreset, xboxPreset);

    /// <summary>
    ///     Registers a new Keybind with the provided arguments.
    /// </summary>
    /// <remarks>
    ///     If the Improved Input: Extended mod is present, an equivalent <c>PlayerKeybind</c> is also registered to the game.
    /// </remarks>
    /// <param name="id">
    ///     <para>
    ///         The identifier of this keybind. Must be an unique string not used by any other keybind.
    ///     </para>
    ///     <para>
    ///         If null, an unique identifier is generated with the format <c>"{ModId}.{KeybindName}"</c>
    ///     </para>
    /// </param>
    /// <param name="name">The name of the new Keybind. Will be displayed for players with IIC:E enabled.</param>
    /// <param name="keyboardPreset">The key code for usage by keyboard devices.</param>
    /// <param name="gamepadPreset">The key code for usage by gamepad input devices.</param>
    /// <param name="xboxPreset">The key code for usage by Xbox input devices.</param>
    /// <returns>The registered <see cref="Keybind"/> object.</returns>
    public static Keybind Register(string? id, string name, KeyCode keyboardPreset = KeyCode.None, KeyCode gamepadPreset = KeyCode.None, KeyCode xboxPreset = KeyCode.None) =>
        RegisterInternal(Assembly.GetCallingAssembly(), id, name, keyboardPreset, gamepadPreset, xboxPreset);

    private static Keybind RegisterInternal(Assembly caller, string? id, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        BepInPlugin plugin = Registry.GetMod(caller).Plugin;

        id ??= $"{GetModPrefix(plugin)}.{name.ToLowerInvariant()}";

        Keybind? gameKeybind = Get(id);

        if (gameKeybind is null)
        {
            gameKeybind = new Keybind(id, plugin.Name, name, keyboardPreset, gamepadPreset, xboxPreset);

            if (Extras.IsIICEnabled)
                ImprovedInputAccess.RegisterKeybind(gameKeybind);

            _keybinds.Add(gameKeybind);

            Core.Logger.LogDebug($"[Keybind] Registered new keybind: {gameKeybind}");
        }
        else
        {
            Core.Logger.LogDebug($"[Keybind] Retrieving existing keybind: {gameKeybind}");
        }

        return gameKeybind;

        static string GetModPrefix(BepInPlugin plugin)
        {
            return plugin.GUID.Split('.', '_', ' ', '-').Last().ToLowerInvariant();
        }
    }

    private static Keybind RegisterRaw(string id, string mod, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Keybind? keybind = Get(id);

        if (keybind is null)
        {
            keybind = new Keybind(id, mod, name, keyboardPreset, gamepadPreset, xboxPreset);

            _keybinds.Add(keybind);
        }

        return keybind;
    }

    /// <summary>
    ///     Retrieves the equivalent Keybind object of a PlayerKeybind instance. If none is found, a new Keybind is registered using the PlayerKeybind's values as arguments.
    /// </summary>
    /// <param name="self">The PlayerKeybind object to be converted.</param>
    public static implicit operator Keybind(ImprovedInput.PlayerKeybind self)
    {
        return Get(self.Id) ?? RegisterRaw(self.Id, self.Mod, self.Name, self.KeyboardPreset, self.GamepadPreset, self.XboxPreset);
    }

    /// <summary>
    ///     Retrieves the equivalent PlayerKeybind object registered with the Keybind instance. If none is found, a new PlayerKeybind is registered using the Keybind's values as arguments.
    /// </summary>
    /// <param name="self">The Keybind object to be converted.</param>
    public static implicit operator ImprovedInput.PlayerKeybind(Keybind self)
    {
        return ImprovedInput.PlayerKeybind.Get(self.Id) ?? ImprovedInput.PlayerKeybind.Register(self.Id, self.Mod, self.Name, self.KeyboardKey, self.GamepadKey, self.XboxKey);
    }

    private static class ImprovedInputAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RegisterKeybind(Keybind keybind) => ImprovedInputHelper.RegisterKeybind(keybind);
    }
}