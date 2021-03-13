using System;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;

namespace Peon.Managers
{
    public class TargetManager
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly InputManager           _inputManager;

        public TargetManager(DalamudPluginInterface pluginInterface, InputManager inputManager)
        {
            _pluginInterface = pluginInterface;
            _inputManager    = inputManager;
        }

        public bool Target(Predicate<Actor> predicate)
        {
            foreach (var actor in _pluginInterface.ClientState.Actors)
            {
                if (!predicate(actor))
                    continue;

                PluginLog.Verbose("Target set to actor {ActorId}: \"{ActorName}\".", actor.ActorId, actor.Name);
                _pluginInterface.ClientState.Targets.SetCurrentTarget(actor);
                return true;
            }

            return false;
        }

        public bool Target(string targetName)
            => Target(actor => actor.Name == targetName);

        public void Interact()
            => _inputManager.SendKeyPress(InputManager.Num0);

        public bool Interact(Predicate<Actor> predicate)
        {
            if (!Target(predicate))
                return false;

            Interact();
            return true;

        }

        public bool Interact(string targetName)
            => Interact(actor => actor.Name == targetName);
    }
}
