using Menu.Remix.MixedUI;
using MyMod.Utils.Options;
using UnityEngine;

namespace MyMod;

/// <summary>
/// Holds definitions and raw values of the mod's REMIX options.
/// </summary>
/// <seealso cref="ServerOptions"/>
public class Options : OptionInterface
{
    [ClientOption]
    public static Configurable<string>? SLUGCAT_FASHION; // Client-only; Not synced with other players

    public static Configurable<bool>? CAN_EXPLODE_SELF; // Server-side; Synced from host player to clients upon joining a lobby

    public Options()
    {
        SLUGCAT_FASHION = config.Bind(
            "slugcat_fashion",
            "classic",
            new ConfigurableInfo(
                "Unlocks a new fashion sense for slugcats. \"Classic\" is vanilla; \"Overhauled\" is pure chaos.",
                new ConfigAcceptableList<string>("classic", "overhauled")
            )
        );
        CAN_EXPLODE_SELF = config.Bind(
            "can_explode_self",
            false,
            new ConfigurableInfo(
                "If enabled, pressing the \"Explode\" keybind (default: E) explodes the player."
            )
        );
    }

    public override void Initialize()
    {
        Logger.LogInfo($"{nameof(Options)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[1];

        Tabs[0] = new OptionBuilder(this, "Main Options")
            .AddComboBoxOption("Slugcat Fashion", SLUGCAT_FASHION!, width: 120)
            .AddPadding(Vector2.up * 10)
            .AddCheckBoxOption("Can Explode Self", CAN_EXPLODE_SELF!)
            .Build();
    }
}