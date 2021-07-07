using System;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Peon.Managers;
using Peon.Modules;

namespace Peon.Crafting
{
    public class Crafter
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly InterfaceManager       _interface;
        private readonly CommandManager         _commands;
        private readonly PeonConfiguration      _config;

        public bool Verbose { get; set; }

        private bool _running         = false;
        private bool _basicTouchCombo = false;
        private int  _currentStep     = 0;

        public Crafter(DalamudPluginInterface pi, PeonConfiguration config, CommandManager commandManager, InterfaceManager interfaceManager,
            bool verbose)
        {
            _pluginInterface = pi;
            _config          = config;
            _commands        = commandManager;
            _interface       = interfaceManager;
            Verbose          = verbose;
        }

        private void Log(string s)
        {
            PluginLog.Verbose(s);
            if (Verbose)
                _pluginInterface.Framework.Gui.Chat.Print(s);
        }

        private ActionInfo Use(ActionId id)
        {
            var action = id.Use(_interface.Synthesis().Status, _basicTouchCombo);
            _basicTouchCombo = action.Id == ActionId.BasicTouch;
            return action;
        }

        private bool Error(string error)
        {
            PluginLog.Error(error);
            _pluginInterface.Framework.Gui.Chat.PrintError(error);
            return false;
        }

        private ActionInfo Step(Macro macro)
        {
            _currentStep = _interface.Synthesis().Step;
            if (_currentStep >= macro.Count)
            {
                Error($"Reached step {_currentStep} but macro only has {macro.Count}.");
                return ActionId.None.Use();
            }

            return macro.Step(_currentStep);
        }

        public void Cancel()
        {
            _running         = false;
            _basicTouchCombo = false;
            _currentStep     = 0;
        }

        public void ExecuteStep(Macro macro)
        {
            var action = Step(macro);
            if (_running)
            {
                _commands.Execute(action.Cast());
                Log($"{macro.Name} Step {_currentStep}: {action.Name}");
            }
        }

        public async Task<bool> CompleteCraft(Macro macro)
        {
            if (_running)
            {
                Error("Already crafting.");
                return false;
            }

            _running = true;
            var task = _interface.Add("Synthesis", true, 5000);
            task.Wait();
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
            {
                Error("Timeout while waiting for synthesis to begin.");
                _running = false;
                return false;
            }

            _currentStep = ((PtrSynthesis) task.Result).Step;
            var highestStep = 1;
            if (_currentStep == 0)
            {
                Error("Not crafting anything.");
                _running = false;
                return false;
            }

            var tries = 0;
            while (_running && _currentStep < macro.Count)
            {
                _currentStep = _interface.Synthesis().Step;
                if (_currentStep < highestStep)
                {
                    Log($"Terminated craft early at step {_currentStep}/{macro.Count} because steps reset.");
                    break;
                }

                if (_currentStep >= highestStep)
                {
                    if (highestStep == _currentStep)
                        ++tries;
                    else
                        tries = 0;

                    if (tries > 3)
                    {
                        Error("Terminated craft because action could not be used after delays.");
                        break;
                    }

                    highestStep = _currentStep;
                    var action = macro.Step(_currentStep);
                    _commands.Execute(action.Cast());
                    Log($"{macro.Name} Step {_currentStep}: {action.Name}");
                    await Task.Delay(action.Delay);
                }
            }

            Cancel();
            return highestStep > 1;
        }

        public bool RestartCraft()
        {
            if (_running || _interface.Synthesis())
                return true;

            _running = true;
            var task = _interface.Add("RecipeNote", true, 5000);
            task.Wait();
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Error("Restarting craft timed out, no notebook open.");

            PtrRecipeNote note = task.Result;
            if (!_running)
                return true;

            note.Synthesize();

            task = _interface.Add("Synthesis", true, 5000);
            task.Wait();
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Error("Restarting craft timed out, synthesis did not reopen.");

            _running = false;
            return true;
        }
    }
}
