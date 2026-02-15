using System.Linq;
using System.Runtime.CompilerServices;

namespace ModLib.Extensions;

/// <summary>
///     Extension methods for manipulating <see cref="PhysicalObject"/> instances.
/// </summary>
internal static class PhysicalObjectExtensions
{
    /// <summary>
    ///     Stuns all creatures holding the current physical object.
    /// </summary>
    /// <param name="self">The physical object itself.</param>
    /// <param name="stun">The duration for the stun.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StunAllGrasps(this PhysicalObject self, int stun)
    {
        for (int i = self.grabbedBy.Count - 1; i >= 0; i--)
        {
            Creature? grabber = self.grabbedBy.ElementAtOrDefault(i)?.grabber;

            if (grabber is null) continue;

            grabber.LoseAllGrasps();
            grabber.Stun(stun);
        }
    }
}