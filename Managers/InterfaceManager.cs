using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    public struct ModuleInfo
    {
        public string              Name;
        public Func<IntPtr, bool>? Predicate;
        public bool                RequiresVisible;
        public bool                Inverted;
    }

    public sealed class InterfaceManager : TimeOutList<IntPtr, ModuleInfo>
    {
        private readonly IntPtr                     _baseUiObject;
        private readonly IntPtr                     _uiProperties;
        private readonly GetUiObjectByNameDelegate? _getUiObjectByNameDelegate;
        private readonly bool                       _canGetUiObject;

        public InterfaceManager()
        {
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

        public Task<IntPtr> Add(string name, bool requiresVisible, int timeOutMs, Func<IntPtr, bool>? predicate = null)
            => Add(new ModuleInfo
            {
                Name            = name,
                RequiresVisible = requiresVisible,
                Inverted        = false,
                Predicate       = predicate,
            }, timeOutMs);

        public Task<IntPtr> AddInverted(string name, bool requiresVisible, int timeOutMs)
            => Add(new ModuleInfo
            {
                Name            = name,
                RequiresVisible = requiresVisible,
                Inverted        = true,
            }, timeOutMs);

        // @formatter:off
        public PtrBank                     Bank()                     => GetUiObject("Bank");
        public PtrCharaSelectListMenu      CharaSelectListMenu()      => GetUiObject("CharaSelectListMenu");
        public PtrContextIconMenu          ContextIconMenu()          => GetUiObject("ContextIconMenu");
        public PtrFocusTarget              FocusTarget()              => GetUiObject("_FocusTargetInfo");
        public PtrGrandCompanySupplyList   GrandCompanySupplyList()   => GetUiObject("GrandCompanySupplyList");
        public PtrGrandCompanySupplyReward GrandCompanySupplyReward() => GetUiObject("GrandCompanySupplyReward");
        public PtrHousingChocoboList       HousingChocoboList()       => GetUiObject("HousingChocoboList");
        public PtrHousingSignBoard         HousingSignBoard()         => GetUiObject("HousingSignBoard");
        public PtrInventoryGrid            InventoryGrid(int idx)     => GetUiObject($"InventoryGrid{idx:D1}E");
        public PtrJournal                  Journal()                  => GetUiObject("JournalDetail");
        public PtrJournalResult            JournalResult()            => GetUiObject("JournalResult");
        public PtrRecipeNote               RecipeNote()               => GetUiObject("RecipeNote");
        public PtrRequest                  Request()                  => GetUiObject("Request");
        public PtrRetainerList             RetainerList()             => GetUiObject("RetainerList");
        public PtrRetainerTaskAsk          RetainerTaskAsk()          => GetUiObject("RetainerTaskAsk");
        public PtrRetainerTaskList         RetainerTaskList()         => GetUiObject("RetainerTaskList");
        public PtrRetainerTaskResult       RetainerTaskResult()       => GetUiObject("RetainerTaskResult");
        public PtrSelectString             SelectIconString()         => GetUiObject("SelectIconString");
        public PtrSelectString             SelectString()             => GetUiObject("SelectString");
        public PtrSelectYesno              SelectYesno()              => GetUiObject("SelectYesno");
        public PtrSynthesis                Synthesis()                => GetUiObject("Synthesis");
        public PtrSelectString             SystemMenu()               => GetUiObject("SystemMenu");
        public PtrTalk                     Talk()                     => GetUiObject("Talk");
        public PtrTextError                TextError()                => GetUiObject("_TextError");
        public PtrTitleMenu                TitleMenu()                => GetUiObject("_TitleMenu");
        // @formatter:on

        protected override unsafe IntPtr OnCheck(ModuleInfo info)
        {
            var modulePtr = GetUiObject(info.Name, 1);
            if (modulePtr == IntPtr.Zero)
                return IntPtr.Zero;

            if (info.Inverted)
            {
                var basePtr = (AtkUnitBase*) modulePtr.ToPointer();
                if (info.RequiresVisible && !basePtr->IsVisible)
                    return IntPtr.Zero;

                return modulePtr;
            }
            else
            {
                var basePtr = (AtkUnitBase*) modulePtr.ToPointer();
                if (basePtr->UldManager.LoadedState == 3 && (!info.RequiresVisible || basePtr->IsVisible) && (info.Predicate?.Invoke(modulePtr) ?? true))
                    return modulePtr;
            }

            return IntPtr.Zero;
        }


        protected override bool RetIsValid(IntPtr ret, ModuleInfo info)
            => info.Inverted == (ret == IntPtr.Zero);

        protected override void OnTimeout(ModuleInfo info, TaskCompletionSource<IntPtr> task)
            => task.SetResult(IntPtr.Zero);

        protected override string ToString(ModuleInfo info)
            => info.Inverted ? $"!{info.Name}" : info.Name;

        protected override string ToString(IntPtr ret)
            => ret.ToInt64().ToString("X16");
    }
}
