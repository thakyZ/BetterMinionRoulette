using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class MinionData {
  private nint? _minionIcon;

  public uint ID { get; init; }

  public uint IconID { get; init; }

  public string Name { get; }

  public bool Unlocked { get; set; }

  public bool Island { get; set; }

  public MinionData(string name) {
    Name = name;
  }

  public nint GetIcon() {
    _minionIcon ??= Services.TextureHelper.LoadIconTexture(IconID);
    return _minionIcon!.Value;
  }

  public unsafe bool IsAvailable(Pointer<ActionManager> actionManager) {
    return actionManager.Value->GetActionStatus(ActionType.Companion, ID) == 0;
  }
}
