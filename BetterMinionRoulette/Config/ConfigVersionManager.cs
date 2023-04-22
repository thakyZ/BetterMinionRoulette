using System;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;
internal static class ConfigVersionManager {
  public static void DoMigration(Configuration config) {
    config.LoadedVersion = config.Version;
    if (config.Version <= 1) {
      config.Version = 2;

      string? defaultGroup = config.Enabled ? config.DefaultGroupName : null;
      config.MinionRouletteGroup = defaultGroup;
    }

    if (config.Version <= 2) {
      config.Version = 3;
      config.CharacterConfigs.Add(Configuration.DUMMY_LEGACY_CONFIG_ID, new CharacterConfigEntry { CharacterName = "Legacy Data", CharacterWorld = "" });
      config.NewCharacterHandling = Configuration.NewCharacterHandlingModes.ASK;
    }

    // insert migration code here

    if (config.Version < Configuration.CONFIG_VERSION) {
      throw new InvalidOperationException($"Missing migration to version {Configuration.CONFIG_VERSION}");
    }
  }
}
