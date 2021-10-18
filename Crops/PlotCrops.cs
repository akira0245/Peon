using System;
using System.Collections.Generic;
using System.Numerics;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Peon.SeFunctions;

namespace Peon.Crops
{
    public class PlotCrops : IEquatable<PlotCrops>
    {
        [JsonIgnore]
        public ulong Key
            => Plot | ((ulong) Ward << 16) | ((ulong) Zone << 32) | ((ulong) ServerId << 48);

        public readonly HousingZone Zone;
        public readonly ushort      ServerId;
        public readonly ushort      Ward;
        public readonly ushort      Plot;

        private readonly PlantInformation[] _beds;

        public IReadOnlyList<PlantInformation> Beds
            => _beds;

        public PlotCrops(HousingZone zone, ushort ward, ushort plot, ushort serverId)
        {
            Zone     = zone;
            ServerId = serverId;
            Ward     = ward;
            Plot     = plot;
            var numBeds = Plots.GetSize(zone, plot) switch
            {
                PlotSize.Cottage => 8 + 2,
                PlotSize.House   => 2 * 8 + 3,
                PlotSize.Mansion => 3 * 8 + 4,
                _                => 0,
            };
            _beds = new PlantInformation[numBeds];
        }

        [JsonConstructor]
        private PlotCrops(HousingZone zone, ushort ward, ushort plot, ushort serverId, PlantInformation[] beds)
        {
            Zone     = zone;
            ServerId = serverId;
            Ward     = ward;
            Plot     = plot;
            _beds    = beds;
        }

        public string GetName()
        {
            var key  = Key;
            var name = Peon.Config.HousingNames.Find(t => t.Item1 == key);
            return name != default
                ? name.Item2
                : $"{Ward:D2}-{Plot:D2}, {Zone.ToName()} ({Dalamud.GameData.GetExcelSheet<World>()!.GetRow(ServerId)!.Name.RawString}) ";
        }

        public string GetName(ushort idx)
            => idx >= OutdoorPlants
                ? $"Pot {idx - OutdoorPlants + 1}"
                : $"Bed {(idx >> 3) + 1}-{(idx & 0b111) + 1}";

        [JsonIgnore]
        public uint IndoorPlants
            => (uint) (_beds.Length & 0b111u);

        [JsonIgnore]
        public uint OutdoorPlants
            => (uint) (_beds.Length & ~0b111u);

        [JsonIgnore]
        public uint Patches
            => (uint) (_beds.Length >> 3);

        public PlantInformation IndoorPlant(uint idx)
        {
            if (idx >= IndoorPlants)
                throw new ArgumentOutOfRangeException();

            return _beds[OutdoorPlants + idx];
        }

        public PlantInformation OutdoorPlant(uint patch, uint bed)
        {
            if (patch >= Patches)
                throw new ArgumentOutOfRangeException();
            if (bed > 7)
                throw new ArgumentOutOfRangeException();

            return _beds[(patch << 3) + bed];
        }

        public bool Update(Vector3 position, uint itemId, DateTime? plantTime, DateTime? tendTime, DateTime? fertilizeTime)
        {
            ushort oldestPlantIdx = 0;
            for (ushort i = 0; i < IndoorPlants; ++i)
            {
                var plant = IndoorPlant(i);
                if (plant.CloseEnough(position))
                    return _beds[OutdoorPlants + i].Update(itemId, plantTime, tendTime, fertilizeTime);

                if (plant.PlantTime < IndoorPlant(oldestPlantIdx).PlantTime)
                    oldestPlantIdx = i;
            }

            return _beds[OutdoorPlants + oldestPlantIdx].Update(itemId, plantTime, tendTime, fertilizeTime, position);
        }

        public bool Update(ushort patch, ushort bed, uint itemId, DateTime? plantTime, DateTime? tendTime, DateTime? fertilizeTime)
        {
            if (patch >= Patches || bed > 7)
                return false;

            return _beds[(patch << 3) + bed].Update(itemId, plantTime, tendTime, fertilizeTime);
        }

        public bool Equals(PlotCrops? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Zone == other.Zone
             && ServerId == other.ServerId
             && Ward == other.Ward
             && Plot == other.Plot;
        }

        public bool Equals(CropSpotIdentification id)
        {
            if (id.Type != CropSpotType.Outdoors && id.Type != CropSpotType.House)
                return false;

            return Plot == id.Plot && Ward == id.Ward && Zone == id.Zone && ServerId == id.ServerId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((PlotCrops) obj);
        }

        public override int GetHashCode()
            => Key.GetHashCode();
    }
}
