using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace BetterMinionRoulette.Util;

internal static class GameFunctions {
  public static unsafe bool HasMinionUnlocked(uint id) {
    return UIState.Instance()->IsCompanionUnlocked(id);
  }
}
