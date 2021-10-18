using Dalamud.Interface;
using ImGuiNET;
using Peon.Bothers;
using Peon.Crafting;
using Peon.Managers;
using Peon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Lumina.Excel.GeneratedSheets;
using Peon.SeFunctions;

namespace Peon.Gui
{
    public class Interface : IDisposable
    {
        private readonly Peon _peon;

        private          bool   _visible;
        private readonly string _header;

        private Macro? _currentMacro;
        private string _newMacroName     = string.Empty;
        private string _newCharacterName = string.Empty;

        public Interface(Peon peon)
        {
            _peon                                          =  peon;
            Dalamud.PluginInterface.UiBuilder.Draw         += Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi += SetVisible;
            _header                                        =  Peon.Version.Length > 0 ? $"Peon v{Peon.Version}###PeonMain" : "Peon###PeonMain";
            if (Peon.Config.CraftingMacros.Any())
                _currentMacro = Peon.Config.CraftingMacros.Values.First();
        }

        public void SetVisible()
        {
            _visible = !_visible;
        }

        private static readonly string[] MatchTypes =
        {
            "Equal",
            "Contains",
            "Starts With",
            "Ends With",
            "Equal (no case)",
            "Contains (no case)",
            "Starts With (no case)",
            "Ends With (no case)",
            "Regex Full Match",
            "Regex Partial Match",
        };

        private static readonly float LongestMatchText = MatchTypes.Max(t => ImGui.CalcTextSize(t).X) / ImGui.GetIO().FontGlobalScale + 32;

        private static readonly float LongestTalkText =
            SourceExtensions.TalkStrings.Max(t => ImGui.CalcTextSize(t).X) / ImGui.GetIO().FontGlobalScale + 32;

        private static readonly float LongestYesNoText =
            SourceExtensions.YesNoStrings.Max(t => ImGui.CalcTextSize(t).X) / ImGui.GetIO().FontGlobalScale + 32;

        private static readonly float LongestSelectText =
            SourceExtensions.SelectStrings.Max(t => ImGui.CalcTextSize(t).X) / ImGui.GetIO().FontGlobalScale + 32;


        private const float ButtonWidth = 23;

        private string _newYesNo  = string.Empty;
        private string _newTalk   = string.Empty;
        private string _newQuest  = string.Empty;
        private string _newSelect = string.Empty;

        private float _rowSize;

        public static void Save()
            => Peon.Config.Save();

        private bool DrawBotherTextInput(string label, string text, out string newText)
        {
            ImGui.SetNextItemWidth(-_rowSize);
            newText = text;
            return ImGui.InputText(label, ref newText, 1024, ImGuiInputTextFlags.EnterReturnsTrue)
             && newText != text
             && newText != string.Empty;
        }

        private static bool DrawMatchCombo(string label, MatchType match, out MatchType newMatch)
        {
            ImGui.SetNextItemWidth(LongestMatchText * ImGui.GetIO().FontGlobalScale);
            var matchTypeIdx = (int) match;
            if (ImGui.Combo(label, ref matchTypeIdx, MatchTypes, MatchTypes.Length) && matchTypeIdx != (int) match)
            {
                newMatch = (MatchType) matchTypeIdx;
                return true;
            }

            newMatch = match;
            return false;
        }

        private static int DrawSourceCombo(string label, float size, string[] strings, int idx)
        {
            ImGui.SetNextItemWidth(size * ImGui.GetIO().FontGlobalScale);
            var newIdx = idx;
            ImGui.Combo(label, ref newIdx, strings, strings.Length);
            return newIdx;
        }

        private static bool DrawSourceCombo(string label, TalkSource source, out TalkSource newSource)
            => (newSource = (TalkSource) DrawSourceCombo(label, LongestTalkText, SourceExtensions.TalkStrings, (int) source)) != source;

        private static bool DrawSourceCombo(string label, YesNoSource source, out YesNoSource newSource)
            => (newSource = (YesNoSource) DrawSourceCombo(label, LongestYesNoText, SourceExtensions.YesNoStrings, (int) source)) != source;

