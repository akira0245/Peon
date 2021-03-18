using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors.Types;
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
        private readonly DalamudPluginInterface                _pluginInterface;
        private readonly InputManager                          _inputManager;
        private readonly AddonWatcher                          _addons;
        private          TaskCompletionSource<TargetingState>? _state;

        public TargetManager(DalamudPluginInterface pluginInterface, InputManager inputManager, AddonWatcher addons)
        {
            _pluginInterface = pluginInterface;
            _inputManager    = inputManager;
            _addons          = addons;
        }

        public TargetingState Target(Predicate<Actor> predicate)
        {
            foreach (var actor in _pluginInterface.ClientState.Actors)
            {
                if (!predicate(actor))
                    continue;

                PluginLog.Verbose("Target set to actor {ActorId}: \"{ActorName}\".", actor.ActorId, actor.Name);
                _pluginInterface.ClientState.Targets.SetCurrentTarget(actor);
                return TargetingState.Success;
            }

            return TargetingState.ActorNotFound;
        }

        public TargetingState Target(string targetName)
            => Target(actor => actor.Name == targetName);

        private void CheckForRangeError(IntPtr modulePtr, IntPtr _)
        {
            PtrTextError ptr = modulePtr;
            if (ptr.Text() == "Too far away.")
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

        public Task<TargetingState> Interact(int timeOut, Predicate<Actor> predicate)
        {
            if (Target(predicate) != TargetingState.Success)
                return new Task<TargetingState>(() => TargetingState.ActorNotFound);

            return Interact(timeOut);
        }

        public Task<TargetingState> Interact(string targetName, int timeOut)
            => Interact(timeOut, actor => actor.Name == targetName);
    }
}
