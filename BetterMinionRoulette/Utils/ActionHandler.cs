using System;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class ActionHandler : IDisposable {
  private readonly Hook<UseActionHandler>? _useActionHook;
  private (bool hide, uint actionID) _hideAction;
  private bool _disposedValue;

  public unsafe ActionHandler() {
    nint renderAddress = (nint)ActionManager.Addresses.UseAction.Value;
    if (renderAddress is 0) {
      Services.WindowManager.DebugWindow.Broken("Unable to load UseAction address");
      return;
    }

    _useActionHook = Services.GameInteropProvider.HookFromAddress<UseActionHandler>(renderAddress, OnUseAction);
    _useActionHook.Enable();
  }

  public unsafe delegate byte UseActionHandler(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID = 3758096384U, uint a4 = 0U, uint a5 = 0U, uint a6 = 0U, void* a7 = default);

  private unsafe byte OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7) {
    (bool hide, uint hideActionID) = _hideAction;
    _hideAction = (false, 0);

    if (CharacterConfig is not { } characterConfig || (characterConfig.MinionRouletteGroup is null)) {
      return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
    }

    string? groupName = (actionID, actionType) switch {
      (10, ActionType.GeneralAction) => CharacterConfig.MinionRouletteGroup,
      _ => null,
    };

    bool isRouletteActionID = actionID is 9 or 24;
    ActionType oldActionType = actionType;
    uint oldActionId = actionID;
    if (groupName is not null) {
      MinionGroup? minionGroup = CharacterConfig.GetMinionGroup(groupName);

      uint newActionID = 0;
      if (minionGroup is not null) {
        newActionID = Services.MinionRegistry.GetRandom(ActionManager.Instance(), minionGroup, characterConfig.OmitIslandMinions);
      }

      if (newActionID is not 0) {
        actionType = ActionType.Companion;
        actionID = newActionID;
      }
    }

    if (hide) {
      oldActionId = hideActionID;
      oldActionType = ActionType.GeneralAction;
      isRouletteActionID = true;
    }

    return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
  }

  public CharacterConfig? CharacterConfig { get; set; }

  public unsafe void HandleMinionCommand(string __, string arguments) {
    if (CharacterConfig is not { } characterConfig) {
      return;
    }

    if (string.IsNullOrWhiteSpace(arguments)) {
      Services.ChatGui.PrintError("Please specify a minion group");
      return;
    }

    arguments = RenameItemDialog.NormalizeWhiteSpace(arguments);

    MinionGroup? minionGroup = characterConfig.GetMinionGroup(arguments);
    if (minionGroup == null) {
      Services.ChatGui.PrintError($"Minion group \"{arguments}\" not found.");
      return;
    }

    uint minion = Services.MinionRegistry.GetRandom(ActionManager.Instance(), minionGroup, characterConfig.OmitIslandMinions);
    if (minion is not 0) {
      _hideAction = (true, actionID: 9);
      _ = ActionManager.Instance()->UseAction(ActionType.Companion, minion);
    } else {
      Services.ChatGui.PrintError($"Unable to summon minion from group \"{arguments}\".");
    }
  }

  private void Dispose(bool disposing) {
    if (!_disposedValue) {
      if (disposing) {
        // TODO: dispose managed state (managed objects)
      }

      _useActionHook?.Dispose();

      // TODO: free unmanaged resources (unmanaged objects) and override finalizer
      // TODO: set large fields to null
      _disposedValue = true;
    }
  }

  // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
  ~ActionHandler() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: false);
  }

  public void Dispose() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
