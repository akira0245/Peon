using System;
using System.Numerics;

namespace Peon.Crops
{
    public struct PlantInformation
    {
        public DateTime PlantTime;
        public DateTime LastTending;
        public DateTime LastFertilization;
        public Vector3  Position;
        public uint     PlantId;

        public bool Update(uint itemId, DateTime? plantTime, DateTime? tendTime,
            DateTime? fertilizeTime, Vector3? position = null)
        {
            var ret = false;
            if (PlantId != itemId)
            {
                PlantId           = itemId;
                PlantTime         = DateTime.UnixEpoch;
                LastTending       = DateTime.UnixEpoch;
                LastFertilization = DateTime.UnixEpoch;
                ret               = true;
            }

            if (tendTime.HasValue && tendTime.Value != LastTending)
            {
                LastTending = tendTime.Value;
                ret         = true;
            }

            if (fertilizeTime.HasValue && fertilizeTime.Value != LastFertilization)
            {
                LastFertilization = fertilizeTime.Value;
                ret               = true;
            }

            if (plantTime.HasValue && plantTime.Value != PlantTime)
            {
                PlantTime         = plantTime.Value;
                LastTending       = PlantTime;
                LastFertilization = PlantTime;
                ret               = true;
            }

            if (position.HasValue)
            {
                Position = position.Value;
                return true;
            }

            return ret;
        }

        public bool CloseEnough(Vector3 rhs)
            => (Position - rhs).LengthSquared() < 0.01;

        public DateTime FinishTime()
            => PlantTime == DateTime.UnixEpoch ? DateTime.UnixEpoch : PlantTime.AddMinutes(Crops.Find(PlantId).Data.GrowTime);

        public DateTime WiltingTime()
            => LastTending == DateTime.UnixEpoch ? DateTime.UnixEpoch : LastTending.AddMinutes(Crops.Find(PlantId).Data.WiltTime);

        public DateTime DyingTime()
            => LastTending == DateTime.UnixEpoch ? DateTime.UnixEpoch : WiltingTime().AddHours(24);
    }
}
