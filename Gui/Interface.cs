using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using Dalamud.Interface;
using Dalamud.Plugin;
using GatherBuddy.Gui;
using ImGuiNET;
using Peon.Bothers;
using Peon.Utility;

namespace Peon.Gui
{
    public class Interface : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly PeonConfiguration      _config;

        private          bool   _visible = false;
        private readonly string _header;

        public Interface(DalamudPluginInterface pi, PeonConfiguration config)
        {
            _pluginInterface                          =  pi;
            _config                                   =  config;
            _pluginInterface.UiBuilder.OnBuildUi      += Draw;
            _pluginInterface.UiBuilder.OnOpenConfigUi += SetVisibleConfig;
            _header                                   =  Peon.Version.Length > 0 ? $"Peon v{Peon.Version}###PeonMain" : "Peon###PeonMain";
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
        private string _newSelect = string.Empty;

        private float _rowSize = 0;

        private void Save()
            => _pluginInterface.SavePluginConfig(_config);

        private bool DrawBotherTextInput(string label, string text, out string newText)
        {
            ImGui.SetNextItemWidth(-_rowSize);
            newText = text;
            return ImGui.InputText(label, ref newText, 1024, ImGuiInputTextFlags.EnterReturnsTrue)
             && newText != text
             && newText != string.Empty;
        }

        private bool DrawMatchCombo(string label, MatchType match, out MatchType newMatch)
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

        private bool DrawSourceCombo(string label, TalkSource source, out TalkSource newSource)
            => (newSource = (TalkSource) DrawSourceCombo(label, LongestTalkText, SourceExtensions.TalkStrings, (int) source)) != source;

        private bool DrawSourceCombo(string label, YesNoSource source, out YesNoSource newSource)
            => (newSource = (YesNoSource) DrawSourceCombo(label, LongestYesNoText, SourceExtensions.YesNoStrings, (int) source)) != source;

        private bool DrawSourceCombo(string label, SelectSource source, out SelectSource newSource)
            => (newSource = (SelectSource) DrawSourceCombo(label, LongestSelectText, SourceExtensions.SelectStrings, (int) source)) != source;

        private bool DrawDeleteButton(string label)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var value = ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}{label}");
            ImGui.PopFont();
            return value;
        }

        private bool DrawYesNoCheckbox(string label, bool yesNo, out bool newYesNo)
        {
            newYesNo = yesNo;
            return ImGui.Checkbox(label, ref newYesNo) && newYesNo != yesNo;
        }

        private bool DrawIndexInput(string label, int idx, out int newIdx)
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
            for (var idx = 0; idx < _config.BothersTalk.Count; ++idx)
            {
                var bother = _config.BothersTalk[idx];

                var changes = DrawBotherTextInput($"##talkText{idx}", bother.Text, out var text);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##talkMatch{idx}", bother.MatchType, out var match);
                ImGui.SameLine();
                changes |= DrawSourceCombo($"##talkSource{idx}", bother.Source, out var source);
                ImGui.SameLine();
                if (DrawDeleteButton($"##talkTrash{idx}"))
                {
                    _config.BothersTalk.RemoveAt(idx);
                    --idx;
                    Save();
                }
                else if (changes)
                {
                    _config.BothersTalk[idx] = new TalkBotherSet(text, match, source);
                    Save();
                }
            }

            if (DrawNewTextInput("##talkNew", ref _newTalk))
            {
                _config.BothersTalk.Add(new TalkBotherSet(_newTalk, MatchType.Equal, TalkSource.Disabled));
                Save();
                _newTalk = string.Empty;
            }
        }


        private void DrawYesNoBothers()
        {
            using ImGuiRaii imgui = new();
            if (!imgui.Begin(() => ImGui.BeginTabItem("YesNo Bothers"), ImGui.EndTabItem))
                return;

            _rowSize = (LongestMatchText + LongestYesNoText + 2 * ButtonWidth) * ImGui.GetIO().FontGlobalScale
              + 4 * ImGui.GetStyle().ItemSpacing.X;
            for (var idx = 0; idx < _config.BothersYesNo.Count; ++idx)
            {
                var bother = _config.BothersYesNo[idx];

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
                    _config.BothersYesNo.RemoveAt(idx);
                    --idx;
                    Save();
                }
                else if (changes)
                {
                    _config.BothersYesNo[idx] = new ChoiceBotherSet(text, match, source, yesNo);
                    Save();
                }
            }

            if (DrawNewTextInput("##yesNoNew", ref _newYesNo))
            {
                _config.BothersYesNo.Add(new ChoiceBotherSet(_newYesNo, MatchType.Equal, YesNoSource.Disabled, true));
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
            for (var idx = 0; idx < _config.BothersSelect.Count; ++idx)
            {
                var bother = _config.BothersSelect[idx];

                var changes = DrawBotherTextInput($"##selectSText{idx}", bother.SelectionText, out var selectionText);
                ImGui.SameLine();
                changes |= DrawMatchCombo($"##selectSMatch{idx}", bother.SelectionMatchType, out var selectionMatch);
                ImGui.SameLine();
                changes |= DrawSourceCombo($"##selectSource{idx}", bother.Source, out var source);
                ImGui.SameLine();
                if (DrawDeleteButton($"##selectTrash{idx}"))
                {
                    _config.BothersSelect.RemoveAt(idx);
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
                    _config.BothersSelect[idx] =
                        new SelectBotherSet(selectionText, selectionMatch, index, mainText, mainMatch) { Source = source };
                    Save();
                }
            }

            if (DrawNewTextInput("##selectNew", ref _newSelect))
            {
                _config.BothersSelect.Add(new SelectBotherSet(_newSelect, MatchType.Equal) { Source = SelectSource.Disabled });
                Save();
                _newSelect = string.Empty;
            }
        }

        private void Draw()
        {
            var minSize = new Vector2(640 * ImGui.GetIO().FontGlobalScale, ImGui.GetTextLineHeightWithSpacing() * 5);

            ImGui.SetNextWindowSizeConstraints(minSize, Vector2.One * 10000);
            if (!_visible || !ImGui.Begin(_header, ref _visible))
                return;

            try
            {
                using ImGuiRaii imgui = new();
                if (!imgui.Begin(() => ImGui.BeginTabBar("##Tabs"), ImGui.EndTabBar))
                    return;

                DrawTalkBothers();
                DrawYesNoBothers();
                DrawSelectBothers();
            }
            finally
            {
                ImGui.End();
            }
        }

        private void SetVisibleConfig(object _, object _2)
            => SetVisible();

        public void Dispose()
        {
            _visible                                  =  false;
            _pluginInterface.UiBuilder.OnOpenConfigUi -= SetVisibleConfig;
            _pluginInterface.UiBuilder.OnBuildUi      -= Draw;
        }
    }
}
