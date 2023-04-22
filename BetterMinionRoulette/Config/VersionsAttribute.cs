using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Config;
[AttributeUsage(AttributeTargets.Property)]
internal sealed class VersionsAttribute : Attribute {
  public VersionsAttribute(int introduced, int removed = 0) {
    Introduced = introduced;
    Removed = removed;
  }

  public int Introduced { get; }

  public int Removed { get; }
}
