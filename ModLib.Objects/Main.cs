using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace ModLib.Objects;

internal static class Main
{
    private static ILHook[]? ilHooks;

    public static BepInPlugin Metadata { get; } = new("ynhzrfxn.modlib-objects", "ModLib Objects Extension", "0.3.3.0");

    public static void Initialize()
    {
        ilHooks = [
            new ILHook(typeof(Core).GetMethod("ExitGameHook", BindingFlags.NonPublic | BindingFlags.Static), ClearGUADsOnExitILHook),
            new ILHook(typeof(Core).GetMethod("GameUpdateHook", BindingFlags.NonPublic | BindingFlags.Static), UpdateGUADsILHook)
        ];

        for (int i = 0; i < ilHooks.Length; i++)
        {
            ilHooks[i].Apply();
        }
    }

    public static void Disable()
    {
        if (ilHooks is null) return;

        for (int i = 0; i < ilHooks.Length; i++)
        {
            ilHooks[i].Undo();
        }

        ilHooks = null;
    }

    private static void ClearGUADsOnExitILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(static x => x.MatchRet())
         .MoveAfterLabels()
         .Emit(OpCodes.Ldsfld, typeof(GlobalUpdatableAndDeletable).GetField(nameof(GlobalUpdatableAndDeletable.Instances), BindingFlags.NonPublic | BindingFlags.Static))
         .Emit(OpCodes.Callvirt, typeof(List<>).GetMethod(nameof(List<>.Clear)));
    }

    private static void UpdateGUADsILHook(ILContext context) // "There's no such thing as 'too many IL hooks'!" -- me, probably
    {
        ILCursor c = new(context);

        c.GotoNext(static x => x.MatchRet())
         .MoveAfterLabels()
         .Emit(OpCodes.Ldarg_1)
         .EmitDelegate(UpdateGUADs);

        static void UpdateGUADs(RainWorldGame self)
        {
            foreach (GlobalUpdatableAndDeletable guad in GlobalUpdatableAndDeletable.Instances)
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
        }
    }
}