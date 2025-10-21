namespace ModLib.Input;

/// <summary>
///     General interface for managing keybinds and retrieving player input.
/// </summary>
public static class InputHandler
{
    /// <summary>
    ///     Determines whether a given keybind is currently being held by the player.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <returns><c>true</c> if the keybind's key is currently being held, <c>false</c> otherwise.</returns>
    public static bool IsKeyDown(this Player player, Keybind keybind) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.IsKeyDown(player, keybind)
            : keybind.IsDown(player.playerState.playerNumber);

    /// <summary>
    ///     Determines whether a given keybind is currently being held by the player with the provided index.
    /// </summary>
    /// <param name="playerNumber">The index of the player.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <returns><c>true</c> if the keybind's key is currently being held, <c>false</c> otherwise.</returns>
    public static bool IsKeyDown(int playerNumber, Keybind keybind) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.IsKeyDown(playerNumber, keybind)
            : keybind.IsDown(playerNumber);

    /// <summary>
    ///     Determines whether a given keybind has just been pressed by the player.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <returns><c>true</c> if the keybind's key was just pressed, <c>false</c> otherwise.</returns>
    public static bool WasKeyJustPressed(this Player player, Keybind keybind) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.WasKeyJustPressed(player, keybind)
            : keybind.JustPressed(player.playerState.playerNumber);

    /// <summary>
    ///     Determines whether a given keybind has just been pressed by the player with the provided index.
    /// </summary>
    /// <param name="playerNumber">The index of the player.</param>
    /// <param name="keybind">The keybind to be checked.</param>
    /// <returns><c>true</c> if the keybind's key was just pressed, <c>false</c> otherwise.</returns>
    public static bool WasKeyJustPressed(int playerNumber, Keybind keybind) =>
        Extras.IsIICEnabled
            ? ImprovedInputHelper.WasKeyJustPressed(playerNumber, keybind)
            : keybind.JustPressed(playerNumber);
}