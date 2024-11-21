using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Excel. Sheets;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

/// <summary>
/// Responsible for maintaining a list of minions with ID, name, icon, and whether or not the minion is unlocked.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class MinionRegistry {
  private readonly Dictionary<uint, MinionData> _minionsByID = new();
  private readonly List<MinionData> _minions = new();
  private bool _isInitialized;
  private readonly object _lock = new();

  public int UnlockedMinionCount { get; private set; }

  private void InitializeIfNecessary() {
    // make sure initialization only runs once
    if (_isInitialized) {
      return;
    }

    lock (_lock) {
      // make sure initialization only runs once
      // (again, in case multiple threads called this at the same time)
      if (_isInitialized) {
        return;
      }

      _minions.AddRange(GetAllMinions());
      foreach (MinionData minion in _minions) {
        _minionsByID.Add(minion.ID, minion);
      }

      if (Services.PluginInstance.CharacterConfig is not null) {
        var characterConfig = Services.PluginInstance.CharacterConfig;
        Services.Log.Debug("Initializing island data");
        foreach (var minion in characterConfig.IslandMinions) {
          _minions.Find(x => x.ID == minion)!.Island = true;
          _minionsByID[minion].Island = true;
        }
      }

      _isInitialized = true;
    }
  }

  public void RefreshUnlocked() {
    if (!Services.ClientState.IsLoggedIn) {
      return;
    }

    InitializeIfNecessary();
    int count = 0;
    foreach (MinionData minion in _minions) {
      if (minion.Unlocked = GameFunctions.HasMinionUnlocked(minion.ID)) {
        ++count;
      }
    }

    UnlockedMinionCount = count;
  }

  public void RefreshIsland() {
    Services.Log.Debug("Refreshing island spawned");
    if (Services.ClientState.TerritoryType != 1055u) {
      return;
    }
    foreach (var minion in _minions) {
      minion.Island = GameFunctions.IsMinionOnIsland(minion.ID, minion.Island);
    }
  }

  public IEnumerable<MinionData> GetAllMinions() {
    return from minion in Services.DataManager.Excel.GetSheet<Companion>()
           where minion.Icon != 0 /* valid Minions only */
           orderby minion.Order
           select new MinionData(minion.Singular.ExtractText()) {
             IconID = minion.Icon,
             ID = minion.RowId,
             Unlocked = GameFunctions.HasMinionUnlocked(minion.RowId),
           };
  }

  public static List<MinionData> Filter(List<MinionData> minions, string filterText) {
    return AsList(FilteredMinions(minions, filterText));
  }

  private static List<T> AsList<T>(IEnumerable<T> source) {
    return source as List<T> ?? source.ToList();
  }

  private static IEnumerable<MinionData> FilteredMinions(IEnumerable<MinionData> _minions, string filter) {
    if (!string.IsNullOrEmpty(filter)) {
      _minions = _minions.Where(x => x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
    }

    return _minions;
  }

  public List<MinionData> GetUnlockedMinions(bool omitIsland) {
    InitializeIfNecessary();
    return _minions.Where(x => x.Unlocked && (!omitIsland || !x.Island)).ToList();
  }

  public List<MinionData> GetAvailableMinions(Pointer<ActionManager> actionManager, MinionGroup group, bool omitIsland) {
    RefreshUnlocked();
    List<MinionData> unlockedMinions = GetUnlockedMinions(omitIsland);
    List<MinionData> result = new(unlockedMinions.Count);
    RefreshIsland();


    foreach (MinionData item in unlockedMinions)
    {
        if (group.IncludedMinions.Contains(item.ID) == group.IncludedMeansActive && item.IsAvailable(actionManager))
        {
            result.Add(item);
        }
    }

    return unlockedMinions.FindAll(x => !group.IncludedMinions.Contains(x.ID) && x.IsAvailable(actionManager));
  }


  [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-critical use of randomness, so we prefer speed over security")]
  public uint GetRandom(Pointer<ActionManager> actionManager, MinionGroup group, bool omitIsland) {
    List<MinionData> available = GetAvailableMinions(actionManager, group, omitIsland);

    if (available.Count is 0) {
      return 0;
    }

    if (available.Count is 1) {
      // shortcut: exactly one active mount: can only select that, no matter what
      return available[0].ID;
    }

    int index = Random.Shared.Next(available.Count);
    return available[index].ID;
  }
}
