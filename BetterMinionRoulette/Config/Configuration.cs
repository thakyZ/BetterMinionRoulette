using System;
using System.Collections.Generic;
using System.Globalization;

using Dalamud.Configuration;

using Newtonsoft.Json;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;

[Serializable]
internal sealed class Configuration : IPluginConfiguration {
  private const int CONFIG_VERSION = 1;

  public int Version { get; set; }

  public bool Enabled { get; set; }

  public List<CharacterConfig> Characters { get; set; } = new();

  [JsonIgnore]
  public CharacterConfig? CurrentCharacter { get => Characters.Find(x => x.CharacterName == GetCurrentPlayerName() && x.CharacterWorld == GetCurrentPlayerWorld()); }

  [NonSerialized]
  private static Plugin? _plugin;

  public string DefaultGroupName { get; set; } = "Default";

  public void Initialize(Plugin plugin)
  {
      _plugin = plugin;
  }

  public static Configuration Init() {
    return new Configuration { Version = CONFIG_VERSION };
  }

  public static Configuration LoadOnLogin(Configuration config) {
    if (GetCurrentPlayerName() is null || GetCurrentPlayerWorld() is null) {
      return Init();
    }
    if (config.CurrentCharacter is null) {
      config.Characters.Add(new CharacterConfig() {
        CharacterName = GetCurrentPlayerName() ?? "null",
        CharacterWorld = GetCurrentPlayerWorld() ?? "null"
      });
    }
    Minions.Load(config);
    Minions.GetInstance(name: config.DefaultGroupName)!.Update(config.CurrentCharacter?.IncludeNewMinions == true);
    Minions.Remove(config.DefaultGroupName);
    return config;
  }

  public static string? GetCurrentPlayerName() {
    if (_plugin!.ClientState == null || _plugin!.ClientState.LocalPlayer == null || _plugin!.ClientState.LocalPlayer.Name == null) {
      return null;
    }

    return _plugin!.ClientState.LocalPlayer.Name.TextValue;
  }
  public static string? GetCurrentPlayerWorld() {
    if (_plugin!.ClientState == null || _plugin!.ClientState.LocalPlayer == null || _plugin!.ClientState.LocalPlayer.Name == null) {
      return null;
    }

    return _plugin!.ClientState.LocalPlayer.HomeWorld.Id.ToString(new CultureInfo("en-US"));
  }

  public MinionGroup? GetMinionGroup() {
    return new DefaultMinionGroup(this);
  }

  public void Migrate() {
    if (Version <= 0) {
      Version = 1;
    }

    // insert migration code here

    if (Version < CONFIG_VERSION) {
      throw new InvalidOperationException($"Missing migration to version {CONFIG_VERSION}");
    }
  }
}
