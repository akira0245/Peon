using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Bothers;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerTaskList
    {
        private const int ExplorationListIdx = 2;
        private const int NormalListIdx      = 3;
        private const int ExplorationTextIdx = 5;
        private const int NormalTextIdx      = 6;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerTaskList(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerTaskList ptr)
            => ptr.Pointer != null;

        public bool IsExploration
            => Pointer->UldManager.NodeList[ExplorationListIdx]->IsVisible;

        public AtkComponentNode* ExplorationList
            => (AtkComponentNode*) Pointer->UldManager.NodeList[ExplorationListIdx];

        public AtkComponentNode* NormalList
            => (AtkComponentNode*) Pointer->UldManager.NodeList[NormalListIdx];

        public AtkComponentNode* List
            => IsExploration ? ExplorationList : NormalList;

        public bool Select(int idx)
            => Module.ClickList(Pointer, List, idx);

        private static bool ListCallback(CompareString s, AtkComponentListItemRenderer* renderer, int idx)
        {
            var uld  = ((AtkComponentBase*) renderer)->UldManager;
            var name = Module.TextNodeToString((AtkTextNode*) uld.NodeList[idx]);
            PluginLog.Log(name);
            return s.Matches(name);
        }

        public bool Select(CompareString s)
            => IsExploration
                ? Module.ClickList(Pointer, ExplorationList, l => ListCallback(s, l, ExplorationTextIdx))
                : Module.ClickList(Pointer, NormalList,      l => ListCallback(s, l, NormalTextIdx));

        public int Count
            => ((AtkComponentList*) List->Component)->ListLength;
    }
}
