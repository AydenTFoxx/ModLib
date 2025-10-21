using ImprovedInput;

namespace ModLib.Input;

public class CustomInputData
{
    public readonly CustomInput[] input = new CustomInput[CustomInputExt.HistoryLength];

    public readonly CustomInput[] rawInput = new CustomInput[CustomInputExt.HistoryLength];

    public int playerNumber;

    public CustomInputData(Plugin.PlayerData data)
    {
        CopyFrom(data);
    }

    public CustomInputData(int playerNumber)
        : this()
    {
        this.playerNumber = playerNumber;
    }

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

    public void CopyFrom(Plugin.PlayerData data) => CopyFrom(data.input, data.rawInput);

    public void CopyFrom(CustomInput[] input, CustomInput[] rawInput)
    {
        input.CopyTo(this.input, 0);
        rawInput.CopyTo(this.rawInput, 0);
    }

    public void CopyTo(Plugin.PlayerData data) => CopyTo(data.input, data.rawInput);

    public void CopyTo(CustomInput[] input, CustomInput[] rawInput)
    {
        this.input.CopyTo(input, 0);
        this.rawInput.CopyTo(rawInput, 0);
    }

    public static implicit operator Plugin.PlayerData(CustomInputData self)
    {
        Plugin.PlayerData data = new();

        self.CopyTo(data);

        return data;
    }

    public static implicit operator CustomInputData(Plugin.PlayerData self)
    {
        CustomInputData data = new();

        data.CopyFrom(self);

        return data;
    }
}