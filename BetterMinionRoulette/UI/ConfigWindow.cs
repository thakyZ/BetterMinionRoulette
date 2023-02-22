using BetterMinionRoulette.Config;

using ImGuiNET;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class ConfigWindow : IWindow {
  private bool _isOpen;
  private readonly Plugin _plugin;
  private string? _currentMinionGroup;
  private string _search = "";

  public ConfigWindow(Plugin plugin) {
    _plugin = plugin;
  }

  public override int GetHashCode() {
    return 0;
  }

  public override bool Equals(object? obj) {
    return obj is ConfigWindow;
  }

  public void Open() {
    _isOpen = true;
    Minions.RefreshUnlocked();
  }

  public void Draw() {
    if (ImGui.Begin("Better Minion Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
      if (ImGui.BeginTabBar("settings")) {
        if (ImGui.BeginTabItem("General")) {
          bool enablePlugin =  _plugin.Configuration.Enabled;
          _ = ImGui.Checkbox("Enable", ref enablePlugin);

          if (enablePlugin != _plugin.Configuration.Enabled) {
            _plugin.Configuration.Enabled = enablePlugin;
          }
          ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Minions")) {
          if (!_plugin.ClientState.IsLoggedIn) {
            ImGui.Text("Please log in first");
          } else {
            var Minions = SelectCurrentGroup();
            DrawMinionGroup(Minions);
            ImGui.EndTabItem();
          }

          ImGui.EndTabBar();
        }
      }
    }

    ImGui.End();

    if (!_isOpen) {
      Plugin.SaveConfig(_plugin.Configuration);
      _plugin.WindowManager.Close(this);
    }
  }

  private MinionGroup SelectCurrentGroup() {
    var currentGroup = _currentMinionGroup;
    _currentMinionGroup = _plugin.Configuration.DefaultGroupName;

    if (_currentMinionGroup != currentGroup) {
      Minions.GetInstance(_currentMinionGroup)!.Filter(false, null, null);
    }

    return new DefaultMinionGroup(_plugin.Configuration);
  }

  private void DrawMinionGroup(MinionGroup group) {
    if (group is null) {
      ImGui.Text("Group is null!");
      return;
    }
    if (_plugin.Configuration.CurrentCharacter is null) {
      ImGui.Text("Current Character is null!");
      return;
    }

    var minions = Minions.GetInstance(group.Name)!;

    if (minions is null) {
      ImGui.Text($"Unable to load minions for group {group.Name}!");
      return;
    }

    bool enableNewMinions =  _plugin.Configuration.CurrentCharacter.IncludeNewMinions;
    _ = ImGui.Checkbox("Enable on unlock", ref enableNewMinions);

    if (enableNewMinions != _plugin.Configuration.CurrentCharacter.IncludeNewMinions) {
      _plugin.Configuration.CurrentCharacter.IncludeNewMinions = enableNewMinions;
      minions.UpdateUnlocked(enableNewMinions);
    }

    ImGui.SameLine();

    bool omitIslandMinions =  _plugin.Configuration.CurrentCharacter.OmitIslandMinions;
    _ = ImGui.Checkbox("Omit on Island", ref omitIslandMinions);

    if (omitIslandMinions != _plugin.Configuration.CurrentCharacter.OmitIslandMinions) {
      _plugin.Configuration.CurrentCharacter.OmitIslandMinions = omitIslandMinions;
    }

    if (ImGui.InputText("Search", ref _search, 64)) {
    }
    Minions.GetInstance(group.Name)!.Filter(false, null, _search, omitIslandMinions);

    var pages = minions.PageCount;
    if (pages == 0 && minions.ItemCount == 0 && string.IsNullOrEmpty(_search)) {
      ImGui.Text("Please unlock at least one minion.");
    } else if (pages == 0 && !string.IsNullOrEmpty(_search)) {
      ImGui.Text("Search did not turn up any minions.");
    } else if (ImGui.BeginTabBar("minion_pages")) {
      for (int page = 1; page <= pages; page++) {
        if (ImGui.BeginTabItem($"{page}##minion_tab_{page}")) {
          minions.RenderItems(page);
          minions.Save(group);

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
                          (null, true) => "currently unselected Minions",
                          (null, false) => "currently selected Minions",
                          _ => "Minions on the current page",
                        };
            _plugin.WindowManager.ConfirmYesNo(
                "Are you sure?",
                $"Do you really want to {selectText}select all {pageInfo}?",
                () => {
                  minions.Update(info.Select, info.Page);
                  minions.Save(group);
                });
          }

          ImGui.EndTabItem();
        }
      }

      ImGui.SameLine();

      ImGui.EndTabBar();
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
