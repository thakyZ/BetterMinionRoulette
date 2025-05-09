﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.SubCommands;

[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class BaseCommand : SubCommandBase {
  private string? _helpMessage;

  public override string HelpMessage => _helpMessage ??= BuildHelpMessage();

  public override string CommandName => "";

  protected override bool ExecuteInternal(string[] parameter) {
    if (parameter.Length > 0) {
      return false;
    }

    Services.WindowManager.OpenConfigWindow();
    return true;
  }

  private string BuildHelpMessage() {
    var sb = new StringBuilder()
                     .AppendLine("Usage:")
                     .AppendLine(FullCommand)
                     .AppendLine("  -> opens the config window")
                     .Append(FullCommand).AppendLine(" help")
                     .Append("  -> prints this help");

    var modes = SubCommands.Keys.Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.ToLower(CultureInfo.CurrentCulture)).ToArray();
    if (modes.Any()) {
      _ = sb.AppendLine()
          .Append(FullCommand).AppendLine(" <mode> [help]")
          .AppendLine("  -> executes the selected mode. Available modes are: ")
          .Append("  -> ")
          .AppendJoin(", ", modes).AppendLine()
          .Append("  -> if the help parameter is present, displays additional information about the selected mode instead");
    }

    return sb.ToString();
  }
}
