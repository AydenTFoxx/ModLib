using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;

namespace Martyr.Slugcat;

/// <summary>
/// Enables a variety of Saint-exclusive behaviors and mechanics for the Martyr slugcat.
/// </summary>
public static class SaintMechanicsHooks
{
    private static readonly ILContext.Manipulator cachedEnableMechanicILHook = MyExtras.WrapILHook(EnableSaintMechanic);
    private static readonly ILContext.Manipulator cachedEnableMechanicTwiceILHook = MyExtras.WrapILHook(EnableSaintMechanicTwice);

    public static void ApplyHooks()
    {
        IL.Centipede.Update += cachedEnableMechanicILHook;
        IL.Cicada.Collide += cachedEnableMechanicTwiceILHook;
        IL.Fly.Update += cachedEnableMechanicILHook;
        IL.SmallNeedleWorm.Update += cachedEnableMechanicILHook;
        IL.Snail.Click += cachedEnableMechanicILHook;

        IL.Player.ctor += cachedEnableMechanicILHook;
        IL.Player.ClassMechanicsSaint += cachedEnableMechanicILHook;
        IL.Player.SaintTongueCheck += cachedEnableMechanicILHook;

        IL.PlayerGraphics.ctor += cachedEnableMechanicILHook;
        IL.PlayerGraphics.AddToContainer += cachedEnableMechanicTwiceILHook;
        IL.PlayerGraphics.ApplyPalette += cachedEnableMechanicILHook;
        IL.PlayerGraphics.MSCUpdate += cachedEnableMechanicILHook;
        IL.PlayerGraphics.Reset += cachedEnableMechanicILHook;

        IL.Player.GrabUpdate += MyExtras.WrapILHook(SaintFlyAscensionILHook);

        IL.PlayerGraphics.DrawSprites += MyExtras.WrapILHook(MartyrDrawInheritedSpritesILHook);
        IL.PlayerGraphics.InitiateSprites += MyExtras.WrapILHook(MartyrInitializeTongueILHook);

        IL.World.CheckForRegionGhost += MyExtras.WrapILHook(PreventGhostSpawningILHook);
    }

    public static void RemoveHooks()
    {
        IL.Centipede.Update -= cachedEnableMechanicILHook;
        IL.Cicada.Collide -= cachedEnableMechanicTwiceILHook;
        IL.Fly.Update -= cachedEnableMechanicILHook;
        IL.SmallNeedleWorm.Update -= cachedEnableMechanicILHook;
        IL.Snail.Click -= cachedEnableMechanicILHook;

        IL.Player.ctor -= cachedEnableMechanicILHook;
        IL.Player.ClassMechanicsSaint -= cachedEnableMechanicILHook;
        IL.Player.SaintTongueCheck -= cachedEnableMechanicILHook;

        IL.PlayerGraphics.ctor -= cachedEnableMechanicILHook;
        IL.PlayerGraphics.AddToContainer -= cachedEnableMechanicTwiceILHook;
        IL.PlayerGraphics.ApplyPalette -= cachedEnableMechanicILHook;
        IL.PlayerGraphics.MSCUpdate -= cachedEnableMechanicILHook;
        IL.PlayerGraphics.Reset -= cachedEnableMechanicILHook;

        IL.Player.GrabUpdate -= MyExtras.WrapILHook(SaintFlyAscensionILHook);

        IL.PlayerGraphics.DrawSprites -= MyExtras.WrapILHook(MartyrDrawInheritedSpritesILHook);
        IL.PlayerGraphics.InitiateSprites -= MyExtras.WrapILHook(MartyrInitializeTongueILHook);

        IL.World.CheckForRegionGhost -= MyExtras.WrapILHook(PreventGhostSpawningILHook);
    }

    private static void MartyrDrawInheritedSpritesILHook(ILContext context)
    {
        ILCursor c = new(context);

        EnableSaintMechanic(c, skipDelegate: true);

        EnableSaintMechanicTwice(c);
    }

    private static void MartyrInitializeTongueILHook(ILContext context)
    {
        ILCursor c = new(context);

        EnableSaintMechanic(c);

        EnableSaintMechanic(c, skipDelegate: true);

        EnableSaintMechanic(c);
    }

    private static void PreventGhostSpawningILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(
            MoveType.After,
            static x => x.MatchLdsfld(typeof(GhostWorldPresence.GhostID).GetField(nameof(GhostWorldPresence.GhostID.NoGhost))),
            static x => x.MatchCall(typeof(ExtEnum<GhostWorldPresence.GhostID>).GetMethod("op_Inequality")),
            x => x.MatchBrfalse(out target)
        );

        c.Emit(OpCodes.Ldarg_0).EmitDelegate(IsMartyrClass);
        c.Emit(OpCodes.Brtrue, target);
    }

    private static void SaintFlyAscensionILHook(ILContext context)
    {
        ILCursor c = new(context);

        EnableSaintMechanic(c);

        c.GotoNext(MoveType.After, static x => x.MatchBrfalse(out _));

        ILCursor d = new(c);

        d.GotoNext(static x => x.MatchCall(typeof(Creature).GetProperty(nameof(Creature.grasps)).GetGetMethod()));
        d.GotoPrev();

        c.Emit(OpCodes.Ldarg_0).EmitDelegate(IsMartyr);
        c.Emit(OpCodes.Brtrue, d.MarkLabel());
    }

    private static void EnableSaintMechanic(ILContext context) => EnableSaintMechanic(new ILCursor(context));

    private static void EnableSaintMechanic(ILCursor c, bool skipDelegate = false)
    {
        c.GotoNext(
            static x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.SlugCatClass))),
            static x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint)))
        ).GotoNext();

        if (skipDelegate) return;

        c.EmitDelegate(ReplaceWithSaint);
    }

    private static void EnableSaintMechanicTwice(ILContext context) => EnableSaintMechanicTwice(new ILCursor(context));

    private static void EnableSaintMechanicTwice(ILCursor c, bool skipDelegate = false)
    {
        EnableSaintMechanic(c, skipDelegate);

        EnableSaintMechanic(c, skipDelegate);
    }

    private static bool IsMartyr(Player player) => IsMartyrClass(player.SlugCatClass);

    private static bool IsMartyrClass(SlugcatStats.Name slugcatClass) => slugcatClass == MartyrMain.Martyr;

    private static SlugcatStats.Name ReplaceWithSaint(SlugcatStats.Name name) =>
        IsMartyrClass(name) ? MoreSlugcatsEnums.SlugcatStatsName.Saint : name;
}