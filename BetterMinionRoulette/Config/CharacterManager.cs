using System.Collections.Generic;
using System.IO;

using Dalamud.Game.ClientState.Objects.SubKinds;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

using Newtonsoft.Json;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;

internal sealed class CharacterManager {
  private CharacterConfig? _characterConfig;
  private ulong? _playerID;

  public CharacterConfig GetCharacterConfig(ulong playerID, IPlayerCharacter character) {
    if (_characterConfig is { } cfg && playerID == _playerID) {
      return cfg;
    }

    _playerID = playerID;
    if (Services.Configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce)) {
      _characterConfig = LoadCharacterConfig(cce);
    }

    if (_characterConfig is null) {
      _characterConfig = CreateCharacterConfig();
      cce = new CharacterConfigEntry {
        CharacterName = character.Name.TextValue,
        CharacterWorld = character.HomeWorld.IsValid ? character.HomeWorld.Value.Name.ExtractText() : string.Empty,
      };

      cce.FileName = $"{playerID}_{cce.CharacterName.Replace(' ', '_')}@{cce.CharacterWorld}.json";
      Services.Configuration.CharacterConfigs[playerID] = cce;

      SaveCurrentCharacterConfig(cce);
      Services.Interface.SavePluginConfig(Services.Configuration);
    }

    return _characterConfig;
  }

  public bool Import(ulong fromPlayerID) {
    Services.Log.Debug($"Importing {fromPlayerID}");
    if (fromPlayerID == _playerID || _playerID is not ulong currentPlayer) {
      Services.Log.Debug("No use importing from current character");
      // importing from yourself is a noop and should therefore always succeed
      return true;
    }

    CharacterConfig? characterConfig = LoadCharacterConfig(fromPlayerID);
    if (characterConfig is null || _characterConfig is null) {
      List<string> items = new();
      if (characterConfig is null) {
        items.Add("imported config is null");
      }

      if (_characterConfig is null) {
        items.Add("current config is null");
      }

      Services.Log.Debug($"Unable to import: {string.Join(", ", items)}");

      return false;
    }

    CharacterConfigEntry cce = Services.Configuration.CharacterConfigs[currentPlayer];

    _characterConfig.CopyFrom(characterConfig);
    SaveCurrentCharacterConfig(cce);

    Services.Log.Debug("Import successful");
    return true;
  }

  public void SaveCurrentCharacterConfig() {
    if (_playerID is not ulong playerID) {
      return;
    }

    CharacterConfigEntry cce = Services.Configuration.CharacterConfigs[playerID];
    SaveCurrentCharacterConfig(cce);
  }

  private void SaveCharacterConfig(CharacterConfigEntry entry, CharacterConfig config) {
    string dir = GetCharConfigDir();
    if (!Directory.Exists(dir)) {
      _ = Directory.CreateDirectory(dir);
    }

    File.WriteAllText(Path.Combine(dir, entry.FileName), JsonConvert.SerializeObject(config));
  }

  private CharacterConfig? LoadCharacterConfig(ulong playerID) {
    if (Services.Configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce)) {
      CharacterConfig? res = playerID == Configuration.DUMMY_LEGACY_CONFIG_ID
                                       ? LoadCharacterConfig(cce) //LoadLegacyCharacterConfig()
                                       : LoadCharacterConfig(cce);
      if (res is not null) {
        return res;
      }
    }

    return null;
  }

  /*private CharacterConfig LoadLegacyCharacterConfig() {
CharacterConfig result = new() {
  MinionRouletteGroup = _configuration.MinionRouletteGroup,
};

var reg = new MinionRegistry(_services);
reg.RefreshUnlocked();
reg.RefreshIsland();
var allMinions = reg.GetUnlockedMinions(result.OmitIslandMinions).Select(x => x.ID).ToHashSet();

AddGroup(
    result.Groups,
    allMinions,
    _configuration.DefaultGroupName,
    !_configuration.IncludeNewMinions,
    _configuration.EnabledMinions);

foreach (MinionGroup group in _configuration.Groups) {
  // "IncludeNewMinions" meant we would just save all non-unlocked minions as enabled
  // while now we would just save all disabled minions instead
  AddGroup(result.Groups, allMinions, group.Name, !group.IncludedMeansActive, group.IncludedMinions);
}

return result;

static void AddGroup(
    List<MinionGroup> groups,
    HashSet<uint> allMinions,
    string name,
    bool includedMeansActive,
    HashSet<uint> includedMinions) {
  MinionGroup newGroup = new()
        {
    IncludedMeansActive = includedMeansActive,
    Name = name,
  };

  groups.Add(newGroup);
  if (newGroup.IncludedMeansActive /* Previously "IncludeNewMinions" *//*) {
    newGroup.IncludedMinions.UnionWith(includedMinions);
  } else {
    // "IncludeNewMinions" meant we would just save all non-unlocked minions as enabled
    // so now we just save all disabled minions instead
    newGroup.IncludedMinions.UnionWith(allMinions);
    newGroup.IncludedMinions.ExceptWith(includedMinions);
  }
}
}*/

  private void SaveCurrentCharacterConfig(CharacterConfigEntry entry) {
    if (_characterConfig is { } charConfig) {
      SaveCharacterConfig(entry, charConfig);
    }
  }

  private CharacterConfig? LoadCharacterConfig(CharacterConfigEntry cce) {
    if (cce.FileName is not null /* can still be null if freshly loaded */) {
      string path = Path.Combine(GetCharConfigDir(), cce.FileName);

      if (File.Exists(path)) {
        try {
          return JsonConvert.DeserializeObject<CharacterConfig>(File.ReadAllText(path));
        } catch (IOException /* file deleted in the meantime. shouldn't happen, but technically can */) {
        }
      }
    }

    return null;
  }

  private CharacterConfig CreateCharacterConfig() {
    // TODO: make defaults
    return new() {
      Groups = new() {
        new() {
          Name = Configuration.DEFAULT_GROUP_NAME,
        }
      },
      IsNew = true,
    };
  }

  private string GetCharConfigDir() {
    return Services.Interface.GetPluginConfigDirectory();
  }
}
