using ModLib.Loader;
using UnityEngine;

namespace ModLib;

internal class CorePlugin : MonoBehaviour
{
    public void OnEnable()
    {
        Extras.WrapAction(Core.Initialize, Core.Logger);

        Core.Logger.LogInfo($"Initialized ModLib v{Core.MOD_VERSION} successfully.");
    }

    public void OnDisable()
    {
        Extras.WrapAction(Entrypoint.Disable, Core.Logger);

        Core.Logger.LogInfo("Disabled ModLib successfully.");
    }
}