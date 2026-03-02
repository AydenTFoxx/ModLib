# ModLib | Extension Assemblies

> [!NOTE]
>
> This document was last updated on `2026-02-26`, and is accurate for ModLib version `0.4.1.0`.

ModLib can be easily tweaked and extended with *extensions*, special assemblies which are retrieved and loaded before ModLib's own initialization process. This document covers the creation of a simple ModLib extension which logs a message during its initialization and deactivation phases.

## Creating an Extension Entrypoint

For an assembly to be recognized as a ModLib extension, it must meet the following requirements:

- File name (not assembly name) MUST end with `.modlib.dll` (e.g. `MyCoolExtension.modlib.dll`)
- At least one *entrypoint* class must be present. An entrypoint class is defined by the following requirements:
  - Must not be static, abstract or an interface;
  - Must implement the `ModLib.Loader.IExtensionEntrypoint` interface;
  - Must have a parameterless constructor.

Here's an example of a basic entrypoint class:

```cs
using BepInEx;
using BepInEx.Logging;
using ModLib.Loader;

// This class will be instantiated by ModLib during BepInEx's preloading process -- as such, do NOT reference Rain World's assembly in your constructor! (It WILL cause the game to not load correctly)

public class MyEntrypoint : IExtensionEntrypoint
{
    // Not required, but useful to have. Used in the below examples for logging to BepInEx during initialization and deactivation.
    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("My Entrypoint");

    // Used for identifying your assembly during ModLib's initialization process.
    public BepInPlugin Metadata { get; } = new("example.entrypoint", "My Entrypoint", "1.0.0.0");

    // Called during BepInEx's chainloader process. Rain World's assembly and types can be accessed from here onwards, but mods may not have loaded yet.
    public void OnEnable()
    {
        Logger.LogInfo("Entrypoint was initialized!");
    }

    // Called just before the game quits. Can be using for storing data to disk, freeing unmanaged resources, or just about whatever you need to do before the application closes.
    public void OnDisable()
    {
        Logger.LogInfo("Entrypoint was disabled!");
    }
}
```

When compiled, the above assembly's file could be named `MyEntrypoint.modlib.dll`, which would make it be loaded by ModLib during its initialization process.

## Loading and Using an Extension Assembly

In order to use a ModLib extension in your code, add it as a reference in your project (.csproj) file, and also include its assembly alongside your mod's compiled assembly:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <!-- Example of a csproj file where "MyExtensionAssembly.dll" is included as a referenced assembly, -->
    <!-- and also copied to "../mod/newest/plugins/" alongside the generated mod assembly -->

    <!-- ... Your other settings here... -->

    <ItemGroup>
        <Reference Include="../path/to/MyExtensionAssembly.dll">
            <Private>false</Private>
        </Reference>
    </ItemGroup>

    <Target Name="CopyModAssemblies" AfterTargets="PostBuildEvent">
        <ItemGroup>
            <CopyPlugins Include="$(TargetDir)$(TargetName).dll" />
            <CopyPlugins Include="$(TargetDir)$(TargetName).pdb" />

            <CopyDeps Include="../path/to/MyExtensionAssembly.dll" />
            <CopyDeps Include="../path/to/MyExtensionAssembly.pdb" />
        </ItemGroup>

        <Copy SourceFiles="@(CopyPlugins); @(CopyDeps)" DestinationFolder="../mod/newest/plugins/" />
    </Target>

</Project>
```

When loading your mod's assembly, ModLib will automatically detect the extension assembly and initialize it before any mod begins its initialization process.
