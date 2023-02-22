using System.Collections.Generic;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;

internal class MinionGroup {
  public virtual string Name { get; set; } = "";

  public virtual List<uint> EnabledMinions { get; set; } = new();
  public virtual List<uint> IslandMinions { get; set; } = new();
}

internal sealed class DefaultMinionGroup : MinionGroup {
  private readonly Configuration _config;

  public DefaultMinionGroup(Configuration config) {
    _config = config;
  }

  public override string Name => _config.DefaultGroupName;

  public override List<uint> EnabledMinions {
    get => _config.CurrentCharacter is not null ? _config.CurrentCharacter.EnabledMinions : new();
    set {
      if (_config.CurrentCharacter is not null) {
        _config.CurrentCharacter.EnabledMinions = value;
      }
      _ = value;
    }
  }

  public override List<uint> IslandMinions {
    get => _config.CurrentCharacter is not null ? _config.CurrentCharacter.IslandMinions : new();
    set {
      if (_config.CurrentCharacter is not null) {
        _config.CurrentCharacter.IslandMinions = value;
      }
      _ = value;
    }
  }
}
