using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
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
        private          TaskCompletionSource<TargetingState>? _state;

        public TargetManager(InputManager inputManager, AddonWatcher addons)
        {
            _inputManager    = inputManager;
            _addons          = addons;
        }

        public TargetingState Target(Predicate<GameObject> predicate)
        {
            foreach (var actor in Dalamud.Objects)
            {
                if (!predicate(actor))
                    continue;

                PluginLog.Verbose("Target set to actor {ActorId}: \"{ActorName}\".", actor.ObjectId, actor.Name);
                Dalamud.Targets.SetTarget(actor);
                return TargetingState.Success;
            }

            return TargetingState.ActorNotFound;
        }

        public TargetingState Target(string targetName)
            => Target(actor => actor.Name.ToString() == targetName);

        private void CheckForRangeError(IntPtr modulePtr, IntPtr _)
        {
            PtrTextError ptr  = modulePtr;
            var          text = ptr.Text();
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
                return _state.Task;

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
    }
}
