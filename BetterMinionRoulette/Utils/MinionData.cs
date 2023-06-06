using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Utils;

internal sealed class MinionData
{
    private nint? _minionIcon;
    private readonly TextureHelper _textureHelper;

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public SeString Name { get; }

    public bool Unlocked { get; set; }

    public bool Island { get; set; }

    public MinionData(TextureHelper textureHelper, SeString name)
    {
        _textureHelper = textureHelper;
        Name = name;
    }

    public nint GetIcon()
    {
        _minionIcon ??= _textureHelper.LoadIconTexture(IconID);
        return _minionIcon!.Value;
    }

    public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Unk_8, ID) == 0;
    }
}
