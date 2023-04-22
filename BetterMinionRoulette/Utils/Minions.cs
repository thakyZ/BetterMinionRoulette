using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.UI;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.Interop;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config.Data;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class Minions {
  private const int PAGE_SIZE = COLUMNS * ROWS;
  private const int COLUMNS = 5;
  private const int ROWS = 6;

  private static volatile bool _isInitialized;
  private static readonly object _initLock = new();

  private static readonly Dictionary<uint, MinionData> _minionsByID = new();
  private static readonly List<MinionData> _minions = new();
  private readonly List<MinionSelectionData> _selectableMinions;
  private readonly Dictionary<uint, MinionSelectionData> _selectableMinionsByID;
  private static readonly Random _random = new();

  private List<MinionSelectionData>? _filteredMinions;
  private static readonly Dictionary<string, Minions> _instancesByGroup = new(StringComparer.InvariantCultureIgnoreCase);
  private static Configuration _config = new();

  private static void InitializeIfNecessary() {
    // make sure initialization only runs once
    if (_isInitialized) {
      return;
    }

    lock (_initLock) {
      // make sure initialization only runs once
      // (again, in case multiple threads called this at the same time)
      if (_isInitialized) {
        return;
      }

      _minions.AddRange(GetAllMinions());
      foreach (var Minion in _minions) {
        _minionsByID.Add(Minion.ID, Minion);
      }

      _isInitialized = true;
    }
  }

  public static WindowManager WindowManager { get; set; } = null!;

  private static IEnumerable<MinionData> GetAllMinions() {
    var minions = from Minion in Plugin.GetPlugin().GameData.GetExcelSheet<Companion>()
                  where Minion.Order > 0 && Minion.Icon != 0 /* valid Minions only */
                  orderby Minion.Order
                  select new MinionData {
                    IconID = Minion.Icon,
                    ID = Minion.RowId,
                    Name = Minion.Singular,
                  };
    return minions;
  }

  private Minions() {
    InitializeIfNecessary();

    _selectableMinions = _minions.ConvertAll(x => new MinionSelectionData(x, true));
    _selectableMinionsByID = _selectableMinions.ToDictionary(x => x.Minion.ID);

    RefreshUnlocked();
    RefreshIsland();
  }

  public int ItemCount => (_filteredMinions ?? _selectableMinions).Count;

  public static Minions? GetInstance(string name) {
    if (_instancesByGroup.TryGetValue(name, out var value)) {
      return value;
    } else {
      var minionGroup = _config.GetMinionGroup();

      if (minionGroup is null) {
        return null;
      }

      value = new();
      _instancesByGroup[name] = value;
      value.Load(minionGroup);
      return value;
    }
  }

  public int PageCount {
    get {
      var cnt = ItemCount;
      return cnt / PAGE_SIZE + (cnt % PAGE_SIZE == 0 ? 0 : 1);
    }
  }

  internal static void Load(Configuration config) {
    RefreshUnlocked();
    RefreshIsland();

    _config = config;
  }

  internal static void Remove(string groupName) {
    _ = _instancesByGroup.Remove(groupName);
  }

  private void Load(MinionGroup minionGroup) {
    _selectableMinions.ForEach(x => x.Enabled = false);
    minionGroup.EnabledMinions.ForEach(x => {
      if (_selectableMinionsByID.TryGetValue(x, out var minion)) {
        minion.Enabled = true;
      }
    });
    minionGroup.IslandMinions.ForEach(x => {
      if (_selectableMinionsByID.TryGetValue(x, out var minion)) {
        minion.Minion.Island = true;
      }
    });

    UpdateUnlocked(_config.CurrentCharacter?.IncludeNewMinions == true);
  }

  internal void Save(MinionGroup minionGroup) {
    minionGroup.EnabledMinions = _selectableMinions.Where(x => x.Enabled).Select(x => x.Minion.ID).ToList();
    minionGroup.IslandMinions = _selectableMinions.Where(x => x.Minion.Island).Select(x => x.Minion.ID).ToList();
  }

  public static void RefreshUnlocked() {
    if (!Plugin.GetPlugin().ClientState.IsLoggedIn) {
      return;
    }

    PluginLog.Debug("Refreshing unlocked");
    InitializeIfNecessary();
    foreach (var minion in _minions) {
      minion.Unlocked = GameFunctions.HasMinionUnlocked(minion.ID);
    }
  }

  public static unsafe bool IsPlayersOwnIsland() {
    var mjiManager = MJIManager.Instance();
    if (mjiManager is not null) {
      return mjiManager->IslandState.CanEditIsland;
    }
    return false;
  }

  public static unsafe Span<bool> RoamingMinionList(byte* roamingMinions) {
    return new(Unsafe.AsPointer(ref roamingMinions[0]), 480);
  }

  public static unsafe void RefreshIsland() {
    var mjiPastureHandler = MJIManager.Instance()->PastureHandler;
    InitializeIfNecessary();
    if (mjiPastureHandler is not null && IsPlayersOwnIsland()) {
      Plugin.Log("Refreshing island spawned");
      foreach (var minion in _minions) {
        minion.Island = RoamingMinionList(mjiPastureHandler->RoamingMinions)[(int)minion.ID];
      }
    }
  }

  public void Filter(bool showLocked, bool? enabledStatus, string? filterText, bool island = false) {
    _filteredMinions = AsList(FilteredMinions(showLocked, enabledStatus, filterText, island));
  }

  public void ClearFilter() {
    _filteredMinions = null;
  }

  private static List<T> AsList<T>(IEnumerable<T> source) {
    return source as List<T> ?? source.ToList();
  }

  public void RenderItems(int page) {
    RefreshUnlocked();
    RefreshIsland();

    _ = ImGui.BeginTable("tbl_Minions", COLUMNS);

    GetPage(page).ForEach(x => x.Render());

    ImGui.EndTable();
  }

  private List<MinionSelectionData> GetPage(int page) {
    return (_filteredMinions ?? _selectableMinions)
        .Skip((page - 1) * PAGE_SIZE)
        .Take(PAGE_SIZE)
        .ToList();
  }

  private IEnumerable<MinionSelectionData> FilteredMinions(bool showUnlocked, bool? enabledStatus, string? filter, bool island = false) {
    IEnumerable<MinionSelectionData> Minions = _selectableMinions;
    if (!showUnlocked) {
      Minions = Minions.Where(x => x.Minion.Unlocked);
    }

    if (enabledStatus is not null) {
      Minions = Minions.Where(x => x.Enabled == enabledStatus);
    }

    if (!string.IsNullOrEmpty(filter)) {
      Minions = Minions.Where(x => x.Minion.Name.RawString.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
    }

    if (island) {
      Minions = Minions.Where(x => !x.Minion.Island);
    }

    return Minions;
  }

  public static (uint IconID, SeString Name) GetCastBarInfo(uint MinionID) {
    var Minion = _minionsByID[MinionID];
    return (Minion.IconID, Minion.Name);
  }

  internal uint GetRandom(Pointer<ActionManager> actionManager) {
    var availableMinions = _selectableMinions.Where(x => x.IsAvailable(actionManager) && (_config.CurrentCharacter?.OmitIslandMinions != true || !x.Minion.Island)).ToList();
    if (!availableMinions.Any()) {
      return 0;
    }

    // no secure randomness required
    var index = _random.Next(availableMinions.Count);

    return availableMinions[index].Minion.ID;
  }

  internal void UpdateUnlocked(bool enableNewMinions) {
    foreach (var Minion in _selectableMinions) {
      if (!Minion.Minion.Unlocked) {
        Minion.Enabled = enableNewMinions;
      }
    }
  }

  internal void Update(bool enabled, int? page = null) {
    RefreshUnlocked();
    RefreshIsland();
    var list = page is null
            ? _selectableMinions.Where(x => x.Minion.Unlocked).ToList()
            : GetPage(page.Value);
    list.ForEach(x => x.Enabled = enabled);
  }

  private sealed class MinionSelectionData {
    public MinionSelectionData(MinionData minion, bool enabled) {
      Minion = minion;
      Enabled = enabled;
    }

    public MinionData Minion { get; }

    public bool Enabled { get; set; }

    public void Render() {
      Enabled = Minion.Render(Enabled);
    }

    public bool IsAvailable(Pointer<ActionManager> actionManager) {
      return Enabled && Minion.IsAvailable(actionManager);
    }
  }

  private sealed class MinionData {
    private nint? _MinionIcon;
    private nint? _selectedUnselectedIcon;

    public uint ID { get; set; }
    public uint IconID { get; set; }
    public SeString Name { get; init; } = null!;
    public bool Unlocked { get; set; }
    public bool Island { get; set; }

    public bool Render(bool enabled) {
      _ = ImGui.TableNextColumn();

      LoadImages();

      var originalPos = ImGui.GetCursorPos();

      const float ButtonSize = 60f;
      const float OverlaySize = 24f;
      const float OverlayOffset = 4f;
      var buttonSize = new Vector2(ButtonSize);
      var overlaySize = new Vector2(OverlaySize);

      ImGui.PushStyleColor(ImGuiCol.Button, 0);
      ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
      ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

      if (ImGui.ImageButton(_MinionIcon!.Value, buttonSize, Vector2.Zero, Vector2.One, 0)) {
        enabled ^= true;
      }

      ImGui.PopStyleColor(3);

      if (ImGui.IsItemHovered()) {
        ImGui.SetTooltip(Name.RawString);
      }

      var finalPos = ImGui.GetCursorPos();

      // calculate overlay (top right corner) position
      var overlayPos = originalPos + new Vector2(buttonSize.X - overlaySize.X + OverlayOffset, 0);
      ImGui.SetCursorPos(overlayPos);

      Vector2 offset = new(enabled ? 0.1f : 0.6f, 0.2f);
      Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
      ImGui.Image(_selectedUnselectedIcon!.Value, overlaySize, offset, offset2);

      // put cursor back to where it was after rendering the button to prevent
      // messing up the table rendering
      ImGui.SetCursorPos(finalPos);

      return enabled;
    }

    private void LoadImages() {
      _MinionIcon ??= TextureHelper.LoadIconTexture(IconID);
      _selectedUnselectedIcon ??= TextureHelper.LoadUldTexture("readycheck");
    }

    public unsafe bool IsAvailable(Pointer<ActionManager> actionManager) {
      return actionManager.Value->GetActionStatus(ActionType.Unk_8, ID) == 0;
    }
  }
}
