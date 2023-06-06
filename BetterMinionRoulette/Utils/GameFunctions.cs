using System;
using System.Runtime.CompilerServices;

using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal static class GameFunctions
{
    public static unsafe bool HasMinionUnlocked(uint id)
    {
        return UIState.Instance()->IsCompanionUnlocked(id);
    }

    public static unsafe bool IsPlayersOwnIsland()
    {
        var mjiManager = MJIManager.Instance();
        if (mjiManager is not null)
        {
            return mjiManager->IslandState.CanEditIsland;
        }
        return false;
    }

    public static unsafe Span<bool> RoamingMinionList(byte* roamingMinions)
    {
        return new(Unsafe.AsPointer(ref roamingMinions[0]), 496);
    }

    public static unsafe bool IsMinionOnIsland(uint id, bool _default)
    {
        var mjiManager = MJIManager.Instance();
        if (mjiManager is not null)
        {
            var mjiPastureHandler = MJIManager.Instance()->PastureHandler;
            if (mjiPastureHandler is not null && IsPlayersOwnIsland())
            {
                //return mjiPastureHandler->RoamingMinionsSpan[(int)id];
                return RoamingMinionList(mjiPastureHandler->RoamingMinions)[(int)id];
            }
        }
        return _default;
    }
}
