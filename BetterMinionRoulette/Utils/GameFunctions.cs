using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Util;

internal static class GameFunctions {
  public static unsafe bool HasMinionUnlocked(uint id) {
    return UIState.Instance()->IsCompanionUnlocked(id);
  }
}
