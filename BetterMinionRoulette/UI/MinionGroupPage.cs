using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class MinionGroupPage
{
    private readonly MinionRenderer _minionRenderer;
    private string? _currentMinionGroup;
    private bool omitIsland;

    internal MinionGroupPage()
    {
        _minionRenderer = new MinionRenderer();
    }

    public void RenderPage(CharacterConfig characterConfig)
    {
        MinionGroup minions = SelectCurrentGroup(characterConfig);
        omitIsland = characterConfig.OmitIslandMinions;
        DrawMinionGroup(minions);
    }

    private void DrawMinionGroup(MinionGroup group)
    {
        if (group is null)
        {
            ImGui.Text("Group is null!");
            return;
        }

        RenderGroupSettings(group, out bool enableNewMinions);
        List<MinionData> unlockedMinions = Services.MinionRegistry.GetUnlockedMinions(omitIsland);
        UpdateMinionSelectionData(group, unlockedMinions, enableNewMinions);

        int pages = MinionRenderer.GetPageCount(Services.MinionRegistry.UnlockedMinionCount);
        if (pages == 0) {
            ImGui.Text("Please unlock at least one minion.");
        } else if (ImGui.BeginTabBar("minion_pages")) {
            for (int page = 1; page <= pages; page++) {
                if (ImGui.BeginTabItem($"{page}")) {
                    RenderMinionListPage(page, group, unlockedMinions);
                    ImGui.EndTabItem();
                }
            }

            ImGui.SameLine();
            ImGui.EndTabBar();
        }
    }

    private void RenderMinionListPage(int page, MinionGroup group, List<MinionData> unlockedMinions) {
        _minionRenderer.RenderPage(unlockedMinions, group, page);

        int currentPage = page;
        (bool Select, int? Page)? maybeInfo = ControlHelper.Buttons("Select all", "Unselect all", "Select page", "Unselect page") switch {
            0 => (true, default(int?)),
            1 => (false, default(int?)),
            2 => (true, page),
            3 => (false, page),
            _ => default((bool, int?)?),
        };

        if (maybeInfo is { } info) {
            string selectText = info.Select ? "select" : "unselect";
            string pageInfo = (info.Page, info.Select) switch {
                (null, true) => "currently unselected minions",
                (null, false) => "currently selected minions",
                _ => "minions on the current page",
            };

          Services.WindowManager.ConfirmYesNo("Are you sure?", $"Do you really want to {selectText} all {pageInfo}?",
            () => MinionRenderer.Update(Services.MinionRegistry.GetUnlockedMinions(omitIsland), group, info.Select, info.Page));
        }
    }

    private static void RenderGroupSettings(MinionGroup group, out bool enableNewMinions) {
        enableNewMinions = !group.IncludedMeansActive;

        _ = ImGui.Checkbox("Enable new minions on unlock", ref enableNewMinions);
    }

    private static void UpdateMinionSelectionData(MinionGroup group, List<MinionData> unlockedMinions, bool enableNewMinions) {
        if (enableNewMinions == group.IncludedMeansActive) {
            // we auto-enable new minions by tracking which minions are explicitly disabled
            group.IncludedMeansActive = !enableNewMinions;

            // invert selection
            var unlockedMinionIDs = unlockedMinions.Select(x => x.ID).ToHashSet();
            unlockedMinionIDs.ExceptWith(group.IncludedMinions);
            group.IncludedMinions.Clear();
            group.IncludedMinions.UnionWith(unlockedMinionIDs);
        }
    }

    private MinionGroup SelectCurrentGroup(CharacterConfig characterConfig) {
        if (_currentMinionGroup is not null && characterConfig.Groups.All(x => x.Name != _currentMinionGroup)) {
            _currentMinionGroup = null;
        }

        _currentMinionGroup ??= characterConfig.Groups[0].Name;

        ControlHelper.SelectItem(characterConfig.Groups, x => x.Name, ref _currentMinionGroup, "##currentgroup", 150);

        string currentGroup = _currentMinionGroup;
        ImGui.SameLine();
        if (ImGui.Button("Add")) {
            var dialog = new RenameItemDialog("Add a new group", string.Empty, x => AddMinionGroup(characterConfig, x)) {
                NormalizeWhitespace = true
            };

            dialog.SetValidation(x => ValidateGroup(x, isNew: true), _ => "A group with that name already exists.");
            Services.WindowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        if (ImGui.Button("Edit")) {
            var dialog = new RenameItemDialog($"Rename {_currentMinionGroup}", _currentMinionGroup, (newName) => RenameMinionGroup(_currentMinionGroup, newName)) {
                NormalizeWhitespace = true
            };

            dialog.SetValidation(x => ValidateGroup(x, isNew: false), _ => "Another group with that name already exists.");

            Services.WindowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!characterConfig.HasNonDefaultGroups);
        if (ImGui.Button("Delete")) {
            Services.WindowManager.Confirm(
              "Confirm deletion of minion group",
              $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
              ("OK", () => DeleteMinionGroup(currentGroup)),
              "Cancel");
        }

        ImGui.EndDisabled();

        return characterConfig.GetMinionGroup(_currentMinionGroup)!;

        bool ValidateGroup(string newName, bool isNew) {
            if (Services.PluginInstance.CharacterConfig is not { } characterConfig) {
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
        if (Services.PluginInstance.CharacterConfig is not { } characterConfig) {
            return;
        }

        MinionGroupManager.Delete(characterConfig, name);

        if (_currentMinionGroup == name) {
           _currentMinionGroup = null;
        }
    }

    private void RenameMinionGroup(string currentMinionGroup, string newName) {
      if (Services.PluginInstance.CharacterConfig is not { } characterConfig) {
          return;
      }

      MinionGroupManager.Rename(characterConfig, currentMinionGroup, newName);

      if (_currentMinionGroup == currentMinionGroup) {
          _currentMinionGroup = newName;
      }
    }

    private void AddMinionGroup(CharacterConfig characterConfig, string name) {
        characterConfig.Groups.Add(new MinionGroup { Name = name });
        _currentMinionGroup = name;
    }
}
