using System;
using System.Collections.Generic;
using System.Linq;

namespace Peon.Crops
{
    public class CropTimers
    {
        public List<PlotCrops>    Plots        = new();
        public List<PrivateCrops> PrivateCrops = new();

        private PlotCrops FindPlotCrop(CropSpotIdentification id)
        {
            foreach (var crop in Plots.Where(crop => crop!.Equals(id)))
                return crop;

            var ret = new PlotCrops(id.Zone, id.Ward, id.Plot, id.ServerId);
            Plots.Add(ret);
            return ret;
        }

        private PrivateCrops FindPrivateCrop(CropSpotIdentification id)
        {
            foreach (var crop in PrivateCrops.Where(crop => crop!.Equals(id)))
                return crop;

            var ret = new PrivateCrops(id.PlayerName, id.ServerId);
            PrivateCrops.Add(ret);
            return ret;
        }

        private bool Update(CropSpotIdentification id, uint itemId, DateTime? plantTime, DateTime? tendTime, DateTime? fertilizeTime)
        {
            switch (id.Type)
            {
                case CropSpotType.Invalid: return false;
                case CropSpotType.Apartment:
                case CropSpotType.Chambers:
                {
                    var crop = FindPrivateCrop(id);
                    return crop.Update(id.Type, id.Position, itemId, plantTime, tendTime, fertilizeTime);
                }
                case CropSpotType.House:
                {
                    var crop = FindPlotCrop(id);
                    return crop.Update(id.Position, itemId, plantTime, tendTime, fertilizeTime);
                }
                case CropSpotType.Outdoors:
                {
                    var crop = FindPlotCrop(id);
                    return crop.Update(id.Patch, id.Bed, itemId, plantTime, tendTime, fertilizeTime);
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public bool HarvestCrop(CropSpotIdentification id)
            => Update(id, 0, null, null, null);

        public bool PlantCrop(CropSpotIdentification id, uint itemId, DateTime plantTime)
            => Update(id, itemId, plantTime, null, null);

        public bool TendCrop(CropSpotIdentification id, uint itemId, DateTime tendTime)
            => Update(id, itemId, null, tendTime, null);

        public bool FertilizeCrop(CropSpotIdentification id, uint itemId, DateTime fertilizeTime)
            => Update(id, itemId, null, null, fertilizeTime);
    }
}
