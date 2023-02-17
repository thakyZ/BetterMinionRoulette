﻿#if DEBUG
using System.Diagnostics.CodeAnalysis;

namespace BetterMinionRoulette.SubCommands;

[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class DebugCommand : SubCommandBase
{
    public override string HelpMessage => "Shows the debug window";

    public override string CommandName => "dbg";

    protected override bool ExecuteInternal(string[] parameter)
    {
        if (parameter.Length == 1 && parameter[0] == "clr")
        {
            Plugin.WindowManager.DebugWindow.Clear();
            return true;
        }

        if (parameter.Length > 0)
        {
            return false;
        }

        Plugin.WindowManager.DebugWindow.Open();

        return true;
    }
}
#endif
