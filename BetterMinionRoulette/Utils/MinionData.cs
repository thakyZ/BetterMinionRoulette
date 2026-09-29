using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class MinionData {
  public uint ID { get; init; }

  public uint IconID { get; init; }

  public string Name { get; }

  public bool Unlocked { get; set; }

  public bool Island { get; set; }

  public MinionData(string name) {
    Name = name;
  }

  public Dalamud.Bindings.ImGui.ImTextureID GetIcon() {
    return Services.TextureHelper.LoadIconTexture(IconID);
  }

  public unsafe bool IsAvailable(Pointer<ActionManager> actionManager) {
    return actionManager.Value->GetActionStatus(ActionType.Companion, ID) == 0;
  }
}
