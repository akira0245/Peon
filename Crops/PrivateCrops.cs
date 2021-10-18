using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace Peon.Crops
{
    public class PrivateCrops : IEquatable<PrivateCrops>
    {
        public readonly string PlayerName;
        public readonly ushort ServerId;

        private readonly PlantInformation[] _beds;

        public IReadOnlyList<PlantInformation> Beds
            => _beds;

        public PrivateCrops(string playerName, ushort serverId)
        {
            PlayerName = playerName;
            ServerId   = serverId;

            _beds = new PlantInformation[4];
        }

        [JsonConstructor]
        private PrivateCrops(string playerName, ushort serverId, PlantInformation[] beds)
        {
            PlayerName = playerName;
            ServerId   = serverId;
            _beds = beds;
        }

        public string GetName()
            => PlayerName;

        public string GetName(ushort idx)
            => idx < 2 ? $"Apartment, Pot {idx + 1}" : $"Chambers, Pot {idx - 1}";

        public PlantInformation Bed(ushort idx)
            => _beds[idx];

        public PlantInformation ApartmentPlant(ushort idx)
        {
            if (idx > 1)
                throw new ArgumentOutOfRangeException();

            return _beds[idx];
        }

        public PlantInformation ChambersPlant(ushort idx)
        {
            if (idx > 1)
                throw new ArgumentOutOfRangeException();

            return _beds[2 + idx];
        }

        public bool Update(CropSpotType type, Vector3 position, uint itemId, DateTime? plantTime, DateTime? tendTime,
            DateTime? fertilizeTime)
        {
            return type switch
            {
                CropSpotType.Apartment when _beds[0].CloseEnough(position) => _beds[0].Update(itemId, plantTime, tendTime, fertilizeTime),
                CropSpotType.Apartment when _beds[1].CloseEnough(position) => _beds[1].Update(itemId, plantTime, tendTime, fertilizeTime),
                CropSpotType.Apartment => _beds[_beds[0].PlantTime <= _beds[1].PlantTime ? 0 : 1]
                   .Update(itemId, plantTime, tendTime, fertilizeTime, position),
                CropSpotType.Chambers when _beds[2].CloseEnough(position) => _beds[2].Update(itemId, plantTime, tendTime, fertilizeTime),
                CropSpotType.Chambers when _beds[3].CloseEnough(position) => _beds[3].Update(itemId, plantTime, tendTime, fertilizeTime),
                CropSpotType.Chambers => _beds[_beds[2].PlantTime <= _beds[3].PlantTime ? 2 : 3]
                   .Update(itemId, plantTime, tendTime, fertilizeTime, position),
                _ => false,
            };
        }

        public bool Equals(PrivateCrops? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return PlayerName == other.PlayerName
             && ServerId == other.ServerId;
        }

        public bool Equals(CropSpotIdentification id)
        {
            if (id.Type != CropSpotType.Apartment && id.Type != CropSpotType.Chambers)
                return false;

            return id.PlayerName == PlayerName && id.ServerId == ServerId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((PrivateCrops) obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(PlayerName, ServerId);
    }
}
