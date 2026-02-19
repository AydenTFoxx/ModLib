using BepInEx;

namespace ModLib.Loader;

/// <summary>
///     Specifies that a given class is the entrypoint for a ModLib extension assembly.
/// </summary>
public interface IExtensionEntrypoint
{
    /// <summary>
    ///     The metadata of the extension assembly, used for identification.
    /// </summary>
    BepInPlugin Metadata { get; }

    /// <summary>
    ///     Invoked just before ModLib initializes; Has the guarantee most ModLib components are initialized, though some features may not be available yet.
    /// </summary>
    void OnEnable();

    /// <summary>
    ///     Invoked just before ModLib is disabled.
    /// </summary>
    void OnDisable();
}