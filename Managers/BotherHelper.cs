using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using Peon.Bothers;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class BotherHelper : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly AddonWatcher           _addons;
        private readonly PeonConfiguration      _config;


        private readonly List<ChoiceBotherSet> _bothersYesNo;

        private readonly List<TalkBotherSet>   _bothersTalk;
        private readonly List<SelectBotherSet> _bothersSelect;

        internal         bool            _skipNextTalk        = false;
        internal         bool?           _selectNextYesNo     = null;

        public BotherSetter SkipNextTalk()
            => new(true, _selectNextYesNo, this);

        public BotherSetter SelectNextYesNo(bool which)
            => new(_skipNextTalk, which, this);

        public readonly struct BotherSetter : IDisposable
        {
            private readonly BotherHelper _bother;
            private readonly bool         _skipNextTalkOld;
            private readonly bool?        _selectNextYesNoOld;

            internal BotherSetter(bool skipNextTalk, bool? selectNextYesNo, BotherHelper bother)
            {
                _bother                  = bother;
                _skipNextTalkOld         = bother._skipNextTalk;
                _selectNextYesNoOld      = bother._selectNextYesNo;
                _bother._skipNextTalk    = skipNextTalk;
                _bother._selectNextYesNo = selectNextYesNo;
            }

            public void Dispose()
            {
                _bother._skipNextTalk    = _skipNextTalkOld;
                _bother._selectNextYesNo = _selectNextYesNoOld;
            }
        }

        public BotherHelper(DalamudPluginInterface pluginInterface, AddonWatcher addons, PeonConfiguration configuration)
        {
            _pluginInterface = pluginInterface;
            _addons          = addons;
            _config          = configuration;
            _bothersYesNo    = configuration.BothersYesNo;
            _bothersTalk     = configuration.BothersTalk;
            _bothersSelect   = configuration.BothersSelect;

            _addons.OnSelectStringSetup += OnSelectStringSetup;
            _addons.OnSelectYesnoSetup  += OnYesNoSetup;
            _addons.OnTalkUpdate        += OnTalkUpdate;
        }

        public void Dispose()
        {
            _addons.OnSelectStringSetup -= OnSelectStringSetup;
            _addons.OnSelectYesnoSetup  -= OnYesNoSetup;
            _addons.OnTalkUpdate        -= OnTalkUpdate;
        }


        private void OnSelectStringSetup(IntPtr ptr, IntPtr _)
        {
            if (!_config.EnableNoBother)
                return;

            PtrSelectString selectStringPtr = ptr;
            var             mainText        = selectStringPtr.Description();
            string[]?       texts           = null;
            foreach (var bother in _bothersSelect.Where(b => b.Source != SelectSource.Disabled))
                switch (bother.Source)
                {
                    case SelectSource.SelectionText:
                    {
                        texts ??= selectStringPtr.ItemTexts();
                        var idx = Array.FindIndex(texts, bother.SelectionMatches);
                        if (idx >= 0)
                        {
                            selectStringPtr.Select(idx);
                            {
                                PluginLog.Verbose(
                                    "Clicked SelectString window at {Index} due to match on {StringType}, {MatchType}: \"{Text}\".", idx,
                                    bother.Source, bother.SelectionMatchType, bother.SelectionText);
                                return;
                            }
                        }

                        break;
                    }
                    case SelectSource.CheckMainAndSelectionText:
                    {
                        if (bother.MainMatches(mainText))
                        {
                            texts ??= selectStringPtr.ItemTexts();
                            var idx = Array.FindIndex(texts, bother.SelectionMatches);
                            if (selectStringPtr.Select(idx))
                            {
                                PluginLog.Verbose(
                                    "Clicked SelectString window at {Index} due to match on {StringType}, {MainMatchType}: \"{MainText}\" and {SelectionMatchType}: \"{SelectionText}\".",
                                    idx, bother.Source, bother.MainMatchType, bother.MainText, bother.SelectionMatchType, bother.SelectionText);
                                return;
                            }
                        }

                        break;
                    }
                    case SelectSource.CheckMainAndSelectionIndex:
                    {
                        if (bother.MainMatches(mainText))
                        {
                            var count = selectStringPtr.Count;
                            var idx   = bother.Index < 0 ? count + bother.Index : bother.Index;
                            if (selectStringPtr.Select(idx))
                            {
                                PluginLog.Verbose(
                                    "Clicked SelectString window at {Index} due to match on {StringType}, {MainMatchType}: \"{MainText}\" and Index {ReqIndex}.",
                                    idx, bother.Source, bother.MainMatchType, bother.MainText, bother.Index);
                                return;
                            }
                        }

                        break;
                    }
                    case SelectSource.CheckIndexAndText:
                    {
                        texts ??= selectStringPtr.ItemTexts();
                        var idx = bother.Index < 0 ? texts.Length + bother.Index : bother.Index;
                        if (idx > 0 && idx < texts.Length && bother.SelectionMatches(texts[idx]) && selectStringPtr.Select(idx))
                        {
                            PluginLog.Verbose(
                                "Clicked SelectString window at {Index} due to match on {StringType}, {SelectionMatchType}: \"{SelectionText}\" and Index {ReqIndex}.",
                                idx, bother.Source, bother.SelectionMatchType, bother.SelectionText, bother.Index);
                            return;
                        }

                        break;
                    }
                    case SelectSource.CheckAll:
                        if (bother.MainMatches(mainText))
                        {
                            texts ??= selectStringPtr.ItemTexts();
                            var idx = bother.Index < 0 ? texts.Length + bother.Index : bother.Index;
                            if (idx > 0 && idx < texts.Length && bother.SelectionMatches(texts[idx]) && selectStringPtr.Select(idx))
                            {
                                PluginLog.Verbose(
                                    "Clicked SelectString window at {Index} due to match on {StringType}, {MainMatchType}: \"{MainText}\", {SelectionMatchType}: \"{SelectionText}\" and Index {ReqIndex}.",
                                    idx, bother.Source, bother.MainMatchType, bother.MainText, bother.SelectionMatchType, bother.SelectionText,
                                    bother.Index);
                                return;
                            }
                        }

                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
        }

        private void OnTalkUpdate(IntPtr ptr, IntPtr _)
        {
            if (!_skipNextTalk && !_config.EnableNoBother)
                return;

            PtrTalk talkPtr = ptr;
            var     speaker = talkPtr.Speaker();
            var     text    = talkPtr.Text();

            foreach (var set in _bothersTalk.Where(b => b.Source != TalkSource.Disabled))
            {
                var click = _skipNextTalk
                 || set.Source switch
                    {
                        TalkSource.Text    => set.Matches(text),
                        TalkSource.Speaker => set.Matches(speaker),
                        _                  => throw new InvalidEnumArgumentException(),
                    };

                void TmpDelegate(Framework _)
                {
                    if (talkPtr.IsVisible)
                        talkPtr.Click();
                    else
                        _pluginInterface.Framework.OnUpdateEvent -= TmpDelegate;
                }

                if (!click)
                    continue;

                _skipNextTalk                            =  false;
                _pluginInterface.Framework.OnUpdateEvent += TmpDelegate;
                PluginLog.Verbose("Clicked Talk window due to match on {StringType}, {MatchType}: \"{Text}\".", set.Source, set.MatchType,
                    set.Text);
                break;
            }
        }

        private unsafe void OnYesNoSetup(IntPtr ptr, IntPtr updateData)
        {
            PtrSelectYesno selectPtr = ptr;

            if (_selectNextYesNo != null)
            {
                selectPtr.Click(_selectNextYesNo.Value);
                return;
            }
            _selectNextYesNo = null;

            if (!_config.EnableNoBother)
                return;

            var stringPtr = *(void**) ((byte*) updateData.ToPointer() + 0x8);
            var text      = Marshal.PtrToStringAnsi(new IntPtr(stringPtr)) ?? string.Empty;

            var yesText = selectPtr.YesText;
            var noText  = selectPtr.NoText;

            foreach (var set in _bothersYesNo.Where(b => b.Source != YesNoSource.Disabled))
            {
                bool? click = set.Source switch
                {
                    YesNoSource.Text      => set.Matches(text) ? set.Choice : null,
                    YesNoSource.YesButton => set.Matches(yesText) ? set.Choice : null,
                    YesNoSource.NoButton  => set.Matches(yesText) ? set.Choice : null,
                    _                     => throw new InvalidEnumArgumentException(),
                };

                if (click == null)
                    continue;

                selectPtr.Click(click.Value);
                PluginLog.Verbose(
                    "Clicked \"{ButtonText}\" in Yesno window due to match on {StringType}, {MatchType}: \"{Text}\".",
                    click.Value ? yesText : noText, set.Source, set.MatchType, set.Text);
            }
        }
    }
}
