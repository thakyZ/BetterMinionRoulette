﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Game.Command;
using Dalamud.Plugin;

using Lumina.Excel. Sheets;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.SubCommands;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette;

public sealed class Plugin : IDalamudPlugin {
  private bool _isDisposed;

  private static string StaticName => "Better Minion Roulette";

  public string Name => StaticName;

  private const string COMMAND_TEXT  = "/bminionr";

  private const string MINION_COMMAND_TEXT  = "/bminiong";

  ////public const string CommandHelpMessage = $"Does all the things. Type \"{CommandText} help\" for more information.";
  public const string COMMAND_HELP_MESSAGE = "Opens the config window.";

  internal CharacterConfig? CharacterConfig {
    get => _actionHandler.CharacterConfig;
    private set => _actionHandler.CharacterConfig = value;
  }

  private readonly ActionHandler _actionHandler;
  private readonly ISubCommand _command;

  public unsafe Plugin(IDalamudPluginInterface pluginInterface) {
    if (pluginInterface is null) {
      throw new ArgumentNullException(nameof(pluginInterface));
    }

    Services.Init(pluginInterface, this);

    try {
      _actionHandler = new ActionHandler();

      pluginInterface.UiBuilder.Draw += Services.WindowManager.Draw;

      Services.ServiceMethods.Login += OnLogin;
      Services.ServiceMethods.TerritoryChanged += OnTerritoryChanged;
      if (Services.ClientState.LocalPlayer is not null) {
        OnLogin(this, EventArgs.Empty);
      }

      _command = InitCommands();

      pluginInterface.UiBuilder.OpenConfigUi += Services.WindowManager.OpenConfigWindow;

      _ = Services.CommandManager.AddHandler(
          COMMAND_TEXT,
          new CommandInfo(HandleCommand) { HelpMessage = COMMAND_HELP_MESSAGE });
      _ = Services.CommandManager.AddHandler(
          MINION_COMMAND_TEXT,
          new CommandInfo(_actionHandler.HandleMinionCommand) {
            HelpMessage = $"Summon a random minion from the specified group, e.g. \"{MINION_COMMAND_TEXT} My Group\" summons a minion from the \"My Group\" group"
          });
#if DEBUG
      _ = Services.CommandManager.AddHandler(
          "/bminiondbg",
          new CommandInfo((string _, string args) => {
            var argsSplit = args.Split(" ");
            if (argsSplit.Length >= 2) {
              if (argsSplit[0] == "island") {
                if (GameFunctions.IsPlayersOwnIsland()) {
                  if (uint.TryParse(argsSplit[1], out uint id)) {
                    var test = GameFunctions.IsMinionOnIsland(id, false);
                    Services.ChatGui.Print($"[{Name}] Minion with Id, {argsSplit[1]} is {(test ? "" : "not ")}on island.");
                  } else {
                    Services.ChatGui.PrintError($"[{Name}] Invalid id = \"{argsSplit[1]}\"");
                  }
                } else {
                  Services.ChatGui.PrintError($"[{Name}] This is not your own island.");
                }
              } else {
                if (uint.TryParse(argsSplit[1], out uint id)) {
                  var test = GameFunctions.HasMinionUnlocked(id);
                  Services.ChatGui.Print($"[{Name}] Minion with Id, {argsSplit[1]} is {(test ? "" : "not ")}unlocked.");
                } else {
                  Services.ChatGui.PrintError($"[{Name}] Invalid id = \"{argsSplit[1]}\"");
                }
              }
            } else {
              Services.ChatGui.PrintError($"[{Name}] No args specified.");
            }
          }) {
            HelpMessage = "Debug Command"
          });
#endif
    } catch {
      Dispose();
      throw;
    }
  }

