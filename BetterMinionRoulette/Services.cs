using System;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

using Lumina;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette;
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
  Justification = "These Plugin Services still need to be able to be set by Dalamud.")]
internal sealed class Services {
  internal readonly DalamudPluginInterface DalamudPluginInterface;

  [PluginService] internal SigScanner SigScanner { get; private set; } = null!;

  [PluginService] internal CommandManager CommandManager { get; private set; } = null!;

  [PluginService] public GameGui GameGui { get; private set; } = null!;

  [PluginService] public DataManager DataManager { get; private set; } = null!;

  internal GameData GameData => DataManager.GameData;

  [PluginService] internal ChatGui ChatGui { get; private set; } = null!;

  [PluginService] internal Condition Condition { get; private set; } = null!;

  [PluginService] internal ClientState ClientState { get; private set; } = null!;

  [PluginService] internal Framework Framework { get; private set; } = null!;

  internal Plugin PluginInstance { get; }

  internal TextureHelper TextureHelper { get; }

  private event EventHandler? LoginInternal;
  internal event EventHandler Login {
    add {
      if (value is null) {
        return;
      }

      LoginInternal += value;
      if (LoginInternal == value) {
        ClientState.Login += OnLogin;
      }
    }
    remove {
      LoginInternal -= value;
      if (LoginInternal == null) {
        ClientState.Login -= OnLogin;
      }
    }
  }

  private event EventHandler<ushort>? TerritoryChangedInternal;
  internal event EventHandler<ushort> TerritoryChanged {
    add {
      if (value is null) {
        return;
      }

      TerritoryChangedInternal += value;
      if (TerritoryChangedInternal == value) {
        ClientState.TerritoryChanged += OnTerritoryChanged;
      }
    }
    remove {
      TerritoryChangedInternal -= value;
      if (TerritoryChangedInternal == null) {
        ClientState.TerritoryChanged -= OnTerritoryChanged;
      }
    }
  }

  public Services(DalamudPluginInterface pluginInterface, Plugin plugin) {
    DalamudPluginInterface = pluginInterface;
    _ = pluginInterface.Inject(this);
    TextureHelper = new(this);
    PluginInstance = plugin;
  }

  private void OnLogin(object? sender, EventArgs e) {
    Framework.Update += OnFrameworkUpdate;
  }

  private static ushort _territory;

  private void OnTerritoryChanged(object? sender, ushort territory) {
    Framework.Update += OnFrameworkUpdate;
    _territory = territory;
  }

  private void OnFrameworkUpdate(Framework framework) {
    if (ClientState.LocalPlayer is null) {
      return;
    }

    Framework.Update -= OnFrameworkUpdate;
    LoginInternal?.Invoke(this, EventArgs.Empty);
    TerritoryChangedInternal?.Invoke(this, _territory);
  }
}
