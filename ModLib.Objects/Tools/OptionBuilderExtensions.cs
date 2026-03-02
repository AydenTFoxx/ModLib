using BepInEx;
using ModLib.Options;
using UnityEngine;

namespace ModLib.Objects;

/// <summary>
///     Extension methods for the <see cref="OptionBuilder"/> class.
/// </summary>
public static class OptionBuilderExtensions
{
    /// <summary>
    ///     Creates a basic header featuring the mod's name and version.
    /// </summary>
    /// <remarks>
    ///     This method overrides the current position of the builder, and should be used before all other methods.
    /// </remarks>
    /// <param name="self">The OptionBuilder instance.</param>
    /// <param name="plugin">The plugin instance whose metadata will be used.</param>
    /// <param name="colors">The color values to be used by the title and version, respectively.</param>
    /// <returns>The OptionBuilder itself.</returns>
    public static OptionBuilder CreateModHeader(this OptionBuilder self, BaseUnityPlugin plugin, params Color[] colors) => CreateModHeader(self, plugin.Info.Metadata, colors);

    /// <summary>
    ///     Creates a basic header featuring the mod's name and version.
    /// </summary>
    /// <remarks>
    ///     This method overrides the current position of the builder, and should be used before all other methods.
    /// </remarks>
    /// <param name="self">The OptionBuilder instance.</param>
    /// <param name="metadata">The plugin metadata.</param>
    /// <param name="colors">The color values to be used by the title and version, respectively.</param>
    /// <returns>The OptionBuilder itself.</returns>
    public static OptionBuilder CreateModHeader(this OptionBuilder self, BepInPlugin metadata, params Color[] colors) =>
        self.SetOrigin(new Vector2(100f, 500f))
            .AddText(metadata.Name, new Vector2(64f, 0f), true, OptionBuilder.GetColorOrDefault(colors, 0))
            .AddText($"[v{metadata.Version}]", new Vector2(100f, 32f), false, OptionBuilder.GetColorOrDefault(colors, 1, Color.gray))
            .ResetOrigin();
}