using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using BetterMinionRoulette.Config;
using BetterMinionRoulette.SubCommands;
using BetterMinionRoulette.UI;
using BetterMinionRoulette.Util;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina;
using Lumina.Excel.GeneratedSheets;

namespace BetterMinionRoulette;

public sealed class Plugin : IDalamudPlugin {
  private static BetterMinionRoulette.Plugin _plugin { get; set; } = null!;

  private bool _isDisposed;

  public string Name => "Better Minion Roulette";

  private const string CommandText = "/bminionr";

  ////public const string CommandHelpMessage = $"Does all the things. Type \"{CommandText} help\" for more information.";
  public const string CommandHelpMessage = "Open the config window.";

  internal DalamudPluginInterface PluginInterface { get; init; }

  internal SigScanner SigScanner { get; init; }

  internal CommandManager CommandManager { get; init; }

  internal GameGui GameGui { get; init; }

  internal DataManager DataManager { get; init; }
  internal GameData GameData {
    get {
      if (DataManager is not null) {
        return DataManager.GameData;
      }
      return new GameData("");
    }
  }

  internal ChatGui ChatGui { get; init; }

  internal ClientState ClientState { get; init; }

  internal Configuration Configuration { get; private set; }

  private readonly Hook<UseActionHandler>? _useActionHook;
  internal readonly WindowManager WindowManager;
  private (bool hide, uint actionID) _hideAction;
  internal ISubCommand _command;

  public unsafe Plugin(
    DalamudPluginInterface pluginInterface,
    SigScanner sigScanner,
    CommandManager commandManager,
    GameGui gameGui,
    DataManager dataManager,
    ChatGui chatGui,
    ClientState clientState) {
    PluginInterface = pluginInterface;
    SigScanner = sigScanner;
    CommandManager = commandManager;

    ClientState = clientState;

    GameGui = gameGui;
    DataManager = dataManager;
    ChatGui = chatGui;

    _plugin = this;

    CastBarHelper.Plugin = this;

    WindowManager = new WindowManager(this);
    Minions.WindowManager = WindowManager;
    PluginInterface.UiBuilder.Draw += WindowManager.Draw;

    try {
      Configuration config = PluginInterface.GetPluginConfig() as Configuration ?? Configuration.Init();
      config.Initialize(_plugin);
      LoadCharacter(config);
      config.Migrate();
      SaveConfig(config);

      Configuration = config;

      _command = InitCommands();

      PluginInterface.UiBuilder.OpenConfigUi += WindowManager.OpenConfigWindow;

      _ = CommandManager.AddHandler(CommandText, new CommandInfo(HandleCommand) { HelpMessage = CommandHelpMessage });

      nint renderAddress = (nint)ActionManager.Address.UseAction.Value;

      if (renderAddress is 0) {
        WindowManager.DebugWindow.Broken("Unable to load UseAction address");
        return;
      }

      _useActionHook = Hook<UseActionHandler>.FromAddress(renderAddress, OnUseAction);
      _useActionHook.Enable();
      CastBarHelper.Enable();
      ClientState.Login += ClientState_Login;
      ClientState.TerritoryChanged += ClientState_TerritoryChanged;
    } catch {
      Dispose();
      throw;
    }
  }

  internal static BetterMinionRoulette.Plugin GetPlugin() {
    return _plugin;
  }

  private void ClientState_Login(object? sender, EventArgs e) {
    LoadCharacter();
  }

  private void ClientState_TerritoryChanged(object? sender, ushort territoryID) {
    var territoryTypeSheet = DataManager.Excel.GetSheet<TerritoryType>()!;
    var islandSanctuary = territoryTypeSheet.First(x => x.Name == "h1m2");
    if (islandSanctuary is not null) {
      Minions.RefreshIsland();
    }
  }

  private void LoadCharacter() {
    Configuration = Configuration.LoadOnLogin(Configuration);
    SaveConfig(Configuration);
    Minions.Load(Configuration);
  }
  private void LoadCharacter(Configuration config) {
    Configuration = Configuration.LoadOnLogin(config);
    SaveConfig(config);
    Minions.Load(config);
  }

