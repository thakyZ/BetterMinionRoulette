using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.Interop.Attributes;

namespace BetterMinionRoulette.Agent;

// ctor 1413EA840 ? - extends EventHandler
[StructLayout(LayoutKind.Explicit, Size = 0xAE0)]
public unsafe partial struct MJIPastureHandlerExtension {
  /// <summary>
  /// An array representing which minions are currently out roaming the Island. This array is indexed by row ID from
  /// the Companion EXD sheet. See <see cref="MinionSlots"/> if information about minion locations is required.
  /// </summary>
  // Warning: This array will change size every time new minions are added!!
  [FixedSizeArray<bool>(480)]
  [FieldOffset(0x6F8)] public fixed byte RoamingMinions[480];

  /// <summary>
  /// An array representing which minions are currently out roaming the Island. This array is indexed by row ID from
  /// the Companion EXD sheet. See <see cref="MinionSlots"/> if information about minion locations is required.
  /// </summary>
  // Warning: This array will change size every time new minions are added!!
  public Span<bool> RoamingMinionList => new(Unsafe.AsPointer(ref RoamingMinions[0]), 480);

  /// <summary>
  /// An array containing information on all the minion slots present on the Island Sanctuary.
  /// This array is indexed by an internal ID and does not appear to be grouped by location or similar.
  /// </summary>
  [FixedSizeArray<MJIMinionSlot>(40)]
  [FieldOffset(0x8B8)] public fixed byte MinionSlots[40 * MJIMinionSlot.Size];

  /// <summary>
  /// An array containing information on all the minion slots present on the Island Sanctuary.
  /// This array is indexed by an internal ID and does not appear to be grouped by location or similar.
  /// </summary>
  public Span<MJIMinionSlot> MinionSlotsList => new(Unsafe.AsPointer(ref MinionSlots[0]), 40);
}

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct MJIMinionSlot {
  public const int Size = 0xC;

  /// <summary>
  /// An internal ID used to track minion slots.
  /// </summary>
  /// <remarks>
  /// May be set to 40 if the slot is currently empty or uninitialized.
  /// </remarks>
  [FieldOffset(0x0)] public byte SlotId;

  [FieldOffset(0x4)] public uint ObjectId;
  [FieldOffset(0x8)] public ushort MinionId;

  /// <summary>
  /// The MJIMinionPopAreaId that this minion currently resides in.
  /// </summary>
  [FieldOffset(0xA)] public byte PopAreaId;

  /// <summary>
  /// Check if this specific Minion Slot contains a minion or not.
  /// </summary>
  /// <returns>Returns <c>true</c> if a minion is present, <c>false</c> otherwise.</returns>
  public bool IsSlotPopulated() {
    return this.MinionId != 0;
  }
}
