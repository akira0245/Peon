using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Peon.Utility;

namespace Peon.Crafting
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionId : byte
    {
        None,
        BasicSynthesis,
        BasicTouch,
        MastersMend,
        HastyTouch,
        RapidSynthesis,
        InnerQuiet,
        Observe,
        TricksOfTheTrade,
        WasteNot,
        Veneration,
        StandardTouch,
        GreatStrides,
        Innovation,
        NameOfTheElements,
        BrandOfTheElements,
        FinalAppraisal,
        WasteNot2,
        ByregotsBlessing,
        PreciseTouch,
        MuscleMemory,
        CarefulObservation,
        CarefulSynthesis,
        PatientTouch,
        Manipulation,
        PrudentTouch,
        FocusedSynthesis,
        FocusedTouch,
        Reflect,
        PreparatoryTouch,
        Groundwork,
        DelicateSynthesis,
        IntensiveSynthesis,
        TrainedEye,
    };

    public enum CrafterId
    {
        Carpenter,
        Blacksmith,
        Armorer,
        Goldsmith,
        Leatherworker,
        Weaver,
        Alchemist,
        Culinarian,
    }

    public readonly struct ActionInfo
    {
        public readonly LazyString Name;
        public readonly int        Delay;

        private readonly (uint, uint, uint, uint, uint, uint, uint, uint) _gameId;
        public readonly  ActionId                                         Id;

        public ActionInfo(ActionId id, StringId name, int delay, uint crp, uint bsm, uint arm, uint gsm, uint ltw, uint wvr,
            uint alc, uint cul)
        {
            Name    = new LazyString(name);
            Id      = id;
            Delay   = delay;
            _gameId = (crp, bsm, arm, gsm, ltw, wvr, alc, cul);
        }

        public uint this[CrafterId crafter]
        {
            get
            {
                return crafter switch
                {
                    CrafterId.Carpenter     => _gameId.Item1,
                    CrafterId.Blacksmith    => _gameId.Item2,
                    CrafterId.Armorer       => _gameId.Item3,
                    CrafterId.Goldsmith     => _gameId.Item4,
                    CrafterId.Leatherworker => _gameId.Item5,
                    CrafterId.Weaver        => _gameId.Item6,
                    CrafterId.Alchemist     => _gameId.Item7,
                    CrafterId.Culinarian    => _gameId.Item8,
                    _                       => throw new InvalidEnumArgumentException(),
                };
            }
        }

        public string Cast()
            => $"/ac \"{Name}\"";
    }


    public static class ActionIdExtensions
    {
        // @formatter:off
        public static readonly Dictionary<ActionId, ActionInfo> Actions = new()
        {
            { ActionId.None,               new ActionInfo(ActionId.None,               StringId.Empty,                   0,      0,      0,      0,      0,      0,      0,      0,      0 ) },
            { ActionId.BasicSynthesis,     new ActionInfo(ActionId.BasicSynthesis,     StringId.BasicSynthesis,       2500, 100001, 100015, 100030, 100075, 100045, 100060, 100090, 100105 ) },
            { ActionId.BasicTouch,         new ActionInfo(ActionId.BasicTouch,         StringId.BasicTouch,           2500, 100002, 100016, 100031, 100076, 100046, 100061, 100091, 100106 ) },
            { ActionId.MastersMend,        new ActionInfo(ActionId.MastersMend,        StringId.MastersMend,          2500, 100003, 100017, 100032, 100077, 100047, 100062, 100092, 100107 ) },
            { ActionId.HastyTouch,         new ActionInfo(ActionId.HastyTouch,         StringId.HastyTouch,           2500, 100355, 100356, 100357, 100358, 100359, 100360, 100361, 100362 ) },
            { ActionId.RapidSynthesis,     new ActionInfo(ActionId.RapidSynthesis,     StringId.RapidSynthesis,       2500, 100363, 100364, 100365, 100366, 100367, 100368, 100369, 100370 ) },
            { ActionId.InnerQuiet,         new ActionInfo(ActionId.InnerQuiet,         StringId.InnerQuiet,           1500,    252,    253,    254,    255,    257,    256,    258,    259 ) },
            { ActionId.Observe,            new ActionInfo(ActionId.Observe,            StringId.Observe,              2500, 100010, 100023, 100040, 100082, 100053, 100070, 100099, 100113 ) },
            { ActionId.TricksOfTheTrade,   new ActionInfo(ActionId.TricksOfTheTrade,   StringId.TricksOfTheTrade,     2500, 100371, 100372, 100373, 100374, 100375, 100376, 100377, 100378 ) },
            { ActionId.WasteNot,           new ActionInfo(ActionId.WasteNot,           StringId.WasteNot,             1500,   4631,   4632,   4633,   4634,   4635,   4636,   4637,   4638 ) },
            { ActionId.Veneration,         new ActionInfo(ActionId.Veneration,         StringId.Veneration,           1500,  19297,  19298,  19299,  19300,  19301,  19302,  19303,  19304 ) },
            { ActionId.StandardTouch,      new ActionInfo(ActionId.StandardTouch,      StringId.StandardTouch,        2500, 100004, 100018, 100034, 100078, 100048, 100064, 100093, 100109 ) },
            { ActionId.GreatStrides,       new ActionInfo(ActionId.GreatStrides,       StringId.GreatStrides,         1500,    260,    261,    262,    263,    265,    264,    266,    267 ) },
            { ActionId.Innovation,         new ActionInfo(ActionId.Innovation,         StringId.Innovation,           1500,  19004,  19005,  19006,  19007,  19008,  19009,  19010,  19011 ) },
            { ActionId.NameOfTheElements,  new ActionInfo(ActionId.NameOfTheElements,  StringId.NameOfTheElements,    1500,   4615,   4616,   4617,   4618,   4620,   4619,   4621,   4622 ) },
            { ActionId.BrandOfTheElements, new ActionInfo(ActionId.BrandOfTheElements, StringId.BrandOfTheElements,   2500, 100331, 100332, 100333, 100334, 100335, 100336, 100337, 100338 ) },
            { ActionId.FinalAppraisal,     new ActionInfo(ActionId.FinalAppraisal,     StringId.FinalAppraisal,       1500,  19012,  19013,  19014,  19015,  19016,  19017,  19018,  19019 ) },
            { ActionId.WasteNot2,          new ActionInfo(ActionId.WasteNot2,          StringId.WasteNot2,            1500,   4639,   4640,   4641,   4642,   4643,   4644,   19002, 19003 ) },
            { ActionId.ByregotsBlessing,   new ActionInfo(ActionId.ByregotsBlessing,   StringId.ByregotsBlessing,     2500, 100339, 100340, 100341, 100342, 100343, 100344, 100345, 100346 ) },
            { ActionId.PreciseTouch,       new ActionInfo(ActionId.PreciseTouch,       StringId.PreciseTouch,         2500, 100128, 100129, 100130, 100131, 100132, 100133, 100134, 100135 ) },
            { ActionId.MuscleMemory,       new ActionInfo(ActionId.MuscleMemory,       StringId.MuscleMemory,         2500, 100379, 100380, 100381, 100382, 100383, 100384, 100385, 100386 ) },
            { ActionId.CarefulObservation, new ActionInfo(ActionId.CarefulObservation, StringId.CarefulObservation,   2500, 100395, 100396, 100397, 100398, 100399, 100400, 100401, 100402 ) },
            { ActionId.CarefulSynthesis,   new ActionInfo(ActionId.CarefulSynthesis,   StringId.CarefulSynthesis,     2500, 100203, 100204, 100205, 100206, 100207, 100208, 100209, 100210 ) },
            { ActionId.PatientTouch,       new ActionInfo(ActionId.PatientTouch,       StringId.PatientTouch,         2500, 100219, 100220, 100221, 100222, 100223, 100224, 100225, 100226 ) },
            { ActionId.Manipulation,       new ActionInfo(ActionId.Manipulation,       StringId.Manipulation,         1500,   4574,   4575,   4576,   4577,   4578,   4579,   4580,   4581 ) },
            { ActionId.PrudentTouch,       new ActionInfo(ActionId.PrudentTouch,       StringId.PrudentTouch,         2500, 100227, 100228, 100229, 100230, 100231, 100232, 100233, 100234 ) },
            { ActionId.FocusedSynthesis,   new ActionInfo(ActionId.FocusedSynthesis,   StringId.FocusedSynthesis,     2500, 100235, 100236, 100237, 100238, 100239, 100240, 100241, 100242 ) },
            { ActionId.FocusedTouch,       new ActionInfo(ActionId.FocusedTouch,       StringId.FocusedTouch,         2500, 100243, 100244, 100245, 100246, 100247, 100248, 100249, 100250 ) },
            { ActionId.Reflect,            new ActionInfo(ActionId.Reflect,            StringId.Reflect,              2500, 100387, 100388, 100389, 100390, 100391, 100392, 100393, 100394 ) },
            { ActionId.PreparatoryTouch,   new ActionInfo(ActionId.PreparatoryTouch,   StringId.PreparatoryTouch,     2500, 100299, 100300, 100301, 100302, 100303, 100304, 100305, 100306 ) },
            { ActionId.Groundwork,         new ActionInfo(ActionId.Groundwork,         StringId.Groundwork,           2500, 100403, 100404, 100405, 100406, 100407, 100408, 100409, 100410 ) },
            { ActionId.DelicateSynthesis,  new ActionInfo(ActionId.DelicateSynthesis,  StringId.DelicateSynthesis,    2500, 100323, 100324, 100325, 100326, 100327, 100328, 100329, 100330 ) },
            { ActionId.IntensiveSynthesis, new ActionInfo(ActionId.IntensiveSynthesis, StringId.IntensiveSynthesis,   2500, 100315, 100316, 100317, 100318, 100319, 100320, 100321, 100322 ) },
            { ActionId.TrainedEye,         new ActionInfo(ActionId.TrainedEye,         StringId.TrainedEye,           1500, 100283, 100284, 100285, 100286, 100287, 100288, 100289, 100290 ) },
        };
        // @formatter:on

        public static ActionInfo Use(this ActionId id)
            => Actions[id];


        public static ActionInfo Use(this ActionId id, Status status, bool basicTouchCombo)
        {
            return id switch
            {
                ActionId.BasicTouch => status.Improved() ? ActionId.PreciseTouch.Use() :
                    basicTouchCombo                      ? ActionId.StandardTouch.Use() : id.Use(),
                ActionId.ByregotsBlessing => status == Status.Poor ? ActionId.BasicTouch.Use() : id.Use(),
                ActionId.GreatStrides     => status == Status.Excellent ? ActionId.ByregotsBlessing.Use() : id.Use(),
                ActionId.StandardTouch    => status.Improved() ? ActionId.PreciseTouch.Use() : id.Use(),
                ActionId.FocusedTouch     => status.Improved() ? ActionId.PreciseTouch.Use() : id.Use(),
                ActionId.PreparatoryTouch => status.Improved() ? ActionId.PreciseTouch.Use() : id.Use(),
                _                         => id.Use(),
            };
        }
    }
}
