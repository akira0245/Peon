using System;
using System.Globalization;
using System.Numerics;
using Peon.SeFunctions;

namespace Peon.Crops
{
    public enum CropSpotType
    {
        Invalid,
        Apartment,
        Chambers,
        House,
        Outdoors,
    }

    public struct CropSpotIdentification
    {
        public CropSpotType Type;
        public string       PlayerName;
        public Vector3      Position;
        public HousingZone  Zone;
        public ushort       ServerId;
        public ushort       Ward;
        public ushort       Plot;
        public ushort       Patch;
        public ushort       Bed;

        public static readonly CropSpotIdentification Invalid = new() { Type = CropSpotType.Invalid };
    }
}
