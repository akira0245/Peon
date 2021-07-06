using System;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Peon.Bothers;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class LoginManager
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly InterfaceManager       _interfaceManager;
        private readonly InputManager           _inputManager;
        private readonly BotherHelper           _botherHelper;
        private          string                 _lastCharacterName = "";
        private          string[]?              _characters;
        private          bool                   _canceled = false;

        public LoginManager(DalamudPluginInterface pluginInterface, BotherHelper botherHelper, InterfaceManager interfaceManager, InputManager inputManager)
        {
            _pluginInterface  = pluginInterface;
            _inputManager     = inputManager;
            _interfaceManager = interfaceManager;
            _botherHelper     = botherHelper;
        }

        public void NextCharacter(int timeout)
        {
            _canceled          = false;
            _lastCharacterName = _pluginInterface.ClientState.LocalPlayer?.Name ?? "";
            Task.Run(() =>
            {
                var ptr = LogOut(timeout);
                if (ptr == IntPtr.Zero || _canceled)
                    return;
                ptr = ClickStart(ptr);
                if (ptr == IntPtr.Zero || _canceled)
                    return;

                Task.Delay(500).Wait();
                NextCharacter(ptr);
            });
        }

        public void Cancel()
            => _canceled = true;

        public void Reset()
        {
            _characters = null;
        }

        private IntPtr LogOut(int timeout)
        {
            var task = _interfaceManager.Add("_MainCommand", true, timeout);
            task.SafeWait();
            if (task.IsCanceled)
                return IntPtr.Zero;

            PtrMainCommand main = task.Result;
            main.System();

            task = _interfaceManager.Add("AddonContextMenuTitle", true, timeout);
            task.SafeWait();
            if (task.IsCanceled)
                return IntPtr.Zero;
            
            using var nextYesno = _botherHelper.SelectNextYesNo(true);
            PtrContextMenuTitle menu      = task.Result;
            if (!menu.Select(new CompareString("Log Out", MatchType.Equal)))
                return IntPtr.Zero;

            task = _interfaceManager.Add("_TitleMenu", true, timeout);
            task.SafeWait();
            return task.IsCanceled ? IntPtr.Zero : task.Result;
        }
        private IntPtr ClickStart(IntPtr titleMenu)
        {
            PtrTitleMenu menu = titleMenu;
            menu.Start();
            var task = _interfaceManager.Add("_CharaSelectListMenu", true, 60000);
            task.SafeWait();
            return task.IsCanceled ? IntPtr.Zero : task.Result;
        }

        private bool NextCharacter(IntPtr charaSelect)
        {
            PtrCharaSelectListMenu chara = charaSelect;
            _characters ??= chara.CharacterNames();

            if (_characters.Length == 0)
                return false;

            var idx = Array.IndexOf(_characters, _lastCharacterName);
            idx = (idx + 1) % _characters.Length;

            return chara.Select(idx);
        }

        private bool SpecificCharacter(string character, IntPtr charaSelect)
        {
            PtrCharaSelectListMenu chara = charaSelect;
            return chara.Select(character);
        }
    }
}
