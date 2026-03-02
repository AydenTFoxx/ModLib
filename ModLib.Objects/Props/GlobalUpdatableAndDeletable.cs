using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ModLib.Objects;

/// <summary>
///     A variant of <see cref="UpdatableAndDeletable"/> which is updated independently of a room.
/// </summary>
/// <remarks>
///     Instances of this class (or subtypes) are NOT synced upon joining a Rain Meadow server;
///     Mods should roll their own sync implementation if Rain Meadow compatibility is desired.
/// </remarks>
public abstract class GlobalUpdatableAndDeletable
{
    private static readonly List<GlobalUpdatableAndDeletable> _instances = [];

    /// <inheritdoc cref="UpdatableAndDeletable.evenUpdate"/>
    public bool evenUpdate;

    /// <inheritdoc cref="UpdatableAndDeletable.slatedForDeletetion"/>
    public bool slatedForDeletetion;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GlobalUpdatableAndDeletable"/> class.
    /// </summary>
    public GlobalUpdatableAndDeletable()
    {
        _instances.Add(this);
    }

    /// <inheritdoc cref="UpdatableAndDeletable.Update(bool)"/>
    public virtual void Update(bool eu) => evenUpdate = eu;

    /// <inheritdoc cref="UpdatableAndDeletable.PausedUpdate()"/>
    public virtual void PausedUpdate() { }

    /// <inheritdoc cref="UpdatableAndDeletable.Destroy()"/>
    public virtual void Destroy() => slatedForDeletetion = true;

    internal static class Hooks
    {
        public static void Apply()
        {
            IL.RainWorldGame.Update += UpdateGUADsILHook;

            On.RainWorldGame.ExitGame += ClearGUADsOnExitHook;
        }

        public static void Remove()
        {
            IL.RainWorldGame.Update -= UpdateGUADsILHook;

            On.RainWorldGame.ExitGame -= ClearGUADsOnExitHook;
        }

        private static void ClearGUADsOnExitHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
        {
            orig.Invoke(self, asDeath, asQuit);

            _instances.Clear();

            if (!asDeath && !asQuit && self.IsStorySession)
                DeathProtection.SaveInstancesToDisk(); // not worth creating a second hook for this, sorry
        }

        private static void UpdateGUADsILHook(ILContext context)
        {
            ILCursor c = new(context);

            c.GotoNext(MoveType.After, static x => x.MatchCallvirt(typeof(PathfinderResourceDivider).GetMethod(nameof(PathfinderResourceDivider.Update))))
             .MoveAfterLabels()
             .Emit(OpCodes.Ldarg_0)
             .EmitDelegate(UpdateGUADs);

            static void UpdateGUADs(RainWorldGame self)
            {
                foreach (GlobalUpdatableAndDeletable guad in _instances)
                {
                    if (self.GamePaused)
                    {
                        guad.PausedUpdate();
                    }
                    else
                    {
                        guad.Update(self.evenUpdate);
                    }
                }

                _instances.RemoveAll(static guad => guad.slatedForDeletetion);
            }
        }
    }
}