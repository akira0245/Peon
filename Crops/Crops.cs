using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace Peon.Crops
{
    public readonly struct CropData
    {
        public readonly ushort GrowTime;
        public readonly ushort WiltTime;
        public readonly uint   ItemId;
        public readonly uint   SeedId;

        public ushort WitherTime
            => (ushort) (WiltTime + 24 * 60);

        internal CropData(ushort growTime, ushort wiltTime, uint itemId, uint seedId)
        {
            GrowTime = (ushort) (growTime * 60);
            WiltTime = (ushort) (wiltTime * 60);
            ItemId   = itemId;
            SeedId   = seedId;
        }
    }


    public static class Crops
    {
        private static readonly CropData[] Data = new CropData[]
        {
            new(0 * 00, 00, 00000, 00000), // Nothing
            new(5 * 24, 48, 04835, 07738), // Ala Mhigan Mustard
            new(7 * 24, 24, 30873, 30364), // Allagan Melon
            new(5 * 24, 48, 04842, 07744), // Almond
            new(1 * 22, 72, 15857, 15855), // Althyk Lavender
            new(6 * 24, 36, 07592, 07751), // Apricot
            new(6 * 24, 36, 07769, 07746), // Azeyma Rose
            new(5 * 24, 48, 04830, 07737), // Black Pepper
            new(5 * 24, 48, 04814, 07730), // Blood Currant
            new(7 * 24, 24, 13754, 13755), // Blood Pepper
            new(7 * 24, 24, 07776, 07757), // Broombrush
            new(5 * 24, 48, 05542, 07740), // Chamomile
            new(5 * 24, 48, 12884, 13766), // Chive
            new(2 * 24, 72, 08162, 08179), // Cieldalaes Pineapple
            new(1 * 24, 72, 17548, 17547), // Cloud Acorn
            new(5 * 24, 48, 20791, 20792), // Cloudsbreath
            new(5 * 24, 48, 04778, 07717), // Coerthan Carrot
            new(5 * 24, 48, 12882, 13768), // Coerthan Tea
            new(5 * 24, 48, 07894, 08169), // Curiel Root
            new(6 * 24, 36, 07603, 07749), // Dalamud Popoto Set
            new(2 * 24, 72, 08158, 08175), // Doman Plum
            new(5 * 24, 48, 06148, 07724), // Dzemael Tomato
            new(1 * 18, 72, 00005, 15868), // Earth Shard
            new(7 * 24, 24, 08193, 08185), // Eggplant Knight
            new(5 * 24, 48, 04810, 07727), // Fairie Apple
            new(1 * 18, 72, 00002, 15865), // Firelight
            new(5 * 24, 48, 07735, 07735), // Garlic Cloves
            new(7 * 24, 24, 08194, 08186), // Garlic Jester
            new(7 * 24, 24, 07775, 07756), // Glazenut
            new(3 * 24, 24, 04868, 08572), // Gysahl Greens
            new(6 * 24, 36, 07768, 07745), // Halone Gerbera
            new(2 * 24, 72, 08163, 08180), // Han Lemon
            new(5 * 24, 48, 06147, 07733), // Honey Lemon
            new(1 * 18, 72, 00003, 15866), // Icelight
            new(7 * 24, 24, 07774, 07755), // Jute
            new(3 * 24, 24, 08165, 08182), // Krakka Root
            new(6 * 24, 36, 07593, 07752), // La Noscean Leek
            new(5 * 24, 48, 04782, 07718), // La Noscean Lettuce
            new(5 * 24, 48, 04809, 07725), // La Noscean Orange
            new(5 * 24, 48, 05539, 07736), // Lavender
            new(1 * 18, 72, 00006, 15869), // Levinlight
            new(5 * 24, 48, 05346, 07741), // Linseed
            new(5 * 24, 48, 04808, 07726), // Lowland Grape
            new(2 * 24, 72, 08159, 08176), // Mamook Pear
            new(7 * 24, 24, 08196, 08188), // Mandragora Queen
            new(5 * 24, 48, 05543, 07743), // Mandrake
            new(5 * 24, 48, 04837, 07742), // Midland Basil
            new(5 * 24, 48, 04789, 07723), // Midland Cabbage
            new(5 * 24, 48, 04821, 07721), // Millioncorn
            new(5 * 24, 48, 07897, 08171), // Mimett Gourd
            new(5 * 24, 48, 06146, 07731), // Mirror Apple
            new(6 * 24, 36, 07770, 07747), // Nymeia Lily
            new(2 * 24, 72, 08161, 08178), // O'Ghomoro Berry
            new(5 * 24, 48, 12896, 13765), // Old World Fig
            new(5 * 24, 48, 04804, 07719), // Olive
            new(7 * 24, 24, 08192, 08184), // Onion Prince
            new(5 * 24, 48, 07900, 08173), // Pahsana Fruit
            new(5 * 24, 48, 04785, 07715), // Paprika
            new(5 * 24, 48, 04836, 07739), // Pearl Ginger Root
            new(6 * 24, 36, 08023, 08167), // Pearl Roselle
            new(5 * 24, 48, 12877, 13767), // Pearl Sprout
            new(5 * 24, 48, 04812, 07729), // Pixie Plum
            new(5 * 24, 48, 04787, 07720), // Popoto Set
            new(5 * 24, 48, 04816, 07734), // Prickly Pineapple
            new(5 * 24, 48, 04815, 07732), // Rolanberry
            new(5 * 24, 24, 20793, 20794), // Royal Fern
            new(6 * 24, 36, 07604, 07750), // Royal Kukuru
            new(6 * 24, 36, 07591, 07753), // Shroud Tea
            new(6 * 24, 36, 07602, 07748), // Star Anise
            new(5 * 24, 48, 04811, 07728), // Sun Lemon
            new(5 * 24, 48, 07895, 08170), // Sylkis Bud
            new(5 * 24, 48, 07898, 08172), // Tantalplant
            new(5 * 48, 24, 08166, 08183), // Thavnairian Onion
            new(7 * 24, 24, 08195, 08187), // Tomato King
            new(7 * 24, 24, 07773, 07754), // Umbrella Fig
            new(2 * 24, 72, 08160, 08177), // Valfruit
            new(1 * 13, 72, 15858, 15856), // Voidrake
            new(1 * 18, 72, 00007, 15870), // Waterlight
            new(5 * 24, 48, 04777, 07716), // Wild Onion Set
            new(1 * 18, 72, 00004, 15867), // Windlight
            new(5 * 24, 48, 04788, 07722), // Wizard Eggplant
            new(2 * 24, 72, 08157, 08174), // Xelphatol Apple
        };

        private static Dictionary<string, CropData>?         _nameToData;
        private static Dictionary<uint, (CropData, string)>? _idToData;

        private static IReadOnlyDictionary<string, CropData> GetNameData()
        {
            if (_nameToData == null)
            {
                var sheet = Dalamud.GameData.GetExcelSheet<Item>()!;
                _nameToData = new Dictionary<string, CropData>(Data.Length * 2);
                foreach (var data in Data)
                {
                    var itemName = sheet.GetRow(data.ItemId)!.Singular.ToString().ToLowerInvariant();
                    var seedName = sheet.GetRow(data.SeedId)!.Singular.ToString().ToLowerInvariant();
                    _nameToData[itemName] = data;
                    _nameToData[seedName] = data;
                }
            }

            return _nameToData;
        }

        private static IReadOnlyDictionary<uint, (CropData, string)> GetIdData()
        {
            if (_idToData == null)
            {
                var sheet    = Dalamud.GameData.GetExcelSheet<Item>()!;
                _idToData = new Dictionary<uint, (CropData, string)>(Data.Length);
                foreach (var data in Data)
                {
                    var itemName = sheet.GetRow(data.ItemId)!.Name.ToString();
                    _idToData.TryAdd(data.ItemId, (data, itemName));
                }
            }

            return _idToData;
        }

        public static (CropData Data, string Name) Find(uint itemId)
            => GetIdData().TryGetValue(itemId, out var crop) ? crop : (Data[0], string.Empty);

        public static CropData Find(string name)
            => GetNameData().TryGetValue(name.ToLowerInvariant(), out var crop) ? crop : Data[0];
    }
}
