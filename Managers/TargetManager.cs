using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public enum TargetingState
    {
        Success,
        ActorNotFound,
        ActorNotInRange,
        TimeOut,
        Unknown,
    }

    public class TargetManager
    {
        private readonly InputManager                          _inputManager;
        private readonly AddonWatcher                          _addons;
        private readonly InterfaceManager                      _interface;
        private          TaskCompletionSource<TargetingState>? _state;

        public TargetManager(InputManager inputManager, AddonWatcher addons, InterfaceManager iface)
        {
            _inputManager = inputManager;
            _addons       = addons;
            _interface    = iface;

        }

        private static float Distance(GameObject? o1, GameObject? o2)
            => o1 == null || o2 == null ? float.MaxValue : Vector3.Distance(o1.Position, o2.Position);

        public TargetingState GetTargetObject(Predicate<GameObject> predicate, out GameObject? actor)
        {
            actor = null;
            var player          = Dalamud.ClientState.LocalPlayer;
            if (player == null)
                return TargetingState.ActorNotFound;

            var currentDistance = Distance(player, null);
            actor = null;
            foreach (var obj in Dalamud.Objects)
            {
                if (!predicate(obj))
                    continue;

                var dist = Distance(player, obj);
                if (dist < currentDistance)
                {
                    currentDistance = dist;
                    actor           = obj;
                }
            }
            return currentDistance == float.MaxValue ? TargetingState.ActorNotFound : TargetingState.Success;
        }

        public TargetingState Target(Predicate<GameObject> predicate)
        {
            var ret = GetTargetObject(predicate, out var currentActor);
            if (ret == TargetingState.Success)
            {
                PluginLog.Verbose("Target set to actor {ActorId}: {ActorName}.", currentActor!.ObjectId, currentActor.Name);
                Dalamud.Targets.SetTarget(currentActor);
            }

            return ret;
        }

        public TargetingState Target(string targetName)
            => Target(actor => actor.Name.ToString() == targetName);

        private void CheckForRangeError(IntPtr modulePtr, IntPtr _)
        {
            PtrTextError ptr  = modulePtr;
            var          text = ptr.Text();
            PluginLog.Verbose("Error Text: {ErrorText}", text);
            if (StringId.TargetTooFarAway.Equal(text)
             || StringId.CannotSeeTarget.Equal(text)
             || StringId.TargetTooFarBelow.Equal(text)
             || StringId.TargetTooFarAbove.Equal(text)
             || StringId.TargetInvalidLocation.Equal(text))
            {
                _state?.SetResult(TargetingState.ActorNotInRange);
                _state = null;
            }
        }

        private void RangeErrorTime()
        {
            _state?.SetResult(TargetingState.Success);
            _state = null;
        }

        public Task<TargetingState> EnableRangeChecking(int timeOutFrames)
        {
            if (_state != null && !_state.Task.IsCompleted)
                _state.SetCanceled();

            _state = new TaskCompletionSource<TargetingState>();
            if (timeOutFrames <= 0)
            {
                _state.SetResult(TargetingState.Unknown);
                return _state.Task;
            }

            _addons.AddOneTime(AddonEvent.TextErrorChange, CheckForRangeError, timeOutFrames, RangeErrorTime);
            return _state.Task;
        }

        public Task<TargetingState> Interact(int timeOut)
        {
            var task = EnableRangeChecking(timeOut);
            _inputManager.SendKeyPress(InputManager.Num0);
            return task;
        }

        public Task<TargetingState> Interact(int timeOut, Predicate<GameObject> predicate)
        {
            if (Target(predicate) != TargetingState.Success)
                return Task.Run(() => TargetingState.ActorNotFound);

            return Interact(timeOut);
        }

        public Task<TargetingState> Interact(string targetName, int timeOut)
            => Interact(timeOut, actor => actor.Name.ToString() == targetName);

        public TargetingState InteractWithoutKey(string targetName)
        {
            var focus = _interface.FocusTarget();
            if (!focus)
                return TargetingState.Unknown;
            if (GetTargetObject(actor => actor.Name.ToString() == targetName, out var target) != TargetingState.Success)
                return TargetingState.ActorNotFound;

            var oldFocus = Dalamud.Targets.FocusTarget;
            PluginLog.Verbose("Interacting with {TargetName} ({Address}).", targetName, target!.Address);
            Dalamud.Targets.SetFocusTarget(target);
            focus.Interact();
            Dalamud.Targets.SetFocusTarget(oldFocus);
            return TargetingState.Success;
        }
    }
}
