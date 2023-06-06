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
[assembly: SuppressMessage("Security", "CA5394:Do not use insecure randomness",
                   Justification = "Non-critical use of randomness, so we prefer speed over security",
                   Scope = "member", Target = "~M:NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils.MinionRegistry.GetRandom(FFXIVClientStructs.Interop.Pointer{FFXIVClientStructs.FFXIV.Client.Game.ActionManager},NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data.MinionGroup,System.Boolean)~System.UInt32")]