  [Conditional("DEBUG")]
  internal static void Log(string message) {
    CastBarHelper.Plugin!.WindowManager.DebugWindow.AddText(message);
  }

  internal static void SaveConfig(Configuration configuration) {
    GetPlugin().PluginInterface.SavePluginConfig(configuration);
  }

  private void HandleCommand(string command, string arguments) {
    // DONE: correctly handle arguments, including
    // [/foo "bar"] being equal to [/foo bar] and the like
    var parts = arguments.Split(new char[2] { '\'', '"' })
          .Select((element, index) => index % 2 == 0 // If even index
                  ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) // Split the item
                  : new string[] { element }) // Keep the entire item
          .SelectMany(element => element).ToArray();
    try {
      var success = _command.Execute(parts);

      if (!success) {
        ChatGui.PrintError($"Invalid command: {command} {arguments}");
      }
    } catch (Exception e) {
      ChatGui.PrintError(e.Message);
      throw;
    } finally {
      ChatGui.UpdateQueue();
    }
  }

  private ISubCommand InitCommands() {
    var allCommands = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.IsAssignableTo(typeof(ISubCommand)) && !x.IsAbstract && !x.IsInterface && x.GetConstructors().Any(x => x.GetParameters().Length == 0))
            .Select(x => (ISubCommand)Activator.CreateInstance(x)!)
            .ToList();
    Dictionary<string, ISubCommand> commands = new(StringComparer.InvariantCultureIgnoreCase);
    foreach (var command in allCommands) {
      var fullCommand = $"{command.ParentCommand} {command.CommandName}".Trim();
      commands.Add(fullCommand, command);
      command.Plugin = this;
      command.FullCommand = $"{CommandText} {command.ParentCommand}".Trim();
    }

    foreach (var command in allCommands) {
      commands[command.ParentCommand ?? string.Empty].AddSubCommand(command);
    }

    return commands[string.Empty];
  }

  private unsafe byte OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7) {
    var hideAction = _hideAction;
    _hideAction = (false, 0);

    if (!Configuration.Enabled) {
      return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
    }

    string? groupName = (actionID, actionType) switch {
      (10, ActionType.General) => Configuration.DefaultGroupName,
      _ => null,
    };

    var isRouletteActionID = actionID is 10;
    var oldActionType = actionType;
    var oldActionId = actionID;
    if (groupName is not null) {
      var newActionID = Minions.GetInstance(groupName)!.GetRandom(actionManager);
      if (newActionID != 0) {
        actionType = ActionType.Unk_8;
        actionID = newActionID;
      }
    }

    if (hideAction.hide) {
      oldActionId = _hideAction.actionID;
      oldActionType = ActionType.General;
      isRouletteActionID = true;
    }

    switch (oldActionType) {
      case ActionType.General when isRouletteActionID && actionType != oldActionType:
        CastBarHelper.Show = false;
        CastBarHelper.IsFlyingRoulette = oldActionId == 10;
        CastBarHelper.MinionID = actionID;
        break;
      case ActionType.Unk_8:
        CastBarHelper.Show = true;
        CastBarHelper.MinionID = actionID;
        break;
    }

    var result = _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);

    return result;
  }

  public unsafe delegate byte UseActionHandler(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID = 3758096384U, uint a4 = 0U, uint a5 = 0U, uint a6 = 0U, void* a7 = default);

  private void Dispose(bool disposing) {
    if (!_isDisposed && disposing) {
      SaveConfig(Configuration);

      _useActionHook?.Disable();
      _useActionHook?.Dispose();

      PluginInterface.UiBuilder.Draw -= WindowManager.Draw;
      PluginInterface.UiBuilder.OpenConfigUi -= WindowManager.OpenConfigWindow;
      ClientState.Login -= ClientState_Login;
      ClientState.TerritoryChanged -= ClientState_TerritoryChanged;

      TextureHelper.Dispose();

      _ = CommandManager.RemoveHandler(CommandText);

      CastBarHelper.Plugin = null;
      CastBarHelper.Disable();
      // TODO: free unmanaged resources (unmanaged objects) and override finalizer
      // TODO: set large fields to null
      _isDisposed = true;
    }
  }

  // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
  ~Plugin() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: true);
  }

  public void Dispose() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