  private void ImportCharacterConfig(int? @override = null) {
    switch (@override ?? Services.Configuration.NewCharacterHandling) {
      case Configuration.NewCharacterHandlingModes.IMPORT:
        _ = Services.CharacterManager.Import(Configuration.DUMMY_LEGACY_CONFIG_ID);
        break;

      case Configuration.NewCharacterHandlingModes.BLANK:
        return;

      case Configuration.NewCharacterHandlingModes.ASK:
      default: /* default to "ask" for invalid values */
        Services.WindowManager.OpenDialog(new ConfirmImportCharacterDialog(ConfirmAction));
        break;
    }

    void ConfirmAction(bool import, bool remember) {
      int mode = import ? Configuration.NewCharacterHandlingModes.IMPORT : Configuration.NewCharacterHandlingModes.BLANK;
      if (import) {
        ImportCharacterConfig(mode);
      }

      if (remember) {
        Services.Configuration.NewCharacterHandling = mode;
      }
    }
  }

  private void OnLogin(object? sender, EventArgs e) {
    if (Services.ClientState.LocalPlayer is { } player) {
      CharacterConfig = Services.CharacterManager.GetCharacterConfig(Services.ClientState.LocalContentId, player);
      if (CharacterConfig.IsNew && Services.Configuration.CharacterConfigs.ContainsKey(Configuration.DUMMY_LEGACY_CONFIG_ID)) {
        ImportCharacterConfig();
        CharacterConfig.IsNew = false;
      }
    }
  }

  private void OnTerritoryChanged(object? sender, ushort territoryID) {
    var territoryTypeSheet = Services.DataManager.Excel.GetSheet<TerritoryType>()!;
    var islandSanctuary = territoryTypeSheet.First(x => x.Name == "h1m2");
    if (islandSanctuary.RowId == territoryID) {
      Services.MinionRegistry.RefreshIsland();
    }
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
      bool success = _command.Execute(parts);

      if (!success) {
        Services.ChatGui.PrintError($"Invalid command: {command} {arguments}");
      }
    } catch (Exception e) {
      Services.ChatGui.PrintError(e.Message);
      throw;
    }
  }

  private ISubCommand InitCommands() {
    var allCommands = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsAssignableTo(typeof(ISubCommand)) && !x.IsAbstract && !x.IsInterface && x.GetConstructors().Any(x => x.GetParameters().Length == 0))
                    .Select(x => (ISubCommand)Activator.CreateInstance(x)!)
                    .ToList();
    Dictionary<string, ISubCommand> commands = new(StringComparer.InvariantCultureIgnoreCase);
    foreach (ISubCommand? command in allCommands) {
      string fullCommand = (command.ParentCommand + " " + command.CommandName).Trim();
      commands.Add(fullCommand, command);
      command.Plugin = this;
      command.FullCommand = (COMMAND_TEXT + " " + command.ParentCommand).Trim();
    }

    foreach (ISubCommand? command in allCommands) {
      commands[command.ParentCommand ?? string.Empty].AddSubCommand(command);
    }

    return commands[string.Empty];
  }

  private void Dispose(bool disposing) {
    if (!_isDisposed) {
      if (disposing) {
        // TODO: dispose managed state (managed objects)
      }
      Services.Configuration.SaveConfig();

      Services.Interface.UiBuilder.Draw -= Services.WindowManager.Draw;
      Services.Interface.UiBuilder.OpenConfigUi -= Services.WindowManager.OpenConfigWindow;

      Services.ServiceMethods.Login -= OnLogin;
      Services.ServiceMethods.TerritoryChanged -= OnTerritoryChanged;

      Services.TextureHelper.Dispose();

      _ = Services.CommandManager.RemoveHandler(COMMAND_TEXT);
#if DEBUG
      _ = Services.CommandManager.RemoveHandler("/bminiondbg");
#endif
      _actionHandler.Dispose();

      // TODO: free unmanaged resources (unmanaged objects) and override finalizer
      // TODO: set large fields to null
      _isDisposed = true;
    }
  }

  // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
  ~Plugin() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: false);
  }

  public void Dispose() {
    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
