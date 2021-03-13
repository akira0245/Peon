using System;
using System.ComponentModel;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    public delegate void OnAddonEventDelegate(IntPtr pluginPtr, IntPtr dataPtr);
    public delegate void OnAddonEventTimeOut();

    public enum AddonEvent : byte
    {
        SelectYesNoSetup,
        SelectStringSetup,
        RetainerListSetup,

        TalkUpdate,

        TextErrorChange,
    }

    public class AddonWatcher : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly TimeOutList            _timeOuts;

        private readonly Hook<OnAddonSetupDelegate>?  _onSelectYesnoSetupHook;
        private readonly Hook<OnAddonSetupDelegate>?  _onSelectStringSetupHook;
        private readonly Hook<OnAddonUpdateDelegate>? _onTalkUpdateHook;
        private readonly Hook<OnAddonChangeDelegate>? _onTextErrorChangeHook;

        public event OnAddonEventDelegate? OnSelectYesnoSetup;
        public event OnAddonEventDelegate? OnSelectStringSetup;
        public event OnAddonEventDelegate? OnTalkUpdate;
        public event OnAddonEventDelegate? OnTextErrorChange;

        public ref OnAddonEventDelegate? this[AddonEvent addonEvent]
        {
            get
            {
                switch (addonEvent)
                {
                    case AddonEvent.SelectYesNoSetup:  return ref OnSelectYesnoSetup;
                    case AddonEvent.SelectStringSetup: return ref OnSelectStringSetup;
                    case AddonEvent.TalkUpdate:        return ref OnTalkUpdate;
                    case AddonEvent.TextErrorChange:   return ref OnTextErrorChange;
                    default:                           throw new InvalidEnumArgumentException();
                }
            }
        }

        public void AddTimedOneTimeDelegate(AddonEvent addonEvent, OnAddonEventDelegate eventHandler, int timeOutFrames, OnAddonEventTimeOut? timeOutHandler)
        {
            var node = _timeOuts.AddEvent(addonEvent, eventHandler, timeOutFrames, timeOutHandler);

            void NewHandler(IntPtr pluginPtr, IntPtr dataPtr)
            {
                eventHandler(pluginPtr, dataPtr);
                _timeOuts.RemoveEvent(node);
                this[addonEvent] -= NewHandler;
            }

            this[addonEvent] += NewHandler;
        }

        public AddonWatcher(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;

            _onSelectYesnoSetupHook  = Service<YesNoOnSetup>.Get().CreateHook(OnSelectYesNoSetupDetour, this);
            _onSelectStringSetupHook = Service<SelectStringOnSetup>.Get().CreateHook(OnSelectStringSetupDetour, this);
            _onTalkUpdateHook        = Service<TalkOnUpdate>.Get().CreateHook(OnTalkUpdateDetour, this);
            _onTextErrorChangeHook   = Service<TextErrorOnChange>.Get().CreateHook(OnTextErrorChangeDetour, this);

            _timeOuts = new TimeOutList(_pluginInterface, this);
        }

        private void OnSelectYesNoSetupDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onSelectYesnoSetupHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("SelectYesNo addon setup at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(), dataPtr.ToInt64());
            OnSelectYesnoSetup?.Invoke(ptr, dataPtr);
        }

        private void OnSelectStringSetupDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onSelectStringSetupHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("SelectString addon setup at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(), dataPtr.ToInt64());
            OnSelectStringSetup?.Invoke(ptr, dataPtr);
        }

        private void OnTalkUpdateDetour(IntPtr ptr, IntPtr dataPtr)
        {
            _onTalkUpdateHook!.Original(ptr, dataPtr);
            PluginLog.Verbose("Talk addon updated at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(), dataPtr.ToInt64());
            OnTalkUpdate?.Invoke(ptr, dataPtr);
        }

        private void OnTextErrorChangeDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onTextErrorChangeHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("Error text event triggered at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(), dataPtr.ToInt64());
            OnTextErrorChange?.Invoke(ptr, dataPtr);
        }

        public void Dispose()
        {
            _timeOuts.Dispose();
            _onSelectYesnoSetupHook?.Dispose();
            _onSelectStringSetupHook?.Dispose();
            _onTalkUpdateHook?.Dispose();
            _onTextErrorChangeHook?.Dispose();
        }
    }
}
