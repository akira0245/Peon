using System;
using Peon.SeFunctions;

namespace Peon.Crops
{
    public enum PlotSize : byte
    {
        Cottage,
        House,
        Mansion,
    }

    public static class Plots
    {
        private static readonly PlotSize[] MistData =
        {
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
        };

        private static readonly PlotSize[] LavenderBedsData =
        {
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
        };

        private static readonly PlotSize[] GobletData =
        {
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion,
        };

        private static readonly PlotSize[] ShiroganeData =
        {
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Mansion,
        };

        private static readonly PlotSize[] FirmamentData =
            { };

        public static PlotSize GetSize(HousingZone zone, ushort plot)
        {
            if (plot > 60)
                throw new ArgumentOutOfRangeException();

            var idx = plot - (plot > 30 ? 31 : 1);
            return zone switch
            {
                HousingZone.Mist         => MistData[idx],
                HousingZone.Goblet       => GobletData[idx],
                HousingZone.LavenderBeds => LavenderBedsData[idx],
                HousingZone.Shirogane    => ShiroganeData[idx],
                HousingZone.Firmament    => FirmamentData[idx],
                _                        => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
