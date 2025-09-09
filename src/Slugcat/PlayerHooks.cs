using System.Runtime.CompilerServices;
using MoreSlugcats;
using MyMod.Utils;
using MyMod.Utils.Options;
using UnityEngine;

namespace MyMod.Slugcat;

public static class PlayerHooks
{
    private static readonly ConditionalWeakTable<Player, PlayerColor> PlayerColors = new();

    private static readonly SlugcatStats.Name Prototype = new("Prototype");

    public static void AddHooks()
    {
        On.Player.Update += PlayerUpdateHook;
        On.PlayerGraphics.DrawSprites += RecolorSlugcatHook;
    }

    public static void RemoveHooks()
    {
        On.Player.Update -= PlayerUpdateHook;
        On.PlayerGraphics.DrawSprites -= RecolorSlugcatHook;
    }

    private static void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig.Invoke(self, eu);

        if (InputHandler.IsKeyPressed(self, InputHandler.Keys.EXPLODE)) // Using a registered keybind
        {
            if (!OptionUtils.IsOptionEnabled(Options.CAN_EXPLODE_SELF)) return; // Testing for a server-side option

            if (self.dead || self.room is null) return;

            if (CompatibilityManager.IsModEnabled("slime-cubed.slugbase") // Checking for a mod's presence
                && self.SlugCatClass == Prototype)
            {
                // Reuse ExplodeOnDeath feature
                self.Die();
            }
            else
            {
                // Doing things the alternative way
                ScavengerBomb scavBomb = new(
                    new(
                        self.room.world,
                        AbstractPhysicalObject.AbstractObjectType.ScavengerBomb,
                        null,
                        self.abstractCreature.pos,
                        self.room.world.game.GetNewID()
                    ),
                    self.room.world
                );

                scavBomb.abstractPhysicalObject.RealizeInRoom();

                scavBomb.Explode(self.mainBodyChunk);
            }
        }
    }

    private static void RecolorSlugcatHook(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (OptionUtils.IsClientOptionValue(Options.SLUGCAT_FASHION, "overhauled")) // Testing for a client-side option
        {
            Color nextColor = CalculateNextColor(self.player);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i == 9) continue;

                sLeaser.sprites[i].color = nextColor;
            }
        }

        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
    }

    // An over-engineered example of a client-only feature.
    private static Color CalculateNextColor(Player player)
    {
        PlayerColor playerColor = PlayerColors.GetValue(player, _ =>
            new(
                PlayerGraphics.SlugcatColor(player.SlugCatClass),
                PlayerGraphics.SlugcatColor(GetRandomSlugcat())
            )
        );

        if (player.dead)
            playerColor.TargetColor = PlayerGraphics.SlugcatColor(player.SlugCatClass);

        return Color.Lerp(playerColor.CurrentColor, playerColor.TargetColor, playerColor.GetTimeStacker());
    }

    public static SlugcatStats.Name GetRandomSlugcat()
    {
        int maxIndex = ModManager.MSC ? 9 : 4;

        if (ModManager.MSC && Random.value <= 0.5f) maxIndex++;

        return Random.Range(0, maxIndex) switch
        {
            9 => MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel,
            8 => MoreSlugcatsEnums.SlugcatStatsName.Saint,
            7 => MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
            6 => MoreSlugcatsEnums.SlugcatStatsName.Spear,
            5 => MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
            4 => MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            3 => SlugcatStats.Name.Night,
            2 => SlugcatStats.Name.Red,
            1 => SlugcatStats.Name.Yellow,
            _ => SlugcatStats.Name.White
        };
    }

    private record PlayerColor(Color CurrentColor, Color TargetColor)
    {
        public Color CurrentColor { get; set; } = CurrentColor;
        public Color TargetColor { get; set; } = TargetColor;

        private float TimeStacker;

        public float GetTimeStacker(float delta = 0.025f)
        {
            TimeStacker += delta;

            if (TimeStacker > 1f)
            {
                TimeStacker = 0f;

                CurrentColor = TargetColor;
                TargetColor = PlayerGraphics.SlugcatColor(GetRandomSlugcat());
            }

            return TimeStacker;
        }
    }
}