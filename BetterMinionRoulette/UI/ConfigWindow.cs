using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class ConfigWindow : IWindow {
  private bool _isOpen;
  private readonly Plugin _plugin;
  private readonly Services _services;
  private string? _currentMinionGroup;
  private readonly MinionRenderer _minionRenderer;
  private readonly CharacterManagementRenderer _charManagementRenderer;
  private string _search = "";

  public ConfigWindow(Plugin plugin, Services services) {
    _plugin = plugin;
    _services = services;
    _minionRenderer = new MinionRenderer(_services);
    _charManagementRenderer = new CharacterManagementRenderer(services, _plugin.WindowManager, _plugin.CharacterManager, _plugin.Configuration);
  }

  public override int GetHashCode() {
    return 0;
  }

  public override bool Equals(object? obj) {
    return obj is ConfigWindow;
  }

  public void Open() {
    _isOpen = true;
    _plugin.MinionRegistry.RefreshUnlocked();
    _plugin.MinionRegistry.RefreshIsland();
  }

  public void Draw() {
    if (ImGui.Begin("Better Minion Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
      if (_plugin.CharacterConfig is not CharacterConfig characterConfig) {
        ImGui.Text("Please log in first");
      } else if (ImGui.BeginTabBar("settings")) {
        if (ImGui.BeginTabItem("General")) {
          string? minionRouletteGroupName = characterConfig.MinionRouletteGroup;

          SelectRouletteGroup(characterConfig, ref minionRouletteGroupName);
          ImGui.Text("For one of these to take effect, the selected group has to enable at least one minion.");

          characterConfig.MinionRouletteGroup = minionRouletteGroupName;

          bool omitIslandMinions = characterConfig.OmitIslandMinions;
          _ = ImGui.Checkbox("Omit on Island", ref omitIslandMinions);
          if (omitIslandMinions != characterConfig.OmitIslandMinions) {
            characterConfig.OmitIslandMinions = omitIslandMinions;
          }

          // backwards compatibility
          _plugin.Configuration.Enabled = (minionRouletteGroupName) is not null;
          ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Minion groups")) {
          MinionGroup minions = SelectCurrentGroup(characterConfig);
          DrawMinionGroup(minions, characterConfig);
          ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Character Management")) {
          _charManagementRenderer.Draw();
          ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
      }
    }

    ImGui.End();

    if (!_isOpen) {
      _plugin.CharacterManager.SaveCurrentCharacterConfig();
      _plugin.SaveConfig(_plugin.Configuration);
      _plugin.WindowManager.Close(this);
    }
  }

  private MinionGroup SelectCurrentGroup(CharacterConfig characterConfig) {
    if (_currentMinionGroup is not null && characterConfig.Groups.All(x => x.Name != _currentMinionGroup)) {
      _currentMinionGroup = null;
    }

    _currentMinionGroup ??= characterConfig.Groups[0].Name;

    SelectMinionGroup(characterConfig, ref _currentMinionGroup, "##currentGroup", 150);

    int mode = 0;
    const int MODE_ADD = 1;
    const int MODE_EDIT = 2;
    const int MODE_DELETE = 3;

    ImGui.SameLine();
    mode = ImGui.Button("Add") ? MODE_ADD : mode;
    ImGui.SameLine();
    mode = ImGui.Button("Edit") ? MODE_EDIT : mode;
    ImGui.SameLine();
    ImGui.BeginDisabled(!characterConfig.HasNonDefaultGroups);
    mode = ImGui.Button("Delete") ? MODE_DELETE : mode;
    ImGui.EndDisabled();

    string currentGroup = _currentMinionGroup;
    switch (mode) {
      case MODE_ADD:
        var dialog = new RenameItemDialog(_plugin.WindowManager, "Add a new group", "", x => AddGroup(characterConfig, x))
                {
          NormalizeWhitespace = true
        };

        dialog.SetValidation(x => ValidateGroup(x, isNew: true), _ => "A group with that name already exists.");
        _plugin.WindowManager.OpenDialog(dialog);
        break;
      case MODE_EDIT:
        dialog = new RenameItemDialog(_plugin.WindowManager, $"Rename {_currentMinionGroup}", _currentMinionGroup,
        (newName) => RenameMinionGroup(_currentMinionGroup, newName)) {
          NormalizeWhitespace = true
        };

        dialog.SetValidation(x => ValidateGroup(x, isNew: false), _ => "Another group with that name already exists.");

        _plugin.WindowManager.OpenDialog(dialog);
        break;
      case MODE_DELETE:
        _plugin.WindowManager.Confirm(
            "Confirm deletion of minion group",
            $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
            ("OK", () => DeleteMinionGroup(currentGroup)),
            "Cancel");
        break;
    }

    return characterConfig.GetMinionGroup(_currentMinionGroup)!;

    bool ValidateGroup(string newName, bool isNew) {
      if (_plugin.CharacterConfig is not { } characterConfig) {
        return false;
      }

      HashSet<string> names = new(characterConfig.Groups.Select(x => x.Name), StringComparer.InvariantCultureIgnoreCase);

      if (!isNew) {
        _ = names.Remove(currentGroup);
      }

      return !names.Contains(newName);
    }
  }

  private void DeleteMinionGroup(string name) {
    if (_plugin.CharacterConfig is not { } characterConfig) {
      return;
    }

    MinionGroupManager.Delete(characterConfig, name);

    if (_currentMinionGroup == name) {
      _currentMinionGroup = null;
    }
  }

  private void RenameMinionGroup(string currentMinionGroup, string newName) {
    if (_plugin.CharacterConfig is not { } characterConfig) {
      return;
    }

    MinionGroupManager.Rename(characterConfig, currentMinionGroup, newName);

    if (_currentMinionGroup == currentMinionGroup) {
      _currentMinionGroup = newName;
    }
  }

  private void AddGroup(CharacterConfig characterConfig, string name) {
    characterConfig.Groups.Add(new MinionGroup { Name = name });
    _currentMinionGroup = name;
  }

  private void DrawMinionGroup(MinionGroup group, CharacterConfig characterConfig) {
    if (group is null) {
      ImGui.Text("Group is null!");
      return;
    }

    bool enableNewMinions = !group.IncludedMeansActive;
    _ = ImGui.Checkbox("Enable new minions on unlock", ref enableNewMinions);

    List<MinionData> unlockedMinions = _plugin.MinionRegistry.GetUnlockedMinions(characterConfig.OmitIslandMinions);
    if (enableNewMinions == group.IncludedMeansActive) {
      // we auto-enable new minions by tracking which minions are explicitly disabled
      group.IncludedMeansActive = !enableNewMinions;

      // invert selection
      var unlockedMinionIDs = unlockedMinions.Select(x => x.ID).ToHashSet();
      unlockedMinionIDs.ExceptWith(group.IncludedMinions);
      group.IncludedMinions.Clear();
      group.IncludedMinions.UnionWith(unlockedMinionIDs);
    }

    _ = ImGui.InputText("Search", ref _search, 64);
    unlockedMinions = _plugin.MinionRegistry.Filter(unlockedMinions, _search, characterConfig.OmitIslandMinions);

    int pages = MinionRenderer.GetPageCount(unlockedMinions.Count);
    if (pages == 0 && unlockedMinions.Count == 0 && string.IsNullOrEmpty(_search)) {
      ImGui.Text("Please unlock at least one minion.");
    } else if (pages == 0 && !string.IsNullOrEmpty(_search)) {
      ImGui.Text("Search did not turn up any minions.");
    } else if (ImGui.BeginTabBar("minion_pages")) {
      for (int page = 1; page <= pages; page++) {
        if (ImGui.BeginTabItem($"{page}")) {
          _minionRenderer.RenderPage(unlockedMinions, group.IncludedMinions, group.IncludedMeansActive, page);

          int currentPage = page;
          (bool Select, int? Page)? maybeInfo =
                        Buttons("Select all", "Unselect all", "Select page", "Unselect page") switch
                        {
                          0 => (true, default(int?)),
                          1 => (false, default(int?)),
                          2 => (true, page),
                          3 => (false, page),
                          _ => default((bool, int?)?),
                        };

          if (maybeInfo is { } info) {
            string selectText = info.Select ? "select" : "unselect";
            string pageInfo = (info.Page, info.Select) switch
                        {
                          (null, true) => "currently unselected minions",
                          (null, false) => "currently selected minions",
                          _ => "minions on the current page",
                        };
            #pragma warning disable IDE0053 // Use expression body for lambda expressions
            // commented-out code needs to be preserved (for now)
            _plugin.WindowManager.ConfirmYesNo(
                "Are you sure?",
                $"Do you really want to {selectText} all {pageInfo}?",
                () => {
                  MinionRenderer.Update(
                                  _plugin.MinionRegistry.GetUnlockedMinions(characterConfig.OmitIslandMinions),
                                  group.IncludedMinions,
                                  info.Select == group.IncludedMeansActive,
                                  info.Page);
                });
            #pragma warning restore IDE0053 // Use expression body for lambda expressions
          }

          ImGui.EndTabItem();
        }
      }

      ImGui.SameLine();

      ImGui.EndTabBar();
    }
  }

  private static void SelectRouletteGroup(CharacterConfig characterConfig, ref string? groupName) {
    bool isEnabled = groupName is not null;
    _ = ImGui.Checkbox("Enable for Minion Roulette", ref isEnabled);

    if (isEnabled) {
      groupName ??= characterConfig.Groups.FirstOrDefault()?.Name;

      if (groupName is not null) {
        ImGui.SameLine();
        SelectMinionGroup(characterConfig, ref groupName, "##rouletteGroup_g", 100);
      }
    } else {
      groupName = null;
    }
  }

  private static void SelectMinionGroup(CharacterConfig characterConfig, ref string groupName, string label, float? width = null) {
    if (width is float w) {
      ImGui.SetNextItemWidth(w);
    }

    if (ImGui.BeginCombo(label, groupName)) {
      foreach (MinionGroup group in characterConfig.Groups) {
        if (ImGui.Selectable(group.Name, group.Name == groupName)) {
          groupName = group.Name;
        }
      }

      ImGui.EndCombo();
    }
  }

  private static int? Buttons(params string[] buttons) {
    int? result = null;
    for (int i = 0; i < buttons.Length; ++i) {
      if (i > 0) {
        ImGui.SameLine();
      }

      if (ImGui.Button(buttons[i])) {
        result = i;
      }
    }

    return result;
  }
}
