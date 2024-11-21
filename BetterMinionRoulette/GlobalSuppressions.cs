// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1309:Use ordinal string comparison",
                   Justification = "We actually want string normalization here, to ensure same behavior as in the duplicate check when renaming or adding a group",
                   Scope = "member", Target = "~M:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data.CharacterConfig.GetMinionGroup(System.String)~NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data.MinionGroup")]
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
                   Justification = "Instantiated via reflection",
                   Scope = "type", Target = "~T:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.SubCommands.BaseCommand")]
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
                   Justification = "Instantiated via reflection",
                   Scope = "type", Target = "~T:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.SubCommands.DebugCommand")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.Log")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.Framework")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.ClientState")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.ChatGui")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.TextureProvider")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.DataManager")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.CommandManager")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.GameInteropProvider")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.Interface")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static",
                   Justification = "IDalamudPlugin requires non-static name property.",
                   Scope = "member", Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Plugin.Name")]
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
                   Justification = "This class does not need to be static.",
                   Scope = "type", Target = "~T:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services")]
[assembly: SuppressMessage("Roslynator", "RCS1170:Use read-only auto-implemented property.",
                   Justification = "Dalamud services manager requires setter", Scope = "member",
                   Target = "~P:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Services.ServiceMethods")]
