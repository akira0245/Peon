using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
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
        JournalResultSetup,

        TalkUpdate,

        TextErrorChange,
    }

    public struct AddonEventInfo
    {
        public OnAddonEventDelegate OnAddonEvent;
        public OnAddonEventTimeOut? OnAddonEventTimeOut;
        public AddonEvent           Event;
    }

    public class AddonWatcher : TimeOutList<object?, AddonEventInfo>
    {
        private readonly Hook<OnAddonSetupDelegate>?  _onSelectYesnoSetupHook;
        private readonly Hook<OnAddonSetupDelegate>?  _onSelectStringSetupHook;
        private readonly Hook<OnAddonSetupDelegate>?  _onJournalResultSetupHook;
        private readonly Hook<OnAddonUpdateDelegate>? _onTalkUpdateHook;
        private readonly Hook<OnAddonChangeDelegate>? _onTextErrorChangeHook;

        public event OnAddonEventDelegate? OnSelectYesnoSetup;
        public event OnAddonEventDelegate? OnSelectStringSetup;
        public event OnAddonEventDelegate? OnJournalResultSetup;
        public event OnAddonEventDelegate? OnTalkUpdate;
        public event OnAddonEventDelegate? OnTextErrorChange;

        public AddonWatcher()
        {
            _onSelectYesnoSetupHook   = Service<YesNoOnSetup>.Get().CreateHook(OnSelectYesNoSetupDetour);
            _onSelectStringSetupHook  = Service<SelectStringOnSetup>.Get().CreateHook(OnSelectStringSetupDetour);
            _onJournalResultSetupHook = Service<JournalResultOnSetup>.Get().CreateHook(OnJournalResultSetupDetour);
            _onTalkUpdateHook         = Service<TalkOnUpdate>.Get().CreateHook(OnTalkUpdateDetour);
            _onTextErrorChangeHook    = Service<TextErrorOnChange>.Get().CreateHook(OnTextErrorChangeDetour);
        }

        public ref OnAddonEventDelegate? this[AddonEvent addonEvent]
        {
            get
            {
                switch (addonEvent)
                {
                    case AddonEvent.SelectYesNoSetup:   return ref OnSelectYesnoSetup;
                    case AddonEvent.SelectStringSetup:  return ref OnSelectStringSetup;
                    case AddonEvent.JournalResultSetup: return ref OnJournalResultSetup;
                    case AddonEvent.TalkUpdate:         return ref OnTalkUpdate;
                    case AddonEvent.TextErrorChange:    return ref OnTextErrorChange;
                    default:                            throw new InvalidEnumArgumentException();
                }
            }
        }

        public void Add(AddonEvent addonEvent, OnAddonEventDelegate eventHandler, int timeOutFrames,
            OnAddonEventTimeOut? timeOutHandler)
        {
            Add(new AddonEventInfo
            {
                Event               = addonEvent,
                OnAddonEvent        = eventHandler,
                OnAddonEventTimeOut = timeOutHandler,
            }, timeOutFrames);
            this[addonEvent] += eventHandler;
        }

        public void AddOneTime(AddonEvent addonEvent, OnAddonEventDelegate eventHandler, int timeOutFrames,
            OnAddonEventTimeOut? timeOutHandler)
        {
            void Indirection(IntPtr pluginPtr, IntPtr dataPtr)
            {
                eventHandler(pluginPtr, dataPtr);
                RemoveNode(block => block.OnAddonEvent == Indirection);
                this[addonEvent] -= Indirection;
            }

            var info = new AddonEventInfo
            {
                Event               = addonEvent,
                OnAddonEvent        = Indirection,
                OnAddonEventTimeOut = timeOutHandler,
            };

            Add(info, timeOutFrames);
            this[addonEvent] += Indirection;
        }

        private void OnSelectYesNoSetupDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onSelectYesnoSetupHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("[AddonWatcher] SelectYesNo addon setup at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(),
                dataPtr.ToInt64());
            OnSelectYesnoSetup?.Invoke(ptr, dataPtr);
        }

        private void OnSelectStringSetupDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onSelectStringSetupHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("[AddonWatcher] SelectString addon setup at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(),
                dataPtr.ToInt64());
            OnSelectStringSetup?.Invoke(ptr, dataPtr);
        }

        private void OnJournalResultSetupDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onJournalResultSetupHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("[AddonWatcher] JournalResult addon setup at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(),
                dataPtr.ToInt64());
            OnJournalResultSetup?.Invoke(ptr, dataPtr);
        }

        private void OnTalkUpdateDetour(IntPtr ptr, IntPtr dataPtr)
        {
            _onTalkUpdateHook!.Original(ptr, dataPtr);
            PluginLog.Verbose("[AddonWatcher] Talk addon updated at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(), dataPtr.ToInt64());
            OnTalkUpdate?.Invoke(ptr, dataPtr);
        }

        private void OnTextErrorChangeDetour(IntPtr ptr, int b, IntPtr dataPtr)
        {
            _onTextErrorChangeHook!.Original(ptr, b, dataPtr);
            PluginLog.Verbose("[AddonWatcher] Error text event triggered at 0x{Address:X16} with data 0x{Data:X16}.", ptr.ToInt64(),
                dataPtr.ToInt64());
            OnTextErrorChange?.Invoke(ptr, dataPtr);
        }

        public override void Dispose()
        {
            _onSelectYesnoSetupHook?.Dispose();
            _onSelectStringSetupHook?.Dispose();
            _onJournalResultSetupHook?.Dispose();
            _onTalkUpdateHook?.Dispose();
            _onTextErrorChangeHook?.Dispose();
            base.Dispose();
        }

        protected override string ToString(AddonEventInfo info)
            => info.Event.ToString();

        protected override string ToString(object? ret)
            => "void";

        protected override bool RetIsValid(object? ret, AddonEventInfo info)
            => false;

        protected override object? OnCheck(AddonEventInfo info)
            => null;

        protected override void OnTimeout(AddonEventInfo info, TaskCompletionSource<object?> task)
        {
            this[info.Event] -= info.OnAddonEvent;
            info.OnAddonEventTimeOut?.Invoke();
        }
    }
}
