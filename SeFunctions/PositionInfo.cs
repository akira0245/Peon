using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Peon.Utility;

namespace Peon.SeFunctions
{
    public enum HousingZone : byte
    {
        Unknown      = 0,
        Mist         = 83,
        Goblet       = 85,
        LavenderBeds = 84,
        Shirogane    = 129,
        Firmament    = 254,
    }

    public static class HousingZoneExtensions
    {
        public static string ToName(this HousingZone z)
        {
            return z switch
            {
                HousingZone.Unknown      => "Unknown",
                HousingZone.Mist         => StringId.Mist.Value(),
                HousingZone.Goblet       => StringId.Goblet.Value(),
                HousingZone.LavenderBeds => StringId.LavenderBeds.Value(),
                HousingZone.Shirogane    => StringId.Shirogane.Value(),
                HousingZone.Firmament    => StringId.Firmament.Value(),
                _                        => throw new ArgumentOutOfRangeException(nameof(z), z, null)
            };
        }
    }

    public enum Floor : byte
    {
        Unknown = 0xFF,
        Ground  = 0,
        First   = 1,
        Cellar  = 0x0A,
    }

    public sealed class PositionInfoAddress : SeAddressBase
    {
        private readonly unsafe struct PositionInfo
        {
            private readonly byte* _address;

            private PositionInfo(byte* address)
                => _address = address;

            public static implicit operator PositionInfo(IntPtr ptr)
                => new((byte*) ptr);

            public static implicit operator PositionInfo(byte* ptr)
                => new(ptr);

            public static implicit operator bool(PositionInfo ptr)
                => ptr._address != null;

            public ushort House
                => (ushort) (_address == null || !InHouse ? 0 : *(ushort*) (_address + 0x96A0) + 1);

            public ushort Ward
                => (ushort) (_address == null ? 0 : *(ushort*) (_address + 0x96A2) + 1);

            public bool Subdivision
                => _address != null && *(_address + 0x96A9) == 2;

            public HousingZone Zone
                => _address == null ? HousingZone.Unknown : *(HousingZone*) (_address + 0x96A4);

            public byte Plot
                => (byte) (_address == null || InHouse ? 0 : *(_address + 0x96A8) + 1);

            public Floor Floor
                => _address == null ? Floor.Unknown : *(Floor*) (_address + 0x9704);

            private bool InHouse
                => *(_address + 0x96A9) == 0;
        }

        private unsafe PositionInfo Info
        {
            get
            {
                var intermediate = *(byte***) Address;
                return intermediate == null ? null : *intermediate;
            }
        }

        public PositionInfoAddress(SigScanner sigScanner)
            : base(sigScanner, "40 ?? 48 83 ?? ?? 33 DB 48 39 ?? ?? ?? ?? ?? 75 ?? 45")
        { }

        public ushort Ward
            => Info.Ward;

        public HousingZone Zone
            => Info.Zone;

        public ushort House
            => Info.House;

        public bool Subdivision
            => Info.Subdivision;

        public byte Plot
            => Info.Plot;

        public Floor Floor
            => Info.Floor;
    }
}
