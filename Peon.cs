using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Peon.Crafting;
using Peon.Gui;
using Peon.Managers;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;
using CommandManager = Peon.Managers.CommandManager;

namespace Peon
{
    public class Peon : IDalamudPlugin
    {
        public static string Version = "";

        public string Name
            => "Peon";

        private DalamudPluginInterface _pluginInterface  = null!;
        private Interface              _interface        = null!;
        private InterfaceManager       _interfaceManager = null!;
        private PeonConfiguration      _configuration    = null!;
        private AddonWatcher           _addons           = null!;
        private BotherHelper           _ohBother         = null!;
        private InputManager           _inputManager     = null!;
        private TargetManager          _targeting        = null!;
        private RetainerManager        _retainers        = null!;
        private LoginManager           _login            = null!;
        private ChocoboManager         _chocobos         = null!;
        private CommandManager         _commands         = null!;
        private Crafter                _crafter          = null!;
        private LoginBar               _loginBar         = null!;

        public static long BaseAddress;


        private void Print(string s)
            => _pluginInterface.Framework.Gui.Chat.Print(s);

        private void Error(string s)
            => _pluginInterface.Framework.Gui.Chat.PrintError(s);

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
            try
            {
                Version          = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                _pluginInterface = pluginInterface;
                _configuration   = _pluginInterface.GetPluginConfig() as PeonConfiguration ?? new PeonConfiguration();
                _interface       = new Interface(_pluginInterface, _configuration);
                SetupServices(_pluginInterface);
                _interfaceManager = new InterfaceManager(pluginInterface);
                _addons           = new AddonWatcher(pluginInterface);
                _inputManager     = new InputManager();
                _targeting        = new TargetManager(pluginInterface, _inputManager, _addons);
                _ohBother         = new BotherHelper(_pluginInterface, _addons, _configuration);
                _retainers        = new RetainerManager(_pluginInterface, _targeting, _addons!, _ohBother, _interfaceManager!);
                _login            = new LoginManager(_pluginInterface, _ohBother, _interfaceManager!);
                _chocobos         = new ChocoboManager(_pluginInterface, _targeting, _addons, _ohBother, _interfaceManager);
                _commands         = new CommandManager(_pluginInterface);
                _crafter          = new Crafter(_pluginInterface, _configuration, _commands, _interfaceManager, false);
                BaseAddress       = _pluginInterface.TargetModuleScanner.Module.BaseAddress.ToInt64();
                _loginBar         = new LoginBar(_pluginInterface, _configuration, _login, _interfaceManager);

                _pluginInterface.SavePluginConfig(_configuration);

                _pluginInterface.CommandManager.AddHandler("/retainer", new CommandInfo(OnRetainer)
                {
                    HelpMessage = "Send Retainers, either 'all' or a comma-separated list of indices.",
                    ShowInHelp  = true,
                });

                _pluginInterface.CommandManager.AddHandler("/login", new CommandInfo(OnLogin)
                {
                    HelpMessage =
                        "Log from your current character to another character on the same server. Use \"/login next\" or \"/login previous\" to log to the next or previous character in the list.",
                    ShowInHelp = true,
                });

                _pluginInterface.CommandManager.AddHandler("/craft", new CommandInfo(OnCraft)
                {
                    HelpMessage =
                        "Log from your current character to another character on the same server. Use \"/login next\" or \"/login previous\" to log to the next or previous character in the list.",
                    ShowInHelp = true,
                });
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e}");
            }
        }

        public void Dispose()
        {
            _loginBar.Dispose();
            _ohBother.Dispose();
            _addons.Dispose();
            _interfaceManager.Dispose();
            _interface.Dispose();
            _pluginInterface!.CommandManager.RemoveHandler("/retainer");
            _pluginInterface!.CommandManager.RemoveHandler("/peon");
            _pluginInterface!.CommandManager.RemoveHandler("/login");
            _pluginInterface!.CommandManager.RemoveHandler("/craft");
            _pluginInterface!.Dispose();
        }

        //private void OnRetainer(string command, string arguments)
        //{
        //    if (arguments == string.Empty)
        //    {
        //        Print("Use with [all|miner|botanist|fisher|hunter|gatherer|name].");
        //    }
        //    var argumentParts = arguments.ToLowerInvariant().Split();
        //
        //    switch (argumentParts[0])
        //    {
        //        case "all":
        //        case "miner":
        //        case "botanist":
        //        case "fisher":
        //        case "hunter":
        //    }
        //
        //}

        private void OnCraft(string command, string arguments)
        {
            if (!arguments.Any())
            {
                Print("Please use with <MacroName>(, Amount).");
                return;
            }

            if (arguments == "cancel")
            {
                _crafter.Cancel();
                return;
            }

            var split = arguments.Split(',');

            if (!_configuration.CraftingMacros.TryGetValue(split[0], out var macro))
            {
                Print($"{arguments} is not a valid macro name.");
                return;
            }

            var amount = 1;
            if (split.Length == 2)
            {
                if (!int.TryParse(split[1].Trim(), out amount))
                {
                    amount = 1;
                    Print($"{split[1].Trim()} is not an integer, set amount to 1.");
                }
                else
                {
                    amount = Math.Max(amount, 1);
                }
            }

            Task.Run(async () =>
            {
                for (var i = 0; i < amount; ++i)
                {
                    _crafter.RestartCraft();
                    await _crafter.CompleteCraft(macro);
                }
            });
        }

        private unsafe void OnLogin(string command, string arguments)
        {
            const int defaultTimeOut = 10000;
            switch (arguments)
            {
                case "":
                    Print("Use with a (partial) character name, \"next\", or \"previous\".");
                    return;
                case "next":
                    _login.NextCharacter(defaultTimeOut);
                    return;
                case "previous":
                case "prev":
                    _login.PreviousCharacter(defaultTimeOut);
                    return;
                default:
                    _login.LogTo(arguments, defaultTimeOut);
                    return;
            }
        }

        private unsafe void OnRetainer(string command, string arguments)
        {
            var argumentParts = arguments.Split(',');
            if (arguments == string.Empty || argumentParts.Length < 1)
            {
                _interface.SetVisible();
                return;
            }

            switch (argumentParts[0])
            {
                case "cancel":
                    _retainers!.Cancel();
                    break;
                case "allturnin":
                    Task.Run(() =>
                    {
                        Task.Run(() =>
                        {
                            var ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("GrandCompanySupplyList", 1);
                            if (ptr == IntPtr.Zero)
                                return;

                            PtrGrandCompanySupplyList s = ptr;
                            while (s.Count > 0)
                            {
                                s.Select(0);
                                var task = _interfaceManager.Add("GrandCompanySupplyReward", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                ((PtrGrandCompanySupplyReward) task.Result).Deliver();
                                Task.Delay(75).Wait();
                                task = _interfaceManager.Add("GrandCompanySupplyList", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                s = task.Result;
                            }
                        });
                    });
                    break;

                case "allretainer":
                    _retainers!.DoAllRetainers(RetainerMode.ResendWithGil);
                    break;
                case "retainer":
                    if (argumentParts.Length < 2)
                        return;

                    _retainers!.DoSpecificRetainers(RetainerMode.ResendWithGil, argumentParts[1].Split(' ').Select(int.Parse).ToArray());
                    break;
                case "test":
                    _retainers.SpecificRetainerNewVenture(5, 2, 1, 4);
                    break;
                case "identify":
                    if (argumentParts.Length < 2)
                        return;

                    _retainers.Identifier.Identify(argumentParts[1], RetainerJob.Botanist, out var info);
                    Print($"Botanist {info.Category} {info.LevelRange}, {info.Item}");
                    _retainers.Identifier.Identify(argumentParts[1], RetainerJob.Miner, out info);
                    Print($"Miner {info.Category} {info.LevelRange}, {info.Item}");
                    _retainers.Identifier.Identify(argumentParts[1], RetainerJob.Fisher, out info);
                    Print($"Fisher {info.Category} {info.LevelRange}, {info.Item}");
                    _retainers.Identifier.Identify(argumentParts[1], RetainerJob.Hunter, out info);
                    Print($"Hunter {info.Category} {info.LevelRange}, {info.Item}");
                    break;
                case "synthesize":
                    var ptr = _pluginInterface.Framework.Gui.GetUiObjectByName("RecipeNote", 1);
                    if (ptr != IntPtr.Zero)
                    {
                        PtrRecipeNote s = ptr;
                        s.Synthesize();
                    }

                    break;
                case "chocobo":
                    _chocobos.FeedAllChocobos();
                    break;
                case "bank":
                    var bank = _interfaceManager.Bank();
                    if (bank)
                        bank.Minus();

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
