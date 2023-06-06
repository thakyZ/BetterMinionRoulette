using System.Linq;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;

internal static class MinionGroupManager
{
    public static void Delete(CharacterConfig config, string name)
    {
        for (int i = 0; i < config.Groups.Count; ++i)
        {
            if (name == config.Groups[i].Name)
            {
                config.Groups.RemoveAt(i);
                break;
            }
        }

        if (config.MinionRouletteGroup == name)
    {
        config.MinionRouletteGroup = config.Groups.FirstOrDefault()?.Name;
    }
    }

    public static void Rename(CharacterConfig config, string currentName, string newName)
    {
        if (config.MinionRouletteGroup == currentName)
        {
            config.MinionRouletteGroup = newName;
        }

        MinionGroup? group = config.Groups.Find(x => x.Name == currentName);
        if (group is not null)
    {
        group.Name = newName;
    }
    }
}
