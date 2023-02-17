using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

using System.Linq;
using System.Reflection;

using Addon = FFXIVClientStructs.Attributes.Addon;

namespace BetterMinionRoulette.Util;

internal static class CastBarHelper {
  private static (int IconID, SeString Text)? _regularMinionRoulette;
  private static (int IconID, SeString Text)? _flyingMinionRoulette;

  private static (long IconID, string Text)? _lastCastInfo;
  private static uint? _lastMinionActionID;
  private static bool _shouldUpdate;

  private static bool _initialized;

  private unsafe delegate void CastBarOnUpdateDelegate(AddonCastBar* castbar, void* a2);

  private static Hook<CastBarOnUpdateDelegate>? _castBarOnUpdate;

  internal static Plugin? Plugin { get; set; }

  private static bool? _show;

  public static bool? Show {
    get => _show;
    set {
      _show = value;
      _shouldUpdate = true;
    }
  }

  public static uint MinionID { get; set; }
  public static bool IsFlyingRoulette { get; set; }

  public static unsafe void Enable() {
    if (!_initialized) {
      _initialized = true;
      if (!Plugin!.SigScanner.TryScanText("48 83 EC 38 48 8B 92", out var address)) {
        return;
      }

      _castBarOnUpdate = Hook<CastBarOnUpdateDelegate>.FromAddress(address, CastBarOnUpdate);
    }

    if (_castBarOnUpdate is null) {
      return;
    }

    _castBarOnUpdate.Enable();
  }

  private static bool IsNullOr(this uint? value, uint comparand) {
    return value is null || value == comparand;
  }

  private static unsafe void CastBarOnUpdate(AddonCastBar* castBar, void* a2) {
    _castBarOnUpdate?.Original(castBar, a2);
    if (!_shouldUpdate || !_initialized) {
      return;
    }

    _shouldUpdate = false;

    if (Show is null || (Show is true && _lastMinionActionID.IsNullOr(MinionID))) {
      _show = null;
      _lastMinionActionID = null;
      _lastCastInfo = null;
      return;
    }

    if (Show is false && _regularMinionRoulette is null) {
      var sheet = Plugin!.GameData.GetExcelSheet<GeneralAction>();
      var MinionRouletteAction = sheet!.GetRow(9);
      _regularMinionRoulette = (MinionRouletteAction!.Icon, MinionRouletteAction.Name);

      MinionRouletteAction = sheet.GetRow(24);
      _flyingMinionRoulette = (MinionRouletteAction!.Icon, MinionRouletteAction.Name);
    }

    // un-hiding Minion doesn't work cleanly.
    // implicitly unhiding works best usually, but not at all if the same Minion
    // is selected first by roulette and then again manually
    UpdateCastBarInternal(castBar);
  }

  private static unsafe void UpdateCastBarInternal(AddonCastBar* castBar) {
    if (castBar->AtkUnitBase.UldManager.NodeList == null || castBar->AtkUnitBase.UldManager.NodeListCount < 8) {
      // cast bar is configured weirdly. ignore.
      return;
    }

    AtkComponentNode* icon = (AtkComponentNode*)castBar->AtkUnitBase.GetNodeById(8u);
    AtkTextNode* skillNameText = castBar->AtkUnitBase.GetTextNodeById(4u);
    var component = (AtkComponentIcon*)icon->Component;

    Plugin.Log($"Casting {castBar->CastName} | {skillNameText->NodeText}");

    if (Show is false) {
      // store current cast info for restoring later if necessary
      _lastCastInfo = (component->IconId, skillNameText->NodeText.ToString());

      // replace cast bar contents with Minion roulette information.
      var MinionRoulette = IsFlyingRoulette ? _flyingMinionRoulette : _regularMinionRoulette;
      var iconID = MinionRoulette!.Value.IconID;
      var text = MinionRoulette.Value.Text.RawData;

      component->IconId = iconID;
      component->IconImage->LoadIconTexture(iconID, 0);
      skillNameText->SetText(text);
    } else if (_lastCastInfo is { } /* same as "not null", but nameable */ castInfo) {
      // restore previous cast info
      // TODO: find the place in the game code where the transformation from
      // TODO: (actiontype, actionid) to (icon, text) happens and just call
      // TODO: that instead.
      component->IconId = castInfo.IconID;
      component->IconImage->LoadIconTexture((int)component->IconId, 0);
      skillNameText->SetText(castInfo.Text);
    }
  }

  private static unsafe void ResetCastBar() {
    if (_lastCastInfo is null) {
      // last cast wasn't replaced Minion roulette, nothing to do.
      return;
    }

    var castBar = GetUnitBase<AddonCastBar>();
    if (castBar is null) {
      return;
    }

    _show = null;
    UpdateCastBarInternal(castBar);
  }

  private static unsafe T* GetUnitBase<T>(string? name = null, int index = 1) where T : unmanaged {
    if (string.IsNullOrEmpty(name) && typeof(T).GetCustomAttribute(typeof(Addon)) is Addon attr) {
      name = attr.AddonIdentifiers.FirstOrDefault();
    }
    return string.IsNullOrEmpty(name)
            ? default
            : (T*)Plugin!.GameGui.GetAddonByName(name, index);
  }

  public static void Disable() {
    _castBarOnUpdate?.Disable();
    ResetCastBar();
  }
}
