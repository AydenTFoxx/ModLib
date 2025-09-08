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
    public static Configurable<string>? EXAMPLE_CLIENT_OPTION;
    public static Configurable<bool>? EXAMPLE_SERVER_OPTION;

    public Options()
    {
        EXAMPLE_CLIENT_OPTION = config.Bind(
            "ex_client_option",
            "classic",
            new ConfigurableInfo(
                "An example setting which is not synced with other clients. This option has no effects.",
                new ConfigAcceptableList<string>("classic", "overhauled")
            )
        );
        EXAMPLE_SERVER_OPTION = config.Bind(
            "ex_server_option",
            false,
            new ConfigurableInfo(
                "An example setting which is synced with clients in an online lobby. This option has no effects."
            )
        );
    }

    public override void Initialize()
    {
        MyLogger.LogInfo($"{nameof(Options)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[1];

        Tabs[0] = new OptionBuilder(this, "Main Options")
            .AddComboBoxOption("Example Client Option", EXAMPLE_CLIENT_OPTION!, width: 120)
            .AddPadding(Vector2.up * 10)
            .AddCheckBoxOption("Example Server Option", EXAMPLE_SERVER_OPTION!)
            .Build();
    }
}