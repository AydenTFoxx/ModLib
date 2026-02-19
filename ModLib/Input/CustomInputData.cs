using ImprovedInput;

namespace ModLib.Input;

/// <summary>
///     Provides simple representation of a given player's input data;
///     A mirror of <see cref="Plugin.PlayerData"/> which can be converted to and from its original class.
/// </summary>
public class CustomInputData
{
    /// <summary>
    ///     The current input buffer of the bound player, accounting for factors such as stun, death, or cutscenes.
    /// </summary>
    public readonly CustomInput[] input = new CustomInput[CustomInputExt.HistoryLength];

    /// <summary>
    ///     The raw input of the bound player, retrieved directly from the game.
    /// </summary>
    public readonly CustomInput[] rawInput = new CustomInput[CustomInputExt.HistoryLength];

    /// <summary>
    ///     The player index this data is bound to.
    /// </summary>
    public int playerNumber;

    /// <summary>
    ///     Creates a new <see cref="CustomInputData"/> from the given <see cref="Plugin.PlayerData"/> instance,
    ///     then binds it to the provided player index.
    /// </summary>
    /// <param name="data">The data whose values will be copied from.</param>
    /// <param name="playerNumber">The player index this data should be bound to.</param>
    public CustomInputData(Plugin.PlayerData data, int playerNumber = 0)
    {
        this.playerNumber = playerNumber;

        CopyFrom(data);
    }

    /// <summary>
    ///     Creates a new <see cref="CustomInputData"/> with empty values, bound to the provided player index.
    /// </summary>
    /// <param name="playerNumber">The player index this data will be bound to.</param>
    public CustomInputData(int playerNumber)
        : this()
    {
        this.playerNumber = playerNumber;
    }

    /// <summary>
    ///     Creates a new <see cref="CustomInputData"/> with empty values, bound to the default player index.
    /// </summary>
    public CustomInputData()
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = new CustomInput();
        }

        for (int j = 0; j < input.Length; j++)
        {
            rawInput[j] = new CustomInput();
        }
    }

    /// <summary>
    ///     Copies the input data from the provided PlayerData to this instance.
    /// </summary>
    /// <param name="data">The data whose values will be copied from.</param>
    public void CopyFrom(Plugin.PlayerData data) => CopyFrom(data.input, data.rawInput);

    /// <summary>
    ///     Copies the input data from the provided input arrays.
    /// </summary>
    /// <param name="input">The input to be copied from.</param>
    /// <param name="rawInput">The raw input to be copied from.</param>
    public void CopyFrom(CustomInput[] input, CustomInput[] rawInput)
    {
        input.CopyTo(this.input, 0);
        rawInput.CopyTo(this.rawInput, 0);
    }

    /// <summary>
    ///     Copies this instance's input data to the provided PlayerData instance.
    /// </summary>
    /// <param name="data">The data whose values will be copied to.</param>
    public void CopyTo(Plugin.PlayerData data) => CopyTo(data.input, data.rawInput);

    /// <summary>
    ///     Copies this instance's input data to the provided input arrays.
    /// </summary>
    /// <param name="input">The input array to be copied to.</param>
    /// <param name="rawInput">The raw input array to be copied to.</param>
    public void CopyTo(CustomInput[] input, CustomInput[] rawInput)
    {
        this.input.CopyTo(input, 0);
        this.rawInput.CopyTo(rawInput, 0);
    }

    internal void UpdateInput(UpdatableAndDeletable? listener = null)
    {
        for (int num = input.Length - 1; num > 0; num--)
        {
            input[num] = input[num - 1];
        }

        for (int num2 = rawInput.Length - 1; num2 > 0; num2--)
        {
            rawInput[num2] = rawInput[num2 - 1];
        }

        rawInput[0] = CustomInput.GetRawInput(playerNumber);
        input[0] = listener is not Creature creature || (creature is { dead: false, stun: 0 })
            ? rawInput[0].Clone()
            : new CustomInput();
    }

    /// <summary>
    ///     Converts this data instance to an equivalent <see cref="Plugin.PlayerData"/> instance.
    /// </summary>
    /// <param name="self">The data itself.</param>
    public static implicit operator Plugin.PlayerData(CustomInputData self)
    {
        Plugin.PlayerData data = new();

        self.CopyTo(data);

        return data;
    }

    /// <summary>
    ///     Converts a <see cref="Plugin.PlayerData"/> instance to an equivalent <see cref="CustomInputData"/> instance.
    /// </summary>
    /// <param name="self">The data itself.</param>
    public static implicit operator CustomInputData(Plugin.PlayerData self)
    {
        CustomInputData data = new();

        data.CopyFrom(self);

        return data;
    }
}