using System;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Crops;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    public enum HousingZones : ushort
    {
        Mist                  = 339,
        LavenderBeds          = 340,
        Goblet                = 341,
        Shirogane             = 641,
        Firmament             = ushort.MaxValue,
        ChambersMist          = 384,
        ChambersLavenderBeds  = 385,
        ChambersGoblet        = 386,
        ChambersShirogane     = 652,
        ChambersFirmament     = ushort.MaxValue - 1,
        ApartmentMist         = 608,
        ApartmentLavenderBeds = 609,
        ApartmentGoblet       = 610,
        ApartmentShirogane    = 655,
        ApartmentFirmament    = ushort.MaxValue - 2,
        CottageMist           = 282,
        CottageLavenderBeds   = 342,
        CottageGoblet         = 345,
        CottageShirogane      = 649,
        CottageFirmament      = ushort.MaxValue - 3,
        HouseMist             = 283,
        HouseLavenderBeds     = 343,
        HouseGoblet           = 346,
        HouseShirogane        = 650,
        HouseFirmament        = ushort.MaxValue - 4,
        MansionMist           = 284,
        MansionLavenderBeds   = 344,
        MansionGoblet         = 347,
        MansionShirogane      = 651,
        MansionFirmament      = ushort.MaxValue - 5,
    }

    public unsafe partial class TimerManager
    {
        internal         string                             LastPlant = string.Empty;
        internal         ushort                             LastPatch = ushort.MaxValue;
        internal         ushort                             LastBed   = ushort.MaxValue;
        private          Hook<OnAddonReceiveEventDelegate>? _selectStringHook;
        private          Hook<OnAddonReceiveEventDelegate>? _selectYesnoHook;
        private readonly PositionInfoAddress                _position;


        public void SetupPlants()
        {
            _watcher.OnTalkUpdate += CheckPlant;
            _selectStringHook     =  Service<SelectStringReceiveEvent>.Get().CreateHook(SelectStringEventDetour);
            _selectYesnoHook      =  Service<SelectYesnoReceiveEvent>.Get().CreateHook(SelectYesnoEventDetour);
        }

        public void DisposePlants()
        {
            _watcher.OnTalkUpdate -= CheckPlant;
            _selectYesnoHook?.Dispose();
            _selectStringHook?.Dispose();
        }

        private static string ParseText(PtrTalk talk)
        {
            var nodeText = talk.Pointer->AtkTextNode228->NodeText;
            var seString = SeString.Parse(nodeText.StringPtr, (int) nodeText.BufUsed - 1);
            if (seString.Payloads.Count < 4)
                return string.Empty;

            var payload = Dalamud.ClientState.ClientLanguage switch
            {
                ClientLanguage.German   => seString.Payloads[3],
                ClientLanguage.French   => seString.Payloads[3],
                ClientLanguage.Japanese => seString.Payloads[2],
                _                       => seString.Payloads[2],
            };

            return (payload as TextPayload)?.Text ?? string.Empty;
        }

        public void CheckPlant(IntPtr talkPtr, IntPtr _)
        {
            var talk = (PtrTalk) talkPtr;
            var text = talk.Text();
            if (text.Contains(StringId.CropDoingWell.Value())
             || text.Contains(StringId.CropBetterDays.Value()))
                LastPlant = ParseText(talk);
        }

        private void SetPatch(PtrSelectString ptr)
        {
            var text = ptr.Description();
            var (bed, patch) = Dalamud.ClientState.ClientLanguage switch
            {
                ClientLanguage.German   => text.Length == 16 ? (text[5] - '1', text[15] - '1') : (9, 9),
                ClientLanguage.French   => text.Length == 24 ? (text[23] - '1', text[8] - '1') : (9, 9),
                ClientLanguage.Japanese => text.Length == 5 ? (text[1] - '1', text[4] - '1') : (9, 9),
                _                       => text.Length == 18 ? (text[0] - '1', text[9] - '1') : (9, 9),
            };

            if (patch < 3 && bed < 8)
            {
                LastPatch = (ushort) patch;
                LastBed   = (ushort) bed;
            }
            else
            {
                LastPatch = ushort.MaxValue;
                LastBed   = ushort.MaxValue;
            }
        }

        private static readonly Regex PlantingTextEn = new(@"Prepare the bed with (?<soil>.*?) and (?<seeds>.*?)\?", RegexOptions.Compiled);
        private static readonly Regex PlantingTextFr = new(@"Planter (?<seeds>.*?) avec (?<soil>.*?).\?", RegexOptions.Compiled);
        private static readonly Regex PlantingTextDe = new(@"(?<soil>.*?) verteilen und (?<seeds>.*?) aussäen\?", RegexOptions.Compiled);
        private static readonly Regex PlantingTextJp = new(@"(?<soil>.*?)に(?<seeds>.*?)を植えます。よろしいですか？", RegexOptions.Compiled);

        private string CleanString(string s)
        {
            switch (Dalamud.ClientState.ClientLanguage)
            {
                case ClientLanguage.French:
                    if (s.StartsWith("un "))
                        return s.Substring(3);
                    if (s.StartsWith("une "))
                        return s.Substring(4);

                    return s;
                case ClientLanguage.German:
                    if (s.StartsWith("einer "))
                        return s.Substring(6);
                    if (s.StartsWith("einem "))
                        return s.Substring(6);

                    return s;
                case ClientLanguage.Japanese: return s;
                default:
                    if (s.StartsWith("a "))
                        return s.Substring(2);
                    if (s.StartsWith("an "))
                        return s.Substring(3);

                    return s;
            }
        }

        private CropData GetCropData(string text)
        {
            var match = Dalamud.ClientState.ClientLanguage switch
            {
                ClientLanguage.German   => PlantingTextDe.Match(text),
                ClientLanguage.French   => PlantingTextFr.Match(text),
                ClientLanguage.Japanese => PlantingTextJp.Match(text),
                _                       => PlantingTextEn.Match(text),
            };
            if (!match.Success)
                return Crops.Crops.Find(0).Data;

            return Crops.Crops.Find(CleanString(match.Groups["seeds"].Value));
        }

        public void SelectYesnoEventDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr data)
        {
            if (eventType == (ushort) EventType.Change && which == PtrSelectYesno.YesButtonId)
            {
                PtrSelectYesno yesno = atkUnit;
                var            node  = (AtkTextNode*) yesno.Pointer->AtkUnitBase.UldManager.NodeList[15];
                if (node->AtkResNode.IsVisible)
                {
                    var seString = SeString.Parse(node->NodeText.StringPtr, (int) node->NodeText.BufUsed - 1);
                    seString.Payloads.RemoveAll(p => p is NewLinePayload);
                    var text = seString.TextValue;
                    if (text.StartsWith(StringId.DisposeCrop.Value()))
                    {
                        var id = IdentifyCropSpot();
                        if (id.Type != CropSpotType.Invalid)
                            if (_timers.Crops.HarvestCrop(id))
                                _timers.SaveCrops();
                    }
                    else
                    {
                        var itemId = GetCropData(text).ItemId;
                        if (itemId != 0)
                        {
                            var id = IdentifyCropSpot();
                            if (id.Type != CropSpotType.Invalid)
                                if (_timers.Crops.PlantCrop(id, itemId, DateTime.UtcNow))
                                    _timers.SaveCrops();
                        }
                    }
                }
            }

            _selectYesnoHook!.Original(atkUnit, eventType, which, source, data);
        }

        private static CropSpotIdentification IdentifyCropSpotPrivate(CropSpotType type)
        {
            var target = Dalamud.Targets.Target;
            if (target == null)
                return CropSpotIdentification.Invalid;

            var ret = new CropSpotIdentification
            {
                Type       = type,
                PlayerName = Dalamud.ClientState.LocalPlayer!.Name.ToString(),
                ServerId   = (ushort) Dalamud.ClientState.LocalPlayer.HomeWorld.Id,
                Position   = target.Position,
            };
            return ret;
        }

        private CropSpotIdentification IdentifyCropSpotHouse()
        {
            var target = Dalamud.Targets.Target;
            if (target == null)
                return CropSpotIdentification.Invalid;

            var ret = new CropSpotIdentification
            {
                Type     = CropSpotType.House,
                ServerId = (ushort) Dalamud.ClientState.LocalPlayer!.CurrentWorld.Id,
                Position = target.Position,
                Zone     = _position.Zone,
                Ward     = _position.Ward,
                Plot     = _position.House,
            };
            if (ret.Zone == HousingZone.Unknown || ret.Ward == 0 || ret.Plot == 0)
                return CropSpotIdentification.Invalid;

            return ret;
        }

        private CropSpotIdentification IdentifyCropSpotOutdoor()
        {
            if (LastBed == ushort.MaxValue)
                return CropSpotIdentification.Invalid;

            var ret = new CropSpotIdentification
            {
                Type     = CropSpotType.Outdoors,
                ServerId = (ushort) Dalamud.ClientState.LocalPlayer!.CurrentWorld.Id,
                Zone     = _position.Zone,
                Ward     = _position.Ward,
                Plot     = _position.Plot,
                Bed      = LastBed,
                Patch    = LastPatch,
            };
            if (ret.Zone == HousingZone.Unknown || ret.Ward == 0 || ret.Plot == 0 || ret.Bed > 7 || ret.Patch > 2)
                return CropSpotIdentification.Invalid;

            return ret;
        }

        private CropSpotIdentification IdentifyCropSpot()
        {
            switch ((HousingZones) Dalamud.ClientState.TerritoryType)
            {
                case HousingZones.Mist:
                case HousingZones.LavenderBeds:
                case HousingZones.Goblet:
                case HousingZones.Shirogane:
                case HousingZones.Firmament:
                    return IdentifyCropSpotOutdoor();
                case HousingZones.ChambersMist:
                case HousingZones.ChambersLavenderBeds:
                case HousingZones.ChambersGoblet:
                case HousingZones.ChambersShirogane:
                case HousingZones.ChambersFirmament:
                    return IdentifyCropSpotPrivate(CropSpotType.Chambers);
                case HousingZones.ApartmentMist:
                case HousingZones.ApartmentLavenderBeds:
                case HousingZones.ApartmentGoblet:
                case HousingZones.ApartmentShirogane:
                case HousingZones.ApartmentFirmament:
                    return IdentifyCropSpotPrivate(CropSpotType.Apartment);
                case HousingZones.CottageMist:
                case HousingZones.CottageLavenderBeds:
                case HousingZones.CottageGoblet:
                case HousingZones.CottageShirogane:
                case HousingZones.CottageFirmament:
                case HousingZones.HouseMist:
                case HousingZones.HouseLavenderBeds:
                case HousingZones.HouseGoblet:
                case HousingZones.HouseShirogane:
                case HousingZones.HouseFirmament:
                case HousingZones.MansionMist:
                case HousingZones.MansionLavenderBeds:
                case HousingZones.MansionGoblet:
                case HousingZones.MansionShirogane:
                case HousingZones.MansionFirmament:
                    return IdentifyCropSpotHouse();
                default:
                    PluginLog.Error($"Housing Zone {Dalamud.ClientState.TerritoryType} should not be able to have crops.");
                    return new CropSpotIdentification();
            }
        }

        private void SelectStringEventDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr data)
        {
            if (eventType == (ushort) EventType.ListIndexChange && data != IntPtr.Zero)
            {
                var idx = ((byte*) data)[0x10];
                if (idx == 1)
                {
                    var selectString = (PtrSelectString) (IntPtr) ((AddonSelectString.PopupMenuDerive*) atkUnit)->Owner;
                    var renderer     = *(AtkComponentListItemRenderer**) data;
                    var text         = renderer->AtkComponentButton.ButtonTextNode->NodeText.ToString();
                    if (text == StringId.TendCrop.Value())
                    {
                        SetPatch(selectString);
                        var id = IdentifyCropSpot();
                        if (id.Type != CropSpotType.Invalid)
                            if (_timers.Crops.TendCrop(id, Crops.Crops.Find(LastPlant).ItemId, DateTime.UtcNow))
                                _timers.SaveCrops();
                    }
                }
                else if (idx == 0)
                {
                    var selectString = (PtrSelectString) (IntPtr) ((AddonSelectString.PopupMenuDerive*) atkUnit)->Owner;
                    var renderer     = *(AtkComponentListItemRenderer**) data;
                    var text         = renderer->AtkComponentButton.ButtonTextNode->NodeText.ToString();
                    if (text == StringId.HarvestCrop.Value())
                    {
                        SetPatch(selectString);
                        var id = IdentifyCropSpot();
                        if (id.Type != CropSpotType.Invalid)
                            if (_timers.Crops.HarvestCrop(id))
                                _timers.SaveCrops();
                    }
                    else if (text == StringId.PlantCrop.Value())
                    {
                        SetPatch(selectString);
                    }
                }
                else if (idx == 2)
                {
                    var selectString = (PtrSelectString) (IntPtr) ((AddonSelectString.PopupMenuDerive*) atkUnit)->Owner;
                    var renderer     = *(AtkComponentListItemRenderer**) data;
                    var text         = renderer->AtkComponentButton.ButtonTextNode->NodeText.ToString();
                    if (text == StringId.RemoveCrop.Value())
                        SetPatch(selectString);
                }
            }

            _selectStringHook!.Original(atkUnit, eventType, which, source, data);
        }
    }
}
