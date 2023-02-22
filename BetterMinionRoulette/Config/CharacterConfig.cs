using System.Collections.Generic;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;

internal sealed class CharacterConfig {
  public string CharacterName { get; set; } = "";

  public string CharacterWorld { get; set; } = "";

  public bool IncludeNewMinions { get; set; } = true;

  public List<uint> EnabledMinions { get; set; } = new();

  public List<uint> IslandMinions { get; set; } = new();

  public bool OmitIslandMinions { get; set; } = true;

  public void CopyFrom(CharacterConfig other) {
    other.CharacterName = CharacterName;
    other.CharacterWorld = CharacterWorld;
    other.IncludeNewMinions = IncludeNewMinions;
    other.OmitIslandMinions = OmitIslandMinions;
    other.EnabledMinions = EnabledMinions;
    other.IslandMinions = IslandMinions;
  }
}
