using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using System.Runtime.CompilerServices;
using System;

using FFXIVClientStructs.FFXIV.Client.Game.UI;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal static class GameFunctions {
  public static unsafe bool HasMinionUnlocked(uint id) {
    return UIState.Instance()->IsCompanionUnlocked(id);
  }

  public static unsafe bool IsPlayersOwnIsland() {
    var mjiManager = MJIManager.Instance();
    if (mjiManager is not null) {
      return mjiManager->IslandState.CanEditIsland;
    }
    return false;
  }

  public static unsafe Span<bool> RoamingMinionList(byte* roamingMinions) {
    return new(Unsafe.AsPointer(ref roamingMinions[0]), 480);
  }

  public static unsafe bool IsMinionOnIsland(MinionData minion, bool _default) {
    var mjiPastureHandler = MJIManager.Instance()->PastureHandler;
    if (mjiPastureHandler is not null && IsPlayersOwnIsland()) {
      return RoamingMinionList(mjiPastureHandler->RoamingMinions)[(int)minion.ID];
    }
    return _default;
  }
}
