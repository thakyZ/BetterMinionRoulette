using System.Collections.Generic;

using Newtonsoft.Json;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

internal class MinionGroup
{
    public virtual string Name { get; set; } = "";

    [JsonProperty(PropertyName = "EnabledMinions")]
    public HashSet<uint> IncludedMinions { get; set; } = new();

    [JsonProperty(PropertyName = "IncludeNewMinions")]
    public bool IncludedMeansActive { get; set; }
}
