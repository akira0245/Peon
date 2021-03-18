using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Peon.Managers;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon
{
    public class PeonWorker
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly InterfaceManager       _interfaceManager;

        public PeonWorker(DalamudPluginInterface pluginInterface, InterfaceManager interfaceManager)
        {
            _pluginInterface  = pluginInterface;
            _interfaceManager = interfaceManager;
        }

        public bool TargetByName()
        {
            foreach (var actor in _pluginInterface.ClientState.Actors)
            { }

            return false;
        }
    }

    public class Peon : IDalamudPlugin
    {
        public string Name
            => "Peon";

        private DalamudPluginInterface? _pluginInterface;
        private InterfaceManager?       _interfaceManager;
        private PeonConfiguration?      _configuration;
        private AddonWatcher?           _addons;
        private BotherHelper?           _ohBother;
        private InputManager?           _inputManager;
        private TargetManager?          _targeting;
        private RetainerManager?        _retainers;
        private LoginManager?           _login;
        private ChocoboManager?         _chocobos;


        public void SetupServices(DalamudPluginInterface pluginInterface)
        {
            Service<DalamudPluginInterface>.Set(pluginInterface);

            // Addresses.
            Service<GetBaseUiObject>.Set(pluginInterface.TargetModuleScanner);
            Service<GetUiObjectByName>.Set(pluginInterface.TargetModuleScanner);
            Service<InputKey>.Set(pluginInterface.TargetModuleScanner);
            Service<SelectStringOnSetup>.Set(pluginInterface.TargetModuleScanner);
            Service<TalkOnUpdate>.Set(pluginInterface.TargetModuleScanner);
            Service<TextErrorOnChange>.Set(pluginInterface.TargetModuleScanner);
            Service<YesNoOnSetup>.Set(pluginInterface.TargetModuleScanner);
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface  = pluginInterface;
            _configuration    = _pluginInterface.GetPluginConfig() as PeonConfiguration ?? new PeonConfiguration();
            SetupServices(_pluginInterface);

            _interfaceManager = new InterfaceManager(pluginInterface);
            _addons           = new AddonWatcher(pluginInterface);
            _inputManager     = new InputManager();
            _targeting        = new TargetManager(pluginInterface, _inputManager, _addons);
            _ohBother         = new BotherHelper(_pluginInterface, _addons, _configuration);
            _retainers        = new RetainerManager(_pluginInterface, _targeting, _addons!, _ohBother, _interfaceManager!);
            _login            = new LoginManager(_pluginInterface, _ohBother, _interfaceManager!, _inputManager!);
            _chocobos         = new ChocoboManager(_pluginInterface, _targeting, _addons, _ohBother, _interfaceManager);

            _pluginInterface.SavePluginConfig(_configuration);

            _pluginInterface.CommandManager.AddHandler("/peon", new CommandInfo(OnRetainer)
            {
                HelpMessage = "Send Retainers, either 'all' or a comma-separated list of indices.",
                ShowInHelp  = true,
            });
        }

        public void Dispose()
        {
            _ohBother?.Dispose();
            _addons?.Dispose();
            _interfaceManager?.Dispose();
            _pluginInterface!.CommandManager.RemoveHandler("/peon");
            _pluginInterface!.Dispose();
        }

        private void DoRetainer(int idx)
        {
            ;
        }

        private unsafe void OnRetainer(string command, string arguments)
        {
            var argumentParts = arguments.Split(',');
            if (argumentParts.Length < 1)
                return;
            switch (argumentParts[0])
            {
                case "cancel":
                    _retainers!.Cancel();
                    break;
                case "turnin":
                    Task.Run(() =>
                    {
                        var ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("GrandCompanySupplyList", 1);
                        if (ptr != IntPtr.Zero)
                        {
                            PtrGrandCompanySupplyList s = ptr;
                            s.Select(0);
                            var task = _interfaceManager.Add("GrandCompanySupplyReward", true, 3000);
                            task.SafeWait();
                            if (!task.IsCanceled)
                                ((PtrGrandCompanySupplyReward) task.Result).Deliver();
                        }
                    });

                    break;
                case "allturnin":
                    Task.Run(() =>
                    {
                        var ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("GrandCompanySupplyList", 1);
                        if (ptr != IntPtr.Zero)
                        {
                            PtrGrandCompanySupplyList s = ptr;
                            while(s.Count > 0)
                            {
                                s.Select(0);
                                var task = _interfaceManager.Add("GrandCompanySupplyReward", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                ((PtrGrandCompanySupplyReward) task.Result).Deliver();
                                task = _interfaceManager.Add("GrandCompanySupplyList", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                s = task.Result;
                            }
                        }
                    });
                    break;
                case "logout":
                    _login!.OpenMenu(3000);
                    break;
                case "allretainer":
                    _retainers!.DoAllRetainers();
                    break;
                case "retainer":
                    if (argumentParts.Length < 2)
                        return;
                    _retainers!.DoFullRetainer(int.Parse(argumentParts[1]));
                    break;
                case "retainertest":
                    _interfaceManager.RetainerList().SelectFirstComplete();
                    break;
                case "interact":
                    if (argumentParts.Length < 2)
                        return;
                    _targeting!.Interact(argumentParts[1], 300);
                    break;
                case "timeout":
                    _addons?.AddOneTime(AddonEvent.SelectYesNoSetup, 
                        (ptrx, data) => PluginLog.Information($"{ptrx}"), 300, 
                        () => PluginLog.Information("timeout"));
                    break;
                case "yes":
                    var ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("SelectYesno", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrSelectYesno s = ptr;
                        s.ClickYes();
                    }

                    break;
                case "no":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("SelectYesno", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrSelectYesno s = ptr;
                        s.ClickNo();
                    }

                    break;
                case "synthesize":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RecipeNote", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRecipeNote s = ptr;
                        s.Synthesize();
                    }

                    break;
                case "list1":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("SelectString", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrSelectString s = ptr;
                        s.Select(0);
                    }

                    break;
                case "list2":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("SelectString", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrSelectString s = ptr;
                        s.Select(1);
                    }

                    break;
                case "talk":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("Talk", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrTalk s = ptr;
                        if (s.Pointer->AtkUnitBase.IsVisible)
                            s.Click();
                    }

                    break;
                case "retainer1":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerList", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerList s = ptr;
                        s.Select(0);
                    }

                    break;
                case "retainer2":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerList", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerList s = ptr;
                        s.Select(1);
                    }

                    break;
                case "confirm":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerTaskResult", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerTaskResult s = ptr;
                        s.Confirm();
                    }

                    break;
                case "reassign":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerTaskResult", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerTaskResult s = ptr;
                        s.Reassign();
                    }

                    break;
                case "return":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerTaskAsk", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerTaskAsk s = ptr;
                        s.Return();
                    }

                    break;
                case "assign":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RetainerTaskAsk", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRetainerTaskAsk s = ptr;
                        s.Assign();
                    }

                    break;
                case "quit":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("SystemMenu", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrSelectString s = ptr;
                        s.Select(10);
                    }

                    break;
                case "start":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("_TitleMenu", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrTitleMenu s = ptr;
                        s.Start();
                    }

                    break;
                case "select":
                    ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("_CharaSelectListMenu", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrCharaSelectListMenu s = ptr;
                        s.Select(4);
                    }

                    break;
                case "chocobo":
                    _chocobos.FeedAllChocobos();
                    break;
                case "bank":
                    var bank = _interfaceManager.Bank();
                    if (bank)
                    {
                        bank.Minus();
                    }

                    break;
                case "feed":
                    foreach (var name in new[]
                    {
                        "InventoryGrid0E",
                        "InventoryGrid1E",
                        "InventoryGrid2E",
                        "InventoryGrid3E",
                    })
                    {
                        ptr = _pluginInterface.Framework.Gui.GetUiObjectByName(name, 1);
                        if (ptr == IntPtr.Zero)
                            continue;

                        PtrInventoryGrid s = ptr;
                        if (s.FeedChocobo())
                            return;
                    }

                    break;
            }
        }
    }
}
