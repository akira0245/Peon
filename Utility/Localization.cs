using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text.Payloads;
using Peon.Bothers;
using Peon.Crafting;
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
        private readonly List<string>           _strings;

        public Localization()
        {
            _strings = new List<string>();
            _strings.AddRange(Enumerable.Repeat(string.Empty, Enum.GetValues(typeof(StringId)).Length));
            SetSkills();
            SetChocobo();
            SetLogin();
            SetRetainers();
            SetTargeting();
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

            Set(StringId.SelectCategory,         sheet!.GetRow(194)!.String);
            Set(StringId.SelectOption,           sheet.GetRow(154)!.String);
            Set(StringId.SummoningBell,          placeName.GetRow(1235)!.Name);
            Set(StringId.RetainerTaskComplete,   sheet.GetRow(168)!.String);
            Set(StringId.RetainerTaskAvailable,  sheet.GetRow(165)!.String);
            Set(StringId.RetainerTaskInProgress, sheet.GetRow(167)!.String);
            Set(StringId.QuickExploration,       retainer.GetRow(30053)!.Name.ToString().ToLowerInvariant());
            Set(StringId.EntrustGil,             sheet.GetRow(156)!.String.ToString());
            Set(StringId.RetainerReturn,         sheet.GetRow(192)!.String.ToString());
            Set(StringId.RetainerMenuComplete,   sheet.GetRow(168)!.String.ToString().Split('(')[1].TrimEnd(')'));
            Set(StringId.RetainerMenuNone,       sheet.GetRow(166)!.String.ToString().Split('(')[1].TrimEnd(')'));
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
