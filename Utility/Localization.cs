using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text.Payloads;
using Peon.Bothers;
using Peon.Crafting;
using Peon.Managers;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace Peon.Utility
{
    [Sheet("RetainerString")]
    public class RetainerString : ExcelRow
    {
        public Lumina.Text.SeString Identifier { get; set; } = null!;
        public Lumina.Text.SeString String     { get; set; } = null!;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            RowId      = parser.Row;
            SubRowId   = parser.SubRow;
            Identifier = parser.ReadColumn<Lumina.Text.SeString>(0)!;
            String     = parser.ReadColumn<Lumina.Text.SeString>(1)!;
        }
    }

    public readonly struct LazyString
    {
        private static Localization? _localization;

        internal static void SetLocalization(Localization loc)
            => _localization = loc;

        internal readonly StringId Id;

        public LazyString(StringId s)
            => Id = s;

        public static implicit operator string(LazyString s)
            => _localization?[s.Id] ?? throw new Exception();

        public static implicit operator LazyString(StringId s)
            => new(s);

        public override string ToString()
            => this;
    }

    public class Localization
    {
        private readonly List<string> _strings;

        public Localization()
        {
            _strings = new List<string>();
            _strings.AddRange(Enumerable.Repeat(string.Empty, Enum.GetValues(typeof(StringId)).Length));
            SetSkills();
            SetChocobo();
            SetLogin();
            SetRetainers();
            SetTargeting();
            SetCrops();
        }

        private void Set(StringId s, string value)
        {
            if (!Enum.IsDefined(typeof(StringId), s))
                throw new InvalidEnumArgumentException();

            _strings[(int) s] = value;
        }

        public string this[StringId s]
        {
            get
            {
                if (Enum.IsDefined(typeof(StringId), s))
                    return _strings[(int) s];

                throw new InvalidEnumArgumentException();
            }
        }

        private void SetSkills()
        {
            var sheet1 = Dalamud.GameData.GetExcelSheet<Action>(Dalamud.ClientState.ClientLanguage)!;
            var sheet2 = Dalamud.GameData.GetExcelSheet<CraftAction>(Dalamud.ClientState.ClientLanguage)!;

            foreach (var action in ActionIdExtensions.Actions.Values)
            {
                var    actionRow = action[CrafterId.Armorer];
                string name      = sheet1.GetRow(actionRow)?.Name ?? sheet2.GetRow(actionRow)!.Name;
                Set(action.Name.Id, name);
            }
        }

        private static readonly char[] _openBrackets =
        {
            '(',
            '[',
        };

        private static readonly char[] _closeBrackets =
        {
            ')',
            ']',
        };

        private void SetRetainers()
        {
            var placeName = Dalamud.GameData.GetExcelSheet<PlaceName>(Dalamud.ClientState.ClientLanguage)!;
            var retainer  = Dalamud.GameData.GetExcelSheet<RetainerTaskRandom>(Dalamud.ClientState.ClientLanguage)!;
            Dalamud.GameData.Excel.RemoveSheetFromCache<RetainerString>();
            var sheet = Dalamud.GameData.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)!
               .MakeGenericMethod(typeof(RetainerString)).Invoke(Dalamud.GameData.Excel, new object?[]
                {
                    "custom/000/CmnDefRetainerCall_00010",
                    Dalamud.ClientState.ClientLanguage.ToLumina(),
                    null,
                }) as ExcelSheet<RetainerString>;
            var addon = Dalamud.GameData.GetExcelSheet<Addon>(Dalamud.ClientState.ClientLanguage)!;

            Set(StringId.SelectCategory,         sheet!.GetRow(194)!.String);
            Set(StringId.SelectOption,           sheet.GetRow(154)!.String);
            Set(StringId.SummoningBell,          placeName.GetRow(1235)!.Name);
            Set(StringId.RetainerTaskComplete,   addon.GetRow(2385)!.Text.ToString());
            Set(StringId.RetainerTaskAvailable,  addon.GetRow(2386)!.Text.ToString());
            Set(StringId.RetainerTaskInProgress, addon.GetRow(2384)!.Text.Payloads[0].RawString!);
            Set(StringId.QuickExploration,       retainer.GetRow(30053)!.Name.ToString().ToLowerInvariant());
            Set(StringId.EntrustGil,             sheet.GetRow(156)!.String.ToString());
            Set(StringId.RetainerReturn,         sheet.GetRow(192)!.String.ToString());
            var x = sheet.GetRow(168)!.String.ToString();
            Set(StringId.RetainerMenuComplete, sheet.GetRow(168)!.String.ToString().Split(_openBrackets)[1].TrimEnd(_closeBrackets));
            Set(StringId.RetainerMenuNone,     sheet.GetRow(166)!.String.ToString().Split(_openBrackets)[1].TrimEnd(_closeBrackets));
        }

        private void SetLogin()
        {
            var sheet = Dalamud.GameData.GetExcelSheet<MainCommand>(Dalamud.ClientState.ClientLanguage)!;
            Set(StringId.LogOut, sheet.GetRow(23)!.Name);
        }

        private static readonly Regex _stableStatusRegex =
            new("\u0005(?'Fair'[^\uFFFD]+)\uFFFD\u0005(?'Poor'[^\uFFFD]+)\uFFFD\u0005(?'Good'[^\u0003]+)\u0003", RegexOptions.Compiled);

        private static string FilterStableStatus(Lumina.Text.SeString seString)
        {
            var match = _stableStatusRegex.Match(seString.RawString);
            if (!match.Success)
            {
                PluginLog.Error("Could not obtain localized stable status string, using 'Good'.");
                return "Good";
            }

            return match.Groups["Good"].Value;
        }

        private void SetChocobo()
        {
            var addon = Dalamud.GameData.GetExcelSheet<Addon>(Dalamud.ClientState.ClientLanguage)!;
            var log   = Dalamud.GameData.GetExcelSheet<LogMessage>(Dalamud.ClientState.ClientLanguage)!;
            Dalamud.GameData.Excel.RemoveSheetFromCache<RetainerString>();
            var sheet = Dalamud.GameData.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)!
               .MakeGenericMethod(typeof(RetainerString)).Invoke(Dalamud.GameData.Excel, new object?[]
                {
                    "custom/002/cmndefhousingbuddystable_00201",
                    Dalamud.ClientState.ClientLanguage.ToLumina(),
                    null,
                }) as ExcelSheet<RetainerString>;
            var placeName = Dalamud.GameData.GetExcelSheet<PlaceName>(Dalamud.ClientState.ClientLanguage);
            Set(StringId.ChocobosStabled,  sheet!.GetRow(1)!.String.Payloads[0].RawString);
            Set(StringId.ChocoboStable,    addon.GetRow(6490)!.Text);
            Set(StringId.TendChocobo,      sheet.GetRow(2)!.String);
            Set(StringId.StableStatusGood, FilterStableStatus(sheet.GetRow(1)!.String));
            Set(StringId.CleanStable,      sheet.GetRow(4)!.String);
            Set(StringId.ChocoboIsResting, log.GetRow(4487)!.Text.Payloads.First(p => p is TextPayload).RawString.RemoveNonSimple());
            Set(StringId.ChocoboIsReady,   addon.GetRow(6497)!.Text);
        }

        private void SetTargeting()
        {
            var log = Dalamud.GameData.GetExcelSheet<LogMessage>(Dalamud.ClientState.ClientLanguage)!;
            Set(StringId.TargetTooFarAway,      log.GetRow(1310)!.Text);
            Set(StringId.TargetTooFarBelow,     log.GetRow(1316)!.Text);
            Set(StringId.TargetTooFarAbove,     log.GetRow(1317)!.Text);
            Set(StringId.TargetInvalidLocation, log.GetRow(1308)!.Text);
            Set(StringId.CannotSeeTarget,       log.GetRow(1315)!.Text);
        }

        private string GetCorrectPayload(Lumina.Text.SeString s, int idxEn, int idxFr, int idxJp, int idxDe)
        {
            return Dalamud.ClientState.ClientLanguage switch
            {
                ClientLanguage.English  => ((TextPayload) s.Payloads[idxEn < 0 ? idxEn + s.Payloads.Count : idxEn]).RawString,
                ClientLanguage.French   => ((TextPayload) s.Payloads[idxFr < 0 ? idxFr + s.Payloads.Count : idxFr]).RawString,
                ClientLanguage.Japanese => ((TextPayload) s.Payloads[idxJp < 0 ? idxJp + s.Payloads.Count : idxJp]).RawString,
                ClientLanguage.German   => ((TextPayload) s.Payloads[idxDe < 0 ? idxDe + s.Payloads.Count : idxDe]).RawString,
                _                       => ((TextPayload) s.Payloads[idxEn < 0 ? idxEn + s.Payloads.Count : idxEn]).RawString,
            };
        }

        private void SetCrops()
        {
            Dalamud.GameData.Excel.RemoveSheetFromCache<RetainerString>();
            var sheet = Dalamud.GameData.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)!
               .MakeGenericMethod(typeof(RetainerString)).Invoke(Dalamud.GameData.Excel, new object?[]
                {
                    "custom/001/cmndefhousinggardeningplant_00151",
                    Dalamud.ClientState.ClientLanguage.ToLumina(),
                    null,
                }) as ExcelSheet<RetainerString>;
            var addon       = Dalamud.GameData.GetExcelSheet<Addon>(Dalamud.ClientState.ClientLanguage)!;
            var territories = Dalamud.GameData.Excel.GetSheet<TerritoryType>()!;
            var names       = Dalamud.GameData.Excel.GetSheet<PlaceName>()!;


            Set(StringId.TendCrop,       sheet!.GetRow(4)!.String.RawString);
            Set(StringId.FertilizeCrop,  sheet.GetRow(3)!.String.RawString);
            Set(StringId.RemoveCrop,     sheet.GetRow(5)!.String.RawString);
            Set(StringId.HarvestCrop,    sheet.GetRow(6)!.String.RawString);
            Set(StringId.PlantCrop,      sheet.GetRow(2)!.String.RawString);
            Set(StringId.DisposeCrop,    sheet.GetRow(11)!.String.RawString);
            Set(StringId.CropBeyondHope, GetCorrectPayload(sheet.GetRow(7)!.String,  -1, -1, -1, 0));
            Set(StringId.CropDoingWell,  GetCorrectPayload(sheet.GetRow(8)!.String,  -1, -1, -1, 0));
            Set(StringId.CropBetterDays, GetCorrectPayload(sheet.GetRow(9)!.String,  -1, -1, -1, 0));
            Set(StringId.CropReady,      GetCorrectPayload(sheet.GetRow(10)!.String, -1, -1, -1, 0));
            Set(StringId.CropPrepareBed, GetCorrectPayload(addon.GetRow(6413)!.Text, 0,  0,  -1, -1));
            Set(StringId.Mist,           names.GetRow(territories.GetRow((uint) HousingZones.Mist)!.PlaceName.Row)!.Name.RawString);
            Set(StringId.LavenderBeds,   names.GetRow(territories.GetRow((uint) HousingZones.LavenderBeds)!.PlaceName.Row)!.Name.RawString);
            Set(StringId.Goblet,         names.GetRow(territories.GetRow((uint) HousingZones.Goblet)!.PlaceName.Row)!.Name.RawString);
            Set(StringId.Shirogane,      names.GetRow(territories.GetRow((uint) HousingZones.Shirogane)!.PlaceName.Row)!.Name.RawString);
            Set(StringId.Firmament, "Firmament"); //names.GetRow(territories.GetRow((uint)HousingZones.Firmament)!.PlaceName.Row)!.Name.RawString);
        }
    }

    public enum StringId
    {
        Empty,

        // Skills
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

        // Retainer
        SelectCategory,
        SelectOption,
        RetainerTaskComplete,
        RetainerTaskInProgress,
        RetainerTaskAvailable,
        SummoningBell,
        QuickExploration,
        EntrustGil,
        RetainerReturn,
        RetainerMenuComplete,
        RetainerMenuNone,

        // Login
        LogOut,

        // Chocobo,
        ChocobosStabled,
        ChocoboStable,
        TendChocobo,
        StableStatusGood,
        CleanStable,
        ChocoboIsResting,
        ChocoboIsReady,

        // Targeting
        TargetTooFarAway,
        CannotSeeTarget,
        TargetTooFarBelow,
        TargetTooFarAbove,
        TargetInvalidLocation,

        // Crops
        TendCrop,
        FertilizeCrop,
        RemoveCrop,
        HarvestCrop,
        PlantCrop,
        DisposeCrop,
        CropBeyondHope,
        CropDoingWell,
        CropBetterDays,
        CropReady,
        CropPrepareBed,
        Mist,
        LavenderBeds,
        Goblet,
        Shirogane,
        Firmament,
    }

    public static class StringIdExtensions
    {
        public static LazyString Value(this StringId s)
            => new(s);

        public static CompareString Cs(this StringId s)
            => new(s);

        public static bool Equal(this StringId s, string rhs)
            => s.Value() == rhs;
    }
}
