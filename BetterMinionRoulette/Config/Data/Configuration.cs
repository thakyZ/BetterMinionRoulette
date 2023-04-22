using System;
using System.Collections.Generic;

using Dalamud.Configuration;

using Newtonsoft.Json;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

[Serializable]
internal sealed class Configuration : IPluginConfiguration {
  public const int CONFIG_VERSION = 2;
  public const ulong DUMMY_LEGACY_CONFIG_ID = 0;
  public const string DEFAULT_GROUP_NAME = "Default";

  [Versions(introduced: 0)]
  public int Version { get; set; }

  [Versions(introduced: 1, removed: 2)]
  public bool Enabled { get; set; }

  [Versions(introduced: 0, removed: 1)]
  public HashSet<uint> EnabledMinions { get; set; } = new();

  [Versions(introduced: 2)]
  public Dictionary<ulong, CharacterConfigEntry> CharacterConfigs { get; set; } = new();

  [Versions(introduced: 2)]
  public int? NewCharacterHandling { get; set; }

  [Versions(introduced: 1, removed: 2)]
  public List<CharacterConfig> Characters { get; set; } = new();

  [Versions(introduced: 2)]
  public string DefaultGroupName { get; set; } = DEFAULT_GROUP_NAME;

  [JsonIgnore]
  public int LoadedVersion { get; set; }

  [JsonIgnore]
  public bool HasNonDefaultGroups => Groups.Count > 0;

  [Versions(introduced: 0, removed: 1)]
  public bool IncludeNewMinions { get; set; } = true;

  [Versions(introduced: 0, removed: 1)]
  public List<MinionGroup> Groups { get; set; } = new();

  [Versions(introduced: 0, removed: 1)]
  public string? MinionRouletteGroup { get; set; }

  public static Configuration Init() {
    return new Configuration { Version = CONFIG_VERSION, NewCharacterHandling = NewCharacterHandlingModes.BLANK };
  }

  public static class NewCharacterHandlingModes {
    public const int ASK = 0;
    public const int IMPORT = 1;
    public const int BLANK = 2;
  }
}

public class CharacterConfigEntry {
  private string? _characterName;
  private string? _characterWorld;

  public string CharacterName {
    get => _characterName ?? "INVALID CONFIG";
    set => _characterName = value;
  }

  public string CharacterWorld {
    get => _characterWorld ?? "INVALID CONFIG";
    set => _characterWorld = value;
  }

  public string FileName { get; set; } = "";
}
