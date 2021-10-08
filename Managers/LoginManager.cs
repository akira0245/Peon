using System;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Peon.Bothers;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class LoginManager
    {
        private readonly InterfaceManager       _interfaceManager;
        private readonly BotherHelper           _botherHelper;
        private          string                 _lastCharacterName = string.Empty;
        private          string[]?              _characters;
        private          bool                   _running;

        public LoginManager(BotherHelper botherHelper, InterfaceManager interfaceManager)
        {
            _interfaceManager = interfaceManager;
            _botherHelper     = botherHelper;
        }

        private void CharacterTask(int timeout, Action<IntPtr> action)
        {
            if (_running)
            {
                Dalamud.Chat.PrintError($"Character Task already running.");
                return;
            }

            _running = true;
            Task.Run(() =>
            {
                var ptr = LogOut(timeout);
                if (ptr == IntPtr.Zero || !_running)
                {
                    _running = false;
                    return;
                }

                ptr = ClickStart(ptr);
                if (ptr == IntPtr.Zero || !_running)
                {
                    _running = false;
                    return;
                }

                Task.Delay(500).Wait();
                action(ptr);
                _running = false;
            });
        }

        public void NextCharacter(int timeout)
        {
            _lastCharacterName = Dalamud.ClientState.LocalPlayer?.Name.ToString() ?? string.Empty;
            CharacterTask(timeout, ptr => NextCharacterIntern(ptr, false));
        }

        public void PreviousCharacter(int timeout)
        {
            _lastCharacterName = Dalamud.ClientState.LocalPlayer?.Name.ToString() ?? string.Empty;
            CharacterTask(timeout, ptr => NextCharacterIntern(ptr, true));
        }

        public void LogTo(string character, int timeout)
            => CharacterTask(timeout, ptr => SpecificCharacter(character, ptr));

        public void LogTo(int idx, int timeout)
            => CharacterTask(timeout, ptr => SpecificCharacter(idx, ptr));

        public void Cancel()
            => _running = false;

        public void Reset()
        {
            _running    = false;
            _characters = null;
        }

        private IntPtr LogOut(int timeout)
        {
            using var nextYesno = _botherHelper.SelectNextYesNo(true);
            if (Dalamud.Conditions.Any())
            {
                var task = _interfaceManager.Add("_MainCommand", true, timeout);
                task.SafeWait();
                if (task.IsCanceled || task.Result == IntPtr.Zero)
                    return IntPtr.Zero;
                PluginLog.Verbose("Reached Main Command");

                PtrMainCommand main = task.Result;
                main.System();

                task = _interfaceManager.Add("AddonContextMenuTitle", true, timeout);
                task.SafeWait();
                if (task.IsCanceled || task.Result == IntPtr.Zero)
                    return IntPtr.Zero;
                PluginLog.Verbose("Reached ContextMenuTitle");

                PtrContextMenuTitle menu = task.Result;
                if (!menu.Select(StringId.LogOut.Cs()))
                    return IntPtr.Zero;
            }

            var titleTask = _interfaceManager.Add("_TitleMenu", true, timeout);
            titleTask.SafeWait();
            return titleTask.IsCanceled ? IntPtr.Zero : titleTask.Result;
        }

        private IntPtr ClickStart(IntPtr titleMenu)
        {
            PtrTitleMenu menu = titleMenu;
            menu.Start();
            var task = _interfaceManager.Add("_CharaSelectListMenu", true, 60000);
            task.SafeWait();
            return task.IsCanceled ? IntPtr.Zero : task.Result;
        }

        private bool NextCharacterIntern(IntPtr charaSelect, bool previous)
        {
            PtrCharaSelectListMenu chara = charaSelect;
            _characters ??= chara.CharacterNames();

            if (_characters.Length == 0)
                return false;

            var idx = Array.IndexOf(_characters, _lastCharacterName);
            idx = (idx + (previous ? -1 : 1)) % _characters.Length;

            return chara.Select(idx);
        }

        private bool SpecificCharacter(string character, IntPtr charaSelect)
        {
            PtrCharaSelectListMenu chara = charaSelect;
            return chara.Select(new CompareString(character, MatchType.CiContains));
        }

        private bool SpecificCharacter(int idx, IntPtr charaSelect)
        {
            PtrCharaSelectListMenu chara = charaSelect;
            return chara.Select(idx);
        }
    }
}
