using System;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class ActionHandler : IDisposable {
  private readonly Hook<ActionManager.Delegates.UseAction>? _useActionHook;
  private bool _disposedValue;

  public unsafe ActionHandler() {
    nint renderAddress = (nint)ActionManager.MemberFunctionPointers.UseAction;
    if (renderAddress is 0) {
      Services.WindowManager.DebugWindow.Broken("Unable to load UseAction address");
      return;
    }

    _useActionHook = Services.GameInteropProvider.HookFromAddress<ActionManager.Delegates.UseAction>(renderAddress, OnUseAction);
    _useActionHook.Enable();
  }

  private unsafe bool OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetID, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteID, bool* outOptAreaTargeted) {
    try {
      if (actionType == ActionType.GeneralAction) {
        Services.Log.Debug("Observed general action {ActionId}.", actionID);
      }

      if (actionID == 10 && actionType == ActionType.GeneralAction) {
        if (CharacterConfig is not { } characterConfig) {
          Services.Log.Warning("Minion Roulette was observed before the character configuration was loaded.");
        } else if (characterConfig.MinionRouletteGroup is not { } groupName) {
          Services.Log.Warning("Minion Roulette was observed, but custom roulette is disabled.");
        } else if (characterConfig.GetMinionGroup(groupName) is not { } minionGroup) {
          Services.Log.Warning("Configured minion group {GroupName} does not exist.", groupName);
        } else {
          Services.Log.Debug("Intercepted Minion Roulette for group {GroupName}.", groupName);
          uint newActionID = Services.MinionRegistry.GetRandom(ActionManager.Instance(), minionGroup, characterConfig.OmitIslandMinions);
          if (newActionID == 0) {
            Services.Log.Warning("No available minion was found for group {GroupName}; using the game's roulette.", groupName);
          } else {
            Services.Log.Debug("Replacing Minion Roulette with companion {CompanionId}.", newActionID);
            actionType = ActionType.Companion;
            actionID = newActionID;
          }
        }
      }
    } catch (Exception exception) {
      // Never allow managed exceptions to cross the native hook boundary.
      Services.Log.Error(exception, "Failed to select a minion; using the original action instead.");
    }

    return _useActionHook!.Original(actionManager, actionType, actionID, targetID, extraParam, mode, comboRouteID, outOptAreaTargeted);
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
