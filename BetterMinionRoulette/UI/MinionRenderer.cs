using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class MinionRenderer {
  private const int PAGE_SIZE = COLUMNS * ROWS;
  private const int COLUMNS = 5;
  private const int ROWS = 6;

  private static nint? _selectedUnselectedIcon;
  private static nint? _selectedIslandIcon;

  public void RenderPage(List<MinionData> minions, MinionGroup group, int page) {
    int i = 0;
    foreach (MinionData minion in minions.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE)) {
      if (i++ > 0) {
        ImGui.SameLine();
      }

      if (i >= COLUMNS) {
        i = 0;
      }

      bool enabled = group.IncludedMinions.Contains(minion.ID) == group.IncludedMeansActive;
      enabled = Render(minion, enabled);
      if (enabled == group.IncludedMeansActive) {
        _ = group.IncludedMinions.Add(minion.ID);
      } else {
        _ = group.IncludedMinions.Remove(minion.ID);
      }
    }
  }

  public static void Update(List<MinionData> minions, MinionGroup group, bool selected, int? page) {
    IEnumerable<MinionData> filteredMinions = minions;
    if (page is not null) {
      filteredMinions = filteredMinions.Skip((page.Value - 1) * PAGE_SIZE).Take(PAGE_SIZE);
    }

    HashSet<uint> selectedMinions = group.IncludedMinions;

    Func<uint, bool> selectOperation = selected == group.IncludedMeansActive ? selectedMinions.Add : selectedMinions.Remove;
    foreach (MinionData minion in filteredMinions) {
      _ = selectOperation(minion.ID);
    }
  }

  public static int GetPageCount(int minionCount) {
    return (minionCount / PAGE_SIZE) + (minionCount % PAGE_SIZE == 0 ? 0 : 1);
  }

  public bool Render(MinionData minionData, bool enabled) {
    _selectedUnselectedIcon ??= Services.TextureHelper.LoadUldTexture("readycheck");
    _selectedIslandIcon ??= Services.TextureHelper.LoadUldTexture("mjiminionnotebookmark");

    nint minionIcon = minionData.GetIcon();

    _ = ImGui.TableNextColumn();

    Vector2 originalPos = ImGui.GetCursorPos();

    const float BUTTON_SIZE = 60f;
    const float OVERLAY_SIZE = 24f;
    const float OVERLAY_OFFSET = 4f;
    var buttonSize = new Vector2(BUTTON_SIZE);
    var overlaySize = new Vector2(OVERLAY_SIZE);

    ImGui.PushStyleColor(ImGuiCol.Button, 0);
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

    if (ImGui.ImageButton(minionIcon, buttonSize, Vector2.Zero, Vector2.One, 0)) {
      enabled ^= true;
    }

    ImGui.PopStyleColor(3);

    if (ImGui.IsItemHovered()) {
      ImGui.SetTooltip(minionData.Name);
    }

    Vector2 finalPos = ImGui.GetCursorPos();

    // calculate overlay (bottom right corner) position
    Vector2 overlayPos2 = originalPos + new Vector2(buttonSize.X - overlaySize.X + OVERLAY_OFFSET, (buttonSize.X - overlaySize.X) + OVERLAY_OFFSET);
    ImGui.SetCursorPos(overlayPos2);

    Vector2 offset3 = new(minionData.Island ? 0.55f : 0.0f, 0.15f);
    Vector2 offset4 = new(minionData.Island ? 0.95f : 0.0f, 0.95f);
    ImGui.Image(_selectedIslandIcon!.Value, overlaySize, offset3, offset4);

    // calculate overlay (top right corner) position
    Vector2 overlayPos1 = originalPos + new Vector2(buttonSize.X - overlaySize.X + OVERLAY_OFFSET, 0);
    ImGui.SetCursorPos(overlayPos1);

    Vector2 offset1 = new(enabled ? 0.1f : 0.6f, 0.2f);
    Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
    ImGui.Image(_selectedUnselectedIcon!.Value, overlaySize, offset1, offset2);

    // put cursor back to where it was after rendering the button to prevent
    // messing up the table rendering
    ImGui.SetCursorPos(finalPos);

    return enabled;
  }
}
