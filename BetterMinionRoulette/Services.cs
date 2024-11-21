using System;
using System.Diagnostics.CodeAnalysis;

using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Util;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

using CharacterManager = NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.CharacterManager;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette;

internal sealed class ServiceMethods {
  private event EventHandler? LoginInternal;

  internal event EventHandler Login {
    add {
      if (value is null) {
        return;
      }
      LoginInternal += value;
      if (LoginInternal == value) {
        Services.ClientState.Login += OnLogin;
      }
    }
    remove {
      LoginInternal -= value;
      if (LoginInternal == null) {
        Services.ClientState.Login -= OnLogin;
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
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
      }
    }
    remove {
      TerritoryChangedInternal -= value;
      if (TerritoryChangedInternal == null) {
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
      }
    }
  }

  private void OnLogin() {
    Services.Framework.Update += OnFrameworkUpdate;
  }

  private static ushort _territory;

  private void OnTerritoryChanged(ushort territory) {
    Services.Framework.Update += OnFrameworkUpdate;
    _territory = territory;
  }

  private void OnFrameworkUpdate(IFramework framework) {
    if (Services.ClientState.LocalPlayer is null) {
      return;
    }

    Services.Framework.Update -= OnFrameworkUpdate;
    LoginInternal?.Invoke(this, EventArgs.Empty);
    TerritoryChangedInternal?.Invoke(this, _territory);
  }
}

internal sealed class Services {
  public static IDalamudPluginInterface Interface { get; private set; } = null!;

  [PluginService]
  public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

  [PluginService]
  public static ICommandManager CommandManager { get; private set; } = null!;

  [PluginService]
  public static IDataManager DataManager { get; private set; } = null!;

  [PluginService]
  public static ITextureProvider TextureProvider { get; private set; } = null!;

  [PluginService]
  public static IChatGui ChatGui { get; private set; } = null!;

  [PluginService]
  public static IClientState ClientState { get; private set; } = null!;

  [PluginService]
  public static IFramework Framework { get; private set; } = null!;

  [PluginService]
  public static IPluginLog Log { get; private set; } = null!;
  internal static Plugin PluginInstance { get; private set; } = null!;
  internal static TextureHelper TextureHelper { get; private set; } = null!;
  internal static MinionRegistry MinionRegistry { get; private set; } = null!;
  internal static WindowManager WindowManager { get; private set; } = null!;
  internal static ServiceMethods ServiceMethods { get; private set; } = null!;
  internal static Configuration Configuration { get; private set; } = null!;
  internal static CharacterManager CharacterManager { get; private set; } = null!;

  public static void Init(IDalamudPluginInterface pluginInterface, Plugin plugin) {
    Interface = pluginInterface;
    pluginInterface.Create<Services>();
    PluginInstance ??= plugin;
    TextureHelper = new();
    MinionRegistry = new MinionRegistry();
    WindowManager = new WindowManager();
    ServiceMethods = new ServiceMethods();
    Configuration config = pluginInterface.GetPluginConfig() as Configuration ?? Configuration.Init();
    ConfigVersionManager.DoMigration(config);
    config.SaveConfig();
    Configuration = config;
    CharacterManager = new CharacterManager();
  }
}
