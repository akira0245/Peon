using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Plugin;
using FFXIVClientStructs.Component.GUI;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    public class InterfaceManager : IDisposable
    {
        private readonly struct ModuleWait
        {
            public ModuleWait(string m, bool r, int t, ulong a, TaskCompletionSource<IntPtr> x)
            {
                ModuleName      = m;
                RequiresVisible = r;
                TimeOut         = (ulong) t + a;
                Task            = x;
            }

            public readonly string                       ModuleName;
            public readonly ulong                        TimeOut;
            public readonly TaskCompletionSource<IntPtr> Task;
            public readonly bool                         RequiresVisible;
        }

        private          ulong                      _currentTime;
        private readonly DalamudPluginInterface     _pi;
        private readonly LinkedList<ModuleWait>     _waitList = new();
        private readonly IntPtr                     _baseUiObject;
        private readonly IntPtr                     _uiProperties;
        private readonly GetUiObjectByNameDelegate? _getUiObjectByNameDelegate;
        private readonly bool                       _canGetUiObject;

        public InterfaceManager(DalamudPluginInterface pi)
        {
            _pi                        = pi;
            _baseUiObject              = Service<GetBaseUiObject>.Get().Invoke() ?? IntPtr.Zero;
            _uiProperties              = _baseUiObject != IntPtr.Zero ? Marshal.ReadIntPtr(_baseUiObject, 0x20) : IntPtr.Zero;
            _getUiObjectByNameDelegate = Service<GetUiObjectByName>.Get().Delegate();
            _canGetUiObject            = _uiProperties != IntPtr.Zero && _getUiObjectByNameDelegate != null;
        }

        public IntPtr GetUiObject(string name, int index = 1)
        {
            if (_canGetUiObject)
                return _getUiObjectByNameDelegate!(_uiProperties, name, index);

            PluginLog.Error("Can not obtain ui objects.");
            return IntPtr.Zero;
        }

        // @formatter:off
        public PtrBank                     Bank()                     => GetUiObject("Bank");
        public PtrCharaSelectListMenu      CharaSelectListMenu()      => GetUiObject("CharaSelectListMenu");
        public PtrGrandCompanySupplyList   GrandCompanySupplyList()   => GetUiObject("GrandCompanySupplyList");
        public PtrGrandCompanySupplyReward GrandCompanySupplyReward() => GetUiObject("GrandCompanySupplyReward");
        public PtrHousingChocoboList       HousingChocoboList()       => GetUiObject("HousingChocoboList");
        public PtrInventoryGrid            InventoryGrid(int idx)     => GetUiObject("InventoryGrid");
        public PtrRecipeNote               RecipeNote()               => GetUiObject("RecipeNote");
        public PtrRetainerList             RetainerList()             => GetUiObject("RetainerList");
        public PtrRetainerTaskAsk          RetainerTaskAsk()          => GetUiObject("RetainerTaskAsk");
        public PtrRetainerTaskResult       RetainerTaskResult()       => GetUiObject("RetainerTaskResult");
        public PtrSelectString             SelectString()             => GetUiObject("SelectString");
        public PtrSelectYesno              SelectYesno()              => GetUiObject("SelectYesno");
        public PtrSelectString             SystemMenu()               => GetUiObject("SystemMenu");
        public PtrTalk                     Talk()                     => GetUiObject("Talk");
        public PtrTextError                TextError()                => GetUiObject("_TextError");
        public PtrTitleMenu                TitleMenu()                => GetUiObject("_TitleMenu");
        // @formatter:on

        public Task<IntPtr> Add(string moduleName, bool requiresVisible, int timeOutAfter)
        {
            var                          ptr  = CheckModule(moduleName, requiresVisible);
            TaskCompletionSource<IntPtr> task = new();
            if (ptr != IntPtr.Zero)
                task.SetResult(ptr);
            else
                lock (_waitList)
                {
                    if (_waitList.Count == 0)
                        _pi.Framework.OnUpdateEvent += OnFrameworkUpdate;
                    var currentTime = (ulong) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    _waitList.AddLast(new ModuleWait(moduleName, requiresVisible, timeOutAfter, currentTime, task));
                }

            return task.Task;
        }

        private unsafe IntPtr CheckModule(string moduleName, bool requiresVisible)
        {
            var modulePtr = GetUiObject(moduleName, 1);
            if (modulePtr != IntPtr.Zero)
            {
                var basePtr = (AtkUnitBase*) modulePtr.ToPointer();
                if (basePtr->ULDData.LoadedState == 3 && (!requiresVisible || basePtr->IsVisible))
                    return modulePtr;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_waitList.Count > 0)
            {
                _pi.Framework.OnUpdateEvent -= OnFrameworkUpdate;
                foreach (var x in _waitList)
                    x.Task.SetCanceled();
            }

            _waitList.Clear();
        }

        private void RemoveNode(LinkedListNode<ModuleWait> node)
        {
            _waitList.Remove(node);
            if (_waitList.Count != 0)
                return;

            _pi.Framework.OnUpdateEvent -= OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(object framework)
        {
            _currentTime = (ulong) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var node = _waitList.First;
            while (node != null)
            {
                var next   = node.Next;
                var module = node.Value;
                if (module.TimeOut < _currentTime)
                {
                    PluginLog.Verbose("Wait for {ModuleName} timed out.", node.Value.ModuleName);
                    node.Value.Task.SetCanceled();
                    RemoveNode(node);
                }

                var modulePtr = CheckModule(module.ModuleName, module.RequiresVisible);
                if (modulePtr != IntPtr.Zero)
                {
                    PluginLog.Verbose("Wait for {ModuleName} returned {ModulePtr}.", node.Value.ModuleName, modulePtr);
                    node.Value.Task.SetResult(modulePtr);
                    RemoveNode(node);
                }

                node.Value = module;
                node       = next;
            }
        }
    }
}
