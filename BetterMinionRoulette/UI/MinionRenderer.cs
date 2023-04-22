using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

internal sealed class MinionRenderer {
  private const int PAGE_SIZE = COLUMNS * ROWS;
  private const int COLUMNS = 5;
  private const int ROWS = 6;

  private static nint? _selectedUnselectedIcon;
  private readonly Services _services;

  public MinionRenderer(Services services) {
    _services = services;
  }

  public void RenderPage(List<MinionData> minions, HashSet<uint> selectedMinions, bool selectedMeansActive, int page) {
    int i = 0;
    foreach (MinionData minion in minions.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE)) {
      if (i++ > 0) {
        ImGui.SameLine();
      }

      if (i >= COLUMNS) {
        i = 0;
      }

      bool enabled = selectedMinions.Contains(minion.ID) == selectedMeansActive;
      enabled = Render(minion, enabled);
      if (enabled == selectedMeansActive) {
        _ = selectedMinions.Add(minion.ID);
      } else {
        _ = selectedMinions.Remove(minion.ID);
      }
    }
  }

  public static void Update(List<MinionData> minions, HashSet<uint> selectedMinions, bool selected, int? page) {
    IEnumerable<MinionData> filteredMinions = minions;
    if (page is not null) {
      filteredMinions = filteredMinions.Skip((page.Value - 1) * PAGE_SIZE).Take(PAGE_SIZE);
    }

    Func<uint, bool> operation = selected ? selectedMinions.Add : selectedMinions.Remove;
    foreach (MinionData minion in filteredMinions) {
      _ = operation(minion.ID);
    }
  }

  public static int GetPageCount(int minionCount) {
    return (minionCount / PAGE_SIZE) + (minionCount % PAGE_SIZE == 0 ? 0 : 1);
  }

  public bool Render(MinionData minionData, bool enabled) {
    _selectedUnselectedIcon ??= _services.TextureHelper.LoadUldTexture("readycheck");

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
      ImGui.SetTooltip(minionData.Name.RawString);
    }

    Vector2 finalPos = ImGui.GetCursorPos();

    // calculate overlay (top right corner) position
    Vector2 overlayPos = originalPos + new Vector2(buttonSize.X - overlaySize.X + OVERLAY_OFFSET, 0);
    ImGui.SetCursorPos(overlayPos);

    Vector2 offset = new(enabled ? 0.1f : 0.6f, 0.2f);
    Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
    ImGui.Image(_selectedUnselectedIcon!.Value, overlaySize, offset, offset2);

    // put cursor back to where it was after rendering the button to prevent
    // messing up the table rendering
    ImGui.SetCursorPos(finalPos);

    return enabled;
  }
}
