using System.Collections.Generic;

namespace ModLib.Loader;

internal static class ModLibAccess
{
    public static void TryLoadModLib(IList<string> compatibilityPaths) =>
        Entrypoint.Initialize(compatibilityPaths);
}