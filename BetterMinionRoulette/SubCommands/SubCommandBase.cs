using System;
using System.Collections.Generic;
using System.Globalization;

using Dalamud.Game.Gui;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.SubCommands;

internal abstract class SubCommandBase : ISubCommand
{
    public abstract string HelpMessage { get; }

    public abstract string CommandName { get; }

    public virtual string? ParentCommand => null;

    public string FullCommand { get; set; } = null!;

    public Plugin Plugin { get; set; } = null!;

    protected Dictionary<string, ISubCommand> SubCommands { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    protected static ChatGui ChatGui => Plugin.GetPlugin().ChatGui;

    public void AddSubCommand(ISubCommand child)
    {
        SubCommands.Add(child.CommandName, child);
    }

    public bool Execute(string[] parameter)
    {
        if (parameter.Length == 1 && parameter[0].ToLower(CultureInfo.CurrentCulture) == "help")
        {
            PrintHelp();
            return true;
        }

        if (parameter.Length >= 1 && SubCommands.TryGetValue(parameter[0], out var subCommand))
        {
            return subCommand.Execute(parameter[1..]);
        }

        return ExecuteInternal(parameter);
    }

    protected void PrintHelp()
    {
        ChatGui.Print(HelpMessage);
    }

    protected static void DebugOutput(string message)
    {
        ChatGui.Print(message);
    }

    protected abstract bool ExecuteInternal(string[] parameter);
}
