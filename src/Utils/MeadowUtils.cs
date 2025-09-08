using MyMod.Utils.Meadow;
using RainMeadow;

namespace MyMod.Utils;

public static class MeadowUtils
{
    public static bool IsOnline => OnlineManager.lobby is not null;
    public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

    public static bool IsGameMode(MeadowGameModes gameMode)
    {
        if (!IsOnline) return false;

        int gamemode = OnlineManager.lobby.gameMode switch
        {
            MeadowGameMode => 0,
            StoryGameMode => 1,
            ArenaOnlineGameMode => 2,
            _ => -1
        };

        return gamemode == (int)gameMode;
    }

    public static bool IsMine(PhysicalObject physicalObject) => physicalObject.IsLocal();

    public static void RequestOptionsSync()
    {
        if (!IsOnline || IsHost) return;

        OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.RequestRemixOptionsSync, OnlineManager.mePlayer);
    }

    public static void RequestOwnership(PhysicalObject physicalObject)
    {
        try
        {
            MyLogger.LogDebug($"Requesting ownership of {physicalObject}...");

            physicalObject.abstractPhysicalObject.GetOnlineObject()?.Request();

            MyLogger.LogDebug($"New owner is: {physicalObject.abstractPhysicalObject.GetOnlineObject()?.owner}");
        }
        catch (System.Exception ex)
        {
            MyLogger.LogError($"Failed to request ownership of {physicalObject}!", ex);
        }
    }

    public enum MeadowGameModes
    {
        Meadow,
        Story,
        Arena,
        Custom = -1
    }
}