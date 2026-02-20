using System;
using RWCustom;
using UnityEngine;

namespace ModLib.Objects;

/// <summary>
///     A fully-independent implementation of <c>SB_A14</c> and <see cref="MoreSlugcats.HRKarmaShrine"/>'s "void melt" effect,
///     where the screen becomes golden for a moment, before smoothly fading to its usual palette again.
/// </summary>
/// <remarks>
///     This object is purely visual, and has no effect on the player's Karma level.
/// </remarks>
public class FadingMeltLights : CosmeticSprite
{
    private RoomSettings.RoomEffect? meltEffect;
    private float effectInitLevel;

    private bool initialized;
    private bool forcedMeltEffect;

    private readonly float speed;
    private readonly float strength;

    /// <summary>
    ///     Creates a new instance of the <see cref="FadingMeltLights"/> class with the default effect strength and duration modifiers.
    /// </summary>
    public FadingMeltLights()
    {
        speed = 1f;
        strength = 1f;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="FadingMeltLights"/> class with the specified effect strength and duration modifiers.
    /// </summary>
    /// <param name="speed">The speed at which the effect disappears. Must be a positive, non-zero value.</param>
    /// <param name="strength">The initial strength of the effect; Ignored if the current room already has Void Melt. Must be a positive, non-zero value.</param>
    /// <exception cref="ArgumentOutOfRangeException">speed is zero or a negative value. -or- strength is zero or a negative value.</exception>
    public FadingMeltLights(float speed, float strength)
    {
        if (speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));

        if (strength <= 0) throw new ArgumentOutOfRangeException(nameof(strength));

        this.speed = speed;
        this.strength = strength;
    }

    /// <summary>
    ///     The progress of the fade effect, ranging from <c>1f</c> (strongest gold tint) to <c>0f</c> (no gold tint, effect is removed).
    /// </summary>
    public float FadeProgress { get; private set; }

    /// <inheritdoc/>
    public override void Destroy()
    {
        base.Destroy();

        if (this is { room: not null, meltEffect: not null, forcedMeltEffect: true })
        {
            room.roomSettings.effects.Remove(meltEffect);

            forcedMeltEffect = false;
        }

        meltEffect = null;
    }

    /// <inheritdoc/>
    public override void Update(bool eu)
    {
        base.Update(eu);

        if (!initialized)
        {
            Initialize();
            return;
        }

        FadeProgress = Mathf.Max(0f, FadeProgress - (0.016666668f * speed));
        meltEffect?.amount = Mathf.Lerp(effectInitLevel, 1f, Custom.SCurve(FadeProgress, 0.6f));

        if (FadeProgress <= 0f)
        {
            Destroy();
        }
    }

    /// <inheritdoc/>
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        if (initialized)
        {
            rCam.ApplyPalette();
        }
    }

    private void Initialize()
    {
        if (initialized || room is null) return;

        for (int i = 0; i < room.roomSettings.effects.Count; i++)
        {
            if (room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidMelt)
            {
                meltEffect = room.roomSettings.effects[i];
                effectInitLevel = meltEffect.amount;
                break;
            }
        }

        room.PlaySound(SoundID.SB_A14);

        if (meltEffect is null)
        {
            meltEffect = new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.VoidMelt, strength, false);

            room.roomSettings.effects.Add(meltEffect);

            forcedMeltEffect = true;
        }

        for (int i = 0; i < 20; i++)
        {
            room.AddObject(new MeltLights.MeltLight(1f, room.RandomPos(), room, RainWorld.GoldRGB));
        }
        FadeProgress = 1f;

        initialized = true;
    }
}