using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

internal sealed class CharacterConfig
{
    [Versions(introduced: 2)]
    [JsonIgnore]
    public bool IsNew { get; set; }

    [Versions(introduced: 1)]
    public bool IncludeNewMinions { get; set; } = true;

    [Versions(introduced: 1)]
    public bool OmitIslandMinions { get; set; } = true;

    [Versions(introduced: 2)]
    public List<MinionGroup> Groups { get; set; } = new();

    [Versions(introduced: 1)]
    public string? MinionRouletteGroup { get; set; }

    [Versions(introduced: 2)]
    [JsonIgnore]
    public bool HasNonDefaultGroups => Groups.Count > 1;

    [Versions(introduced: 1)]
    public List<uint> IslandMinions { get; set; } = new();

    [Versions(introduced: 1, removed: 2)]
    public List<uint> EnabledMinions { get; set; } = new();

    public void CopyFrom(CharacterConfig other)
    {
        IncludeNewMinions = other.IncludeNewMinions;
        OmitIslandMinions = other.OmitIslandMinions;
        Groups = other.Groups;
        IslandMinions = other.IslandMinions;
    }

    public MinionGroup? GetMinionGroup(string name)
    {
        return Groups.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }
}
