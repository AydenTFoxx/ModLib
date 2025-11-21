using System.Runtime.CompilerServices;

namespace ModLib.Input;

/// <summary>
///     General interface for managing keybinds and retrieving player input.
/// </summary>
public static class InputHandler
{
    /// <summary>
    ///     Retrieves the raw input package for the given player.
    /// </summary>
    /// <param name="self">The player whose input will be queried.</param>
    /// <returns>A <see cref="Player.InputPackage"/> containing the input for the given player.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Player.InputPackage GetRawInput(this Player self) => GetRawInput(self.playerState.playerNumber);

    /// <summary>
    ///     Retrieves the raw input package for the given player index.
    /// </summary>
    /// <param name="playerNumber">The player index whose input will be queried.</param>
    /// <returns>A <see cref="Player.InputPackage"/> containing the input for the given player index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Player.InputPackage GetRawInput(int playerNumber) => RWInput.PlayerInput(playerNumber);

    /// <summary>
    ///     Determines whether a given keybind is currently being held by the player.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <param name="rawInput">If true, input will be evaluated even if Slugcat itself cannot receive inputs (e.g. when dead or in cutscene mode).</param>
    /// <returns><c>true</c> if the keybind's key is currently being held, <c>false</c> otherwise.</returns>
    public static bool IsKeyDown(this Player player, Keybind keybind, bool rawInput = false) =>
        (player.AI is null || player.safariControlled) && (Extras.IsIICEnabled
            ? ImprovedInputHelper.IsKeyDown(player, keybind, rawInput)
            : keybind.IsDown(player.playerState.playerNumber, !rawInput ? player : null));

    /// <summary>
    ///     Determines whether a given keybind is currently being held by the player with the provided index.
    /// </summary>
    /// <remarks>
    ///     Unlike its extension variant, this method makes no checking if the player is able to receive input,
    ///     and may return true even if their character is dead or incapacitated.
    /// </remarks>
    /// <param name="playerNumber">The index of the player.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <param name="rawInput">If true, input will be evaluated even if Slugcat itself cannot receive inputs (e.g. when dead or in cutscene mode).</param>
    /// <returns><c>true</c> if the keybind's key is currently being held, <c>false</c> otherwise.</returns>
    public static bool IsKeyDown(int playerNumber, Keybind keybind, bool rawInput = false) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.IsKeyDown(playerNumber, keybind, rawInput)
            : keybind.IsDown(playerNumber);

    /// <summary>
    ///     Determines whether a given keybind has just been pressed by the player.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <param name="rawInput">If true, input will be evaluated even if Slugcat itself cannot receive inputs (e.g. when dead or in cutscene mode).</param>
    /// <returns><c>true</c> if the keybind's key was just pressed, <c>false</c> otherwise.</returns>
    public static bool WasKeyJustPressed(this Player player, Keybind keybind, bool rawInput = false) =>
        (player.AI is null || player.safariControlled) && (Extras.IsIICEnabled
            ? ImprovedInputHelper.WasKeyJustPressed(player, keybind, rawInput)
            : keybind.JustPressed(player.playerState.playerNumber, !rawInput ? player : null));

    /// <summary>
    ///     Determines whether a given keybind has just been pressed by the player with the provided index.
    /// </summary>
    /// <param name="playerNumber">The index of the player.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <param name="rawInput">If true, input will be evaluated even if Slugcat itself cannot receive inputs (e.g. when dead or in cutscene mode).</param>
    /// <returns><c>true</c> if the keybind's key was just pressed, <c>false</c> otherwise.</returns>
    public static bool WasKeyJustPressed(int playerNumber, Keybind keybind, bool rawInput = false) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.WasKeyJustPressed(playerNumber, keybind, rawInput)
            : keybind.JustPressed(playerNumber);

    /// <summary>
    ///     Enables or disables input handling for non-player objects.
    ///     If enabled, a <see cref="CustomInputData"/> can be retrieved from the player's index to obtain their current input.
    /// </summary>
    /// <remarks>
    ///     This requires the Improved Input Config: Extended mod to work, and will do nothing otherwise.
    /// </remarks>
    /// <param name="playerNumber">The player index to be checked.</param>
    public static void ToggleInputListener(int playerNumber)
    {
        if (!Extras.IsIICEnabled) return;

        CustomInputData? listener = ImprovedInputHelper.GetInputListener(playerNumber);

        ImprovedInputHelper.SetInputListener(playerNumber, listener is null);
    }
}