using System;
using System.Linq;

using ImGuiNET;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class ConfigWindow : IWindow {
  private bool _isOpen;
  private readonly MinionGroupPage  _minionGroupPage;
  private readonly CharacterManagementRenderer _charManagementRenderer;

  public ConfigWindow() {
    _minionGroupPage = new MinionGroupPage();
    _charManagementRenderer = new CharacterManagementRenderer();
  }

  public override int GetHashCode() {
    return 0;
  }

  public override bool Equals(object? obj) {
    return obj is ConfigWindow;
  }

  public void Open() {
    _isOpen = true;
    Services.MinionRegistry.RefreshUnlocked();
    Services.MinionRegistry.RefreshIsland();
  }

  public void Draw() {
    if (ImGui.Begin("Better Minion Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
      if (Services.PluginInstance.CharacterConfig is not CharacterConfig characterConfig) {
        ImGui.Text("Please log in first");
      } else if (ImGui.BeginTabBar("settings")) {
        Tab("General", GeneralConfigTab);
        Tab("Minion Groups", _minionGroupPage.RenderPage);
        Tab("Character Management", _ => _charManagementRenderer.Draw());

        ImGui.EndTabBar();

        // Helper method for reducing boilerplate
        void Tab(string name, Action<CharacterConfig> contentSelector) {
          if (ImGui.BeginTabItem(name)) {
            contentSelector(characterConfig);
            ImGui.EndTabItem();
          }
        }
      }
    }

    ImGui.End();

    if (!_isOpen) {
      Services.CharacterManager.SaveCurrentCharacterConfig();
      Services.Configuration.SaveConfig();
      Services.WindowManager.Close(this);
    }
  }

  private void GeneralConfigTab(CharacterConfig characterConfig) {
    string? minionRouletteGroupName = characterConfig.MinionRouletteGroup;

    SelectRouletteGroup(characterConfig, ref minionRouletteGroupName);
    ImGui.Text("For one of these to take effect, the selected group has to enable at least one minion.");

    characterConfig.MinionRouletteGroup = minionRouletteGroupName;

    // backwards compatibility
    Services.Configuration.Enabled = minionRouletteGroupName is not null;
  }

  private static void SelectRouletteGroup(CharacterConfig characterConfig, ref string? groupName) {
    bool isEnabled = groupName is not null;

    if (isEnabled) {
      groupName ??= characterConfig.Groups.FirstOrDefault()?.Name;

      if (groupName is not null) {
        ImGui.SameLine();
        SelectMinionGroup(characterConfig, ref groupName);
      }
    } else {
      groupName = null;
    }
    static void SelectMinionGroup(CharacterConfig config, ref string group) {
      ControlHelper.SelectItem(
          config.Groups,
          x => x.Name,
          ref group,
          "##roulettegroup", 100);
    }
  }
}
