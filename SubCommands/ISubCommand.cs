﻿namespace BetterMinionRoulette.SubCommands;

internal interface ISubCommand
{
    string HelpMessage { get; }

    string CommandName { get; }

    public string? ParentCommand { get; }

    public string FullCommand { get; set; }

    public Plugin Plugin { get; set; }

    void AddSubCommand(ISubCommand child);

    bool Execute(string[] parameter);
}
