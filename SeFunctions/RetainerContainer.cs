using System.Runtime.InteropServices;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    [StructLayout(LayoutKind.Explicit, Size = Size)]
    public unsafe struct Retainer
    {
        public const                     int   Size = 0x48;
        [FieldOffset(0x00)] public       ulong RetainerID;
        [FieldOffset(0x08)] public fixed byte  Name[0x20];
        [FieldOffset(0x29)] public       byte  Available;
        [FieldOffset(0x29)] public       byte  ClassJob;
        [FieldOffset(0x2C)] public       uint  Gil;
        [FieldOffset(0x38)] public       uint  VentureID;
        [FieldOffset(0x3C)] public       uint  VentureComplete;
    }

    [StructLayout(LayoutKind.Sequential, Size = Retainer.Size * 10 + 12)]
    public unsafe struct RetainerContainer
    {
        public fixed byte Retainers[Retainer.Size * 10];
        public fixed byte DisplayOrder[10];
        public       byte Ready;
        public       byte RetainerCount;
    }

    public sealed class StaticRetainerContainer : SeAddressBase
    {
        public StaticRetainerContainer(SigScanner sigScanner)
            : base(sigScanner, "48 8B E9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 4E")
        { }
    }
}