        private static bool DrawSourceCombo(string label, SelectSource source, out SelectSource newSource)
            => (newSource = (SelectSource) DrawSourceCombo(label, LongestSelectText, SourceExtensions.SelectStrings, (int) source)) != source;

        private static bool DrawDeleteButton(string label)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var value = ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}{label}");
            ImGui.PopFont();
            return value;
        }

        private static bool DrawYesNoCheckbox(string label, bool yesNo, out bool newYesNo)
        {
            newYesNo = yesNo;
            return ImGui.Checkbox(label, ref newYesNo) && newYesNo != yesNo;
        }

        private static bool DrawIndexInput(string label, int idx, out int newIdx)
        {
            newIdx = idx;
            ImGui.SetNextItemWidth(LongestSelectText);
            return ImGui.InputInt(label, ref newIdx, 0) && newIdx != idx;
        }

        private bool DrawNewTextInput(string label, ref string text)
        {
            ImGui.SetNextItemWidth(-_rowSize);
            return ImGui.InputTextWithHint(label, "Add new bother...", ref text, 1024, ImGuiInputTextFlags.EnterReturnsTrue)
             && text != string.Empty;
        }


        private void DrawTalkBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Talk Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestTalkText + ButtonWidth) * ImGui.GetIO().FontGlobalScale + 3 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < Peon.Config.BothersTalk.Count; ++idx)
            {
                var bother = Peon.Config.BothersTalk[idx];

                var changes = DrawBotherTextInput($"##talkText{idx}", bother.Text, out var text);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##talkMatch{idx}", bother.MatchType, out var match);
                ImGui.SameLine();
                changes |= DrawSourceCombo($"##talkSource{idx}", bother.Source, out var source);
                ImGui.SameLine();
                if (DrawDeleteButton($"##talkTrash{idx}"))
                {
                    Peon.Config.BothersTalk.RemoveAt(idx);
                    --idx;
                    Save();
                }
                else if (changes)
                {
                    Peon.Config.BothersTalk[idx] = new TalkBotherSet(text, match, source);
                    Save();
                }
            }

            if (DrawNewTextInput("##talkNew", ref _newTalk))
            {
                Peon.Config.BothersTalk.Add(new TalkBotherSet(_newTalk, MatchType.Equal, TalkSource.Disabled));
                Save();
                _newTalk = string.Empty;
            }
        }

        private void DrawQuestBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Quest Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestTalkText + ButtonWidth) * ImGui.GetIO().FontGlobalScale + 3 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < Peon.Config.BothersQuest.Count; ++idx)
            {
                var bother = Peon.Config.BothersQuest[idx];

                var changes = DrawBotherTextInput($"##questText{idx}", bother.Text, out var text);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##questMatch{idx}", bother.MatchType, out var match);
                ImGui.SameLine();
                changes |= DrawYesNoCheckbox($"##questEnabled{idx}", bother.Enabled, out var enabled);
                ImGui.SameLine();
                if (DrawDeleteButton($"##questTrash{idx}"))
                {
                    Peon.Config.BothersQuest.RemoveAt(idx);
                    --idx;
                    Save();
                }
                else if (changes)
                {
                    Peon.Config.BothersQuest[idx] = new QuestBotherSet(text, match, enabled);
                    Save();
                }
            }

            if (DrawNewTextInput("##questNew", ref _newQuest))
            {
                Peon.Config.BothersQuest.Add(new QuestBotherSet(_newQuest, MatchType.Equal, false));
                Save();
                _newQuest = string.Empty;
            }
        }


        private void DrawYesNoBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("YesNo Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestYesNoText + 2 * ButtonWidth) * ImGui.GetIO().FontGlobalScale
              + 4 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < Peon.Config.BothersYesNo.Count; ++idx)
            {
                var bother = Peon.Config.BothersYesNo[idx];

                var changes = DrawBotherTextInput($"##yesNoText{idx}", bother.Text, out var text);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##yesNoMatch{idx}", bother.MatchType, out var match);
                ImGui.SameLine();
                changes |= DrawSourceCombo($"##yesNoSource{idx}", bother.Source, out var source);
                ImGui.SameLine();
                changes |= DrawYesNoCheckbox($"##yesNoChoice{idx}", bother.Choice, out var yesNo);
                ImGui.SameLine();
                if (DrawDeleteButton($"##yesNoTrash{idx}"))
                {
                    Peon.Config.BothersYesNo.RemoveAt(idx);
                    --idx;
                    Save();
                }
                else if (changes)
                {
                    Peon.Config.BothersYesNo[idx] = new ChoiceBotherSet(text, match, source, yesNo);
                    Save();
                }
            }

            if (DrawNewTextInput("##yesNoNew", ref _newYesNo))
            {
                Peon.Config.BothersYesNo.Add(new ChoiceBotherSet(_newYesNo, MatchType.Equal, YesNoSource.Disabled, true));
                Save();
                _newYesNo = string.Empty;
            }
        }

        private void DrawSelectBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Select Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestSelectText + ButtonWidth) * ImGui.GetIO().FontGlobalScale
              + 3 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < Peon.Config.BothersSelect.Count; ++idx)
            {
                var bother = Peon.Config.BothersSelect[idx];

                var changes = DrawBotherTextInput($"##selectSText{idx}", bother.SelectionText, out var selectionText);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##selectSMatch{idx}", bother.SelectionMatchType, out var selectionMatch);
                ImGui.SameLine();
                changes |= DrawSourceCombo($"##selectSource{idx}", bother.Source, out var source);
                ImGui.SameLine();
                if (DrawDeleteButton($"##selectTrash{idx}"))
                {
                    Peon.Config.BothersSelect.RemoveAt(idx);
                    --idx;
                    Save();
                    continue;
                }

                ImGui.Indent();
                changes |= DrawBotherTextInput($"##selectMText{idx}", bother.MainText, out var mainText);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##selectMMatch{idx}", bother.MainMatchType, out var mainMatch);
                ImGui.SameLine();
                changes |= DrawIndexInput($"##selectIndex{idx}", bother.Index, out var index);
                ImGui.Unindent();

                if (changes)
                {
                    Peon.Config.BothersSelect[idx] =
                        new SelectBotherSet(selectionText, selectionMatch, index, mainText, mainMatch) { Source = source };
                    Save();
                }
            }

            if (DrawNewTextInput("##selectNew", ref _newSelect))
            {
                Peon.Config.BothersSelect.Add(new SelectBotherSet(_newSelect, MatchType.Equal) { Source = SelectSource.Disabled });
                Save();
                _newSelect = string.Empty;
            }
        }

        private bool DrawAddButton(string label)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var value = ImGui.Button($"{FontAwesomeIcon.Plus.ToIconChar()}{label}");
            ImGui.PopFont();
            return value;
        }

        private void DrawAlternatingBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Alternating Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestSelectText + ButtonWidth) * ImGui.GetIO().FontGlobalScale
              + 3 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < Peon.Config.BothersAlternatingSelect.Count; ++idx)
            {
                var botherGroup = Peon.Config.BothersAlternatingSelect[idx];
                if (!ImGui.BeginChild($"##AselectGroup{idx}",
                    Vector2.UnitY
                  * (2 * (ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().ItemSpacing.Y)
                      + ImGui.GetFrameHeightWithSpacing() * 2 * botherGroup.Bothers.Length), true))
                {
                    ImGui.EndChild();
                    continue;
                }

                for (var jdx = 0; jdx < botherGroup.Bothers.Length; ++jdx)
                {
                    var bother  = botherGroup.Bothers[jdx];
                    var changes = DrawBotherTextInput($"##AselectSText{idx}_{jdx}", bother.SelectionText, out var selectionText);
                    ImGui.SameLine();
                    changes |= DrawMatchCombo($"##AselectSMatch{idx}_{jdx}", bother.SelectionMatchType, out var selectionMatch);
                    ImGui.SameLine();
                    changes |= DrawSourceCombo($"##AselectSource{idx}_{jdx}", bother.Source, out var source);
                    ImGui.SameLine();
                    if (DrawDeleteButton($"##AselectTrash{idx}_{jdx}"))
                    {
                        botherGroup.Bothers = botherGroup.Bothers.Where((_, i) => i != jdx).ToArray();
                        --jdx;
                        if (!botherGroup.Bothers.Any())
                        {
                            Peon.Config.BothersAlternatingSelect.RemoveAt(idx);
                            --idx;
                            Save();
                            break;
                        }

                        Save();
                        continue;
                    }

                    ImGui.Indent();
                    changes |= DrawBotherTextInput($"##AselectMText{idx}_{jdx}", bother.MainText, out var mainText);
                    ImGui.SameLine();
                    changes |= DrawMatchCombo($"##AselectMMatch{idx}_{jdx}", bother.MainMatchType, out var mainMatch);
                    ImGui.SameLine();
                    changes |= DrawIndexInput($"##AselectIndex{idx}_{jdx}", bother.Index, out var index);
                    ImGui.SameLine();
                    if (DrawAddButton($"##AselectAdd{idx}_{jdx}"))
                    {
                        botherGroup.Bothers = botherGroup.Bothers.Take(jdx + 1)
                           .Append(new SelectBotherSet(string.Empty, MatchType.Equal))
                           .Concat(botherGroup.Bothers.Skip(jdx + 1)).ToArray();
                        changes = true;
                    }

                    ImGui.Unindent();

                    if (changes)
                    {
                        botherGroup.Bothers[jdx] =
                            new SelectBotherSet(selectionText, selectionMatch, index, mainText, mainMatch) { Source = source };
                        Save();
                    }
                }

                ImGui.EndChild();
            }

            if (DrawNewTextInput("##AselectNew", ref _newSelect))
            {
                Peon.Config.BothersAlternatingSelect.Add(
                    new AlternatingBotherSet(new SelectBotherSet(_newSelect, MatchType.Equal) { Source = SelectSource.Disabled }));
                Save();
                _newSelect = string.Empty;
            }
        }

        private void DrawMacros()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Crafting Macros"), ImGui.EndTabItem))
                return;

            if (ImGui.BeginCombo("Current Macro", _currentMacro?.Name ?? ""))
            {
                foreach (var macro in Peon.Config.CraftingMacros)
                    if (ImGui.Selectable(macro.Value.Name, macro.Value == _currentMacro))
                        _currentMacro = macro.Value;
                ImGui.EndCombo();
            }

            ImGui.InputTextWithHint("##NewMacro", "Enter new macro name...", ref _newMacroName, 64);
            ImGui.SameLine();
            if (ImGui.Button("New Macro"))
                if (_newMacroName != string.Empty && !Peon.Config.CraftingMacros.ContainsKey(_newMacroName))
                {
                    Peon.Config.CraftingMacros.Add(_newMacroName, new Macro(_newMacroName));
                    Save();
                    _newMacroName = string.Empty;
                }

            if (_currentMacro == null)
                return;

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            var deleteVal = ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}##deleteMacro");
            ImGui.PopFont();
            if (deleteVal)
            {
                Peon.Config.CraftingMacros.Remove(_currentMacro!.Name!);
                Save();
                _currentMacro = null;
                return;
            }

            for (var i = 0; i < _currentMacro.Count; ++i)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{i + 1,3}. ");
                ImGui.SameLine();
                var currentName = _currentMacro.Actions[i].Use().Name;
                if (ImGui.BeginCombo($"##action{i}", currentName, ImGuiComboFlags.NoArrowButton))
                {
                    foreach (var action in ActionIdExtensions.Actions.Values.Where(a => a.Id != ActionId.None))
                        if (ImGui.Selectable($"{action.Name}##{i}", currentName == action.Name) && _currentMacro.Actions[i] != action.Id)
                        {
                            _currentMacro.Actions[i] = action.Id;
                            Save();
                        }

                    ImGui.EndCombo();
                }
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{_currentMacro.Count + 1,3}. ");
            ImGui.SameLine();
            if (ImGui.BeginCombo($"##action{_currentMacro.Count}", "New Action", ImGuiComboFlags.NoArrowButton))
            {
                foreach (var action in ActionIdExtensions.Actions.Values.Where(a => a.Id != ActionId.None))
                    if (ImGui.Selectable($"{action.Name}##{_currentMacro.Count}", false))
                    {
                        _currentMacro.Actions.Add(action.Id);
                        Save();
                    }

                ImGui.EndCombo();
            }
        }

        private void DrawLoginButtonConfig()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Login Buttons"), ImGui.EndTabItem))
                return;

            for (var i = 0; i < Peon.Config.CharacterNames.Count; ++i)
            {
                var name = Peon.Config.CharacterNames[i];
                if (ImGui.InputText($"##Character{i}", ref name, 32) && name != Peon.Config.CharacterNames[i])
                {
                    if (!name.Any())
                        Peon.Config.CharacterNames.RemoveAt(i);
                    else
                        Peon.Config.CharacterNames[i] = name;
                    Save();
                }
            }

            if (ImGui.InputTextWithHint($"##Character{Peon.Config.CharacterNames.Count}", "New Character...", ref _newCharacterName, 32,
                    ImGuiInputTextFlags.EnterReturnsTrue)
             && _newCharacterName.Any())
            {
                Peon.Config.CharacterNames.Add(_newCharacterName);
                Save();
                _newCharacterName = string.Empty;
            }
        }

        private HousingZone _newHousingZone = HousingZone.Mist;
        private ushort      _newWard        = 1;
        private ushort      _newPlot        = 1;
        private ushort      _newServerId    = 0;
        private string      _newHouseName   = string.Empty;

        private void DrawDeleteButton(int idx)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}##Delete{idx}"))
            {
                Peon.Config.HousingNames.RemoveAt(idx);
                Peon.Config.Save();
            }
            ImGui.PopFont();
        }

        private HousingZone DrawHousingZoneCombo(int idx, HousingZone zone)
        {
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo($"##houseNameZone{idx}", zone == HousingZone.Unknown ? "Zone" : zone.ToString()))
            {
                if (ImGui.Selectable($"{HousingZone.Mist.ToName()}##{idx}", zone == HousingZone.Mist))
                    return HousingZone.Mist;
                if (ImGui.Selectable($"{HousingZone.LavenderBeds.ToName()}##{idx}", zone == HousingZone.LavenderBeds))
                    return HousingZone.LavenderBeds;
                if (ImGui.Selectable($"{HousingZone.Goblet.ToName()}##{idx}", zone == HousingZone.Goblet))
                    return HousingZone.Goblet;
                if (ImGui.Selectable($"{HousingZone.Shirogane.ToName()}##{idx}", zone == HousingZone.Shirogane))
                    return HousingZone.Shirogane;
                if (ImGui.Selectable($"{HousingZone.Firmament.ToName()}##{idx}", zone == HousingZone.Firmament))
                    return HousingZone.Firmament;

                ImGui.EndCombo();
            }

            return zone;
        }

        private ushort DrawServerInput(int idx, ushort serverId)
        {
            ImGui.SetNextItemWidth(150);
            var sheet = Dalamud.GameData.GetExcelSheet<World>()!;
            if (serverId == 0 && Dalamud.ClientState.LocalPlayer != null)
                serverId = (ushort) Dalamud.ClientState.LocalPlayer.CurrentWorld.Id;
            var currentName = serverId == 0 ? "World" : sheet.GetRow(serverId)!.Name.RawString;
            if (ImGui.BeginCombo($"##houseNameServer{idx}", currentName))
            {
                foreach (var world in sheet.Where(w => w.IsPublic))
                {
                    var name = world.Name.RawString;
                    if (ImGui.Selectable(name, currentName == name))
                        serverId = (ushort) world.RowId;
                }

                ImGui.EndCombo();
            }

            return serverId;
        }

        private ushort DrawHousingWardInput(int idx, ushort ward)
        {
            int tmp = ward;
            ImGui.SetNextItemWidth(30);
            if (ImGui.InputInt($"##Ward{idx}", ref tmp, 0) && tmp > 0 && tmp < 25)
                return (ushort) tmp;

            return ward;
        }

        private ushort DrawHousingPlotInput(int idx, ushort plot)
        {
            int tmp = plot;
            ImGui.SetNextItemWidth(30);
            if (ImGui.InputInt($"##Plot{idx}", ref tmp, 0) && tmp > 0 && tmp < 61)
                return (ushort) tmp;

            return plot;
        }

        private bool DrawHousingNameInput(int idx, string oldName, out string name)
        {
            name = oldName;
            ImGui.SetNextItemWidth(-1);
            return ImGui.InputText($"##houseName{idx}", ref name, 32, ImGuiInputTextFlags.EnterReturnsTrue);
        }

        private void DrawHouseNameConfig()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("House Names"), ImGui.EndTabItem))
                return;

            for (var i = 0; i < Peon.Config.HousingNames.Count; ++i)
            {
                DrawDeleteButton(i);
                if (i >= Peon.Config.HousingNames.Count)
                    break;
                ImGui.SameLine();
                var house = Peon.Config.HousingNames[i];
                var zone  = DrawHousingZoneCombo(i, (HousingZone) ((house.Item1 >> 32) & 0xFFFF));
                ImGui.SameLine();
                var server = DrawServerInput(i, (ushort) ((house.Item1 >> 48) & 0xFFFF));
                ImGui.SameLine();
                var ward = DrawHousingWardInput(i, (ushort) ((house.Item1 >> 16) & 0xFFFF));
                ImGui.SameLine();
                var plot = DrawHousingPlotInput(i, (ushort) (house.Item1 & 0xFFFF));
                ImGui.SameLine();
                var nameChange = DrawHousingNameInput(i, house.Item2, out var name);
                var key        = plot | ((ulong) ward << 16) | ((ulong) zone << 32) | ((ulong) server << 48);
                if (key != house.Item1)
                {
                    Peon.Config.HousingNames.RemoveAt(i--);
                    Peon.Config.HousingNames.Add((key, house.Item2));
                    Peon.Config.Save();
                }
                else if (nameChange && name != house.Item2)
                {
                    Peon.Config.HousingNames.Add((key, name));
                    Peon.Config.Save();
                }
            }

            _newHousingZone = DrawHousingZoneCombo(Peon.Config.HousingNames.Count, _newHousingZone);
            ImGui.SameLine();
            _newServerId = DrawServerInput(Peon.Config.HousingNames.Count, _newServerId);
            ImGui.SameLine();
            _newWard = DrawHousingWardInput(Peon.Config.HousingNames.Count, _newWard);
            ImGui.SameLine();
            _newPlot = DrawHousingPlotInput(Peon.Config.HousingNames.Count, _newPlot);
            ImGui.SameLine();
            if (DrawHousingNameInput(Peon.Config.HousingNames.Count, _newHouseName, out _newHouseName) && _newHouseName.Any())
            {
                var key = _newPlot | ((ulong)_newWard << 16) | ((ulong)_newHousingZone << 32) | ((ulong)_newServerId << 48);
                Peon.Config.HousingNames.Add((key, _newHouseName));
                Peon.Config.Save();
                _newHouseName = string.Empty;
            }
        }

        private void DrawPeonConfig()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Config"), ImGui.EndTabItem))
                return;

            var enableBothers = Peon.Config.EnableNoBother;
            if (ImGui.Checkbox("Enable Bothers", ref enableBothers) && enableBothers != Peon.Config.EnableNoBother)
            {
                Peon.Config.EnableNoBother = enableBothers;
                Save();
            }

            var enableLoginButtons = Peon.Config.EnableLoginButtons;
            if (ImGui.Checkbox("Enable Login Buttons", ref enableLoginButtons) && enableLoginButtons != Peon.Config.EnableLoginButtons)
            {
                Peon.Config.EnableLoginButtons = enableLoginButtons;
                Save();
            }

            var enableTimers = Peon.Config.EnableTimers;
            if (ImGui.Checkbox("Enable Timers", ref enableTimers) && enableTimers != Peon.Config.EnableTimers)
            {
                Peon.Config.EnableTimers = enableTimers;
                if (enableTimers)
                    _peon.TimerManager.SetMessage();
                else
                    _peon.TimerManager.RemoveMessage();
                Save();
            }
        }


        private void DrawDebug()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("Debug"), ImGui.EndTabItem))
                return;

            if (ImGui.CollapsingHeader("Interface Waitlist"))
            {
                if (_peon.InterfaceManager.GetType().BaseType?
                   .GetField("_waitList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)?
                   .GetValue(_peon.InterfaceManager) is LinkedList<TimeOutList<IntPtr, ModuleInfo>.WaitBlock> waitList)
                {
                    var i = 0;
                    foreach (var waitBlock in waitList)
                    {
                        ImGui.Selectable(waitBlock.Infos.Inverted ? $"!{waitBlock.Infos.Name}##{i}" : $"{waitBlock.Infos.Name}##{i}");
                        ImGui.SameLine();
                        ImGui.Selectable($"{waitBlock.TimeOut}##{i++}");
                    }
                }
                else
                {
                    ImGui.Text("Could not get Interface Waitlist");
                }
            }

            if (ImGui.CollapsingHeader("Temp Bothers"))
            {
                if (_peon.OhBother._skipNextTalk)
                    ImGui.Text("Skip next Talk");

                if (_peon.OhBother._selectNextYesNo.HasValue)
                    ImGui.Text($"Select next {(_peon.OhBother._selectNextYesNo.Value ? "Yes" : "No")}");

                if (_peon.OhBother._completeNextQuest)
                    ImGui.Text("Complete next Quest");

                if (_peon.OhBother._selectNextString != null)
                    ImGui.Text("Trying to select next string.");
            }

            if (ImGui.CollapsingHeader("Plant Timers"))
            {
                ImGui.Text($"Last Plant: {_peon.TimerManager.LastPlant}");
                ImGui.Text($"Last Patch: {_peon.TimerManager.LastPatch}");
                ImGui.Text($"Last Bed: {_peon.TimerManager.LastBed}");
            }

            if (ImGui.CollapsingHeader("Strings"))
                if (ImGui.BeginTable("##stringDebugTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
                {
                    foreach (StringId id in Enum.GetValues(typeof(StringId)))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(id.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(_peon.Localization[id]);
                    }

                    ImGui.EndTable();
                }
        }

        private void Draw()
        {
            if (!_visible)
                return;

            var minSize = new Vector2(640 * ImGui.GetIO().FontGlobalScale, ImGui.GetTextLineHeightWithSpacing() * 5);

            ImGui.SetNextWindowSizeConstraints(minSize, Vector2.One * 10000);
            if (!ImGui.Begin(_header, ref _visible))
            {
                ImGui.End();
                return;
            }

            try
            {
                using ImGuiRaii imgui = new();
                if (!imgui.Begin(() => ImGui.BeginTabBar("##Tabs"), ImGui.EndTabBar))
                    return;

                DrawPeonConfig();
                DrawTalkBothers();
                DrawQuestBothers();
                DrawYesNoBothers();
                DrawSelectBothers();
                DrawAlternatingBothers();
                DrawMacros();
                DrawLoginButtonConfig();
                DrawHouseNameConfig();
                DrawDebug();
            }
            finally
            {
                ImGui.End();
            }
        }

        public void Dispose()
        {
            _visible                                       =  false;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= SetVisible;
            Dalamud.PluginInterface.UiBuilder.Draw         -= Draw;
        }
    }
}
