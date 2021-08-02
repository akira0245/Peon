using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Peon.Bothers;
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

        public DalamudPluginInterface _pluginInterface  = null!;
        public Interface              _interface        = null!;
        public InterfaceManager       _interfaceManager = null!;
        public PeonConfiguration      _configuration    = null!;
        public AddonWatcher           _addons           = null!;
        public BotherHelper           _ohBother         = null!;
        public InputManager           _inputManager     = null!;
        public TargetManager          _targeting        = null!;
        public RetainerManager        _retainers        = null!;
        public LoginManager           _login            = null!;
        public ChocoboManager         _chocobos         = null!;
        public CommandManager         _commands         = null!;
        public Crafter                _crafter          = null!;
        public LoginBar               _loginBar         = null!;
        public Localization           _localization     = null!;

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
                _localization    = new Localization(_pluginInterface);
                LazyString.SetLocalization(_localization);
                _configuration = _pluginInterface.GetPluginConfig() as PeonConfiguration ?? new PeonConfiguration();
                _interface     = new Interface(this, _pluginInterface, _configuration);
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

                _pluginInterface.CommandManager.AddHandler("/peon", new CommandInfo(OnPeon)
                {
                    HelpMessage =
                        "",
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
            _loginBar?.Dispose();
            _ohBother?.Dispose();
            _addons?.Dispose();
            _interfaceManager?.Dispose();
            _interface?.Dispose();
            _pluginInterface!.CommandManager.RemoveHandler("/retainer");
            _pluginInterface!.CommandManager.RemoveHandler("/peon");
            _pluginInterface!.CommandManager.RemoveHandler("/login");
            _pluginInterface!.CommandManager.RemoveHandler("/craft");
            _pluginInterface!.Dispose();
        }

        private int[] ParseIndices(string text)
        {
            var split = text.Split(',');
            if (!split.Any())
                return Array.Empty<int>();

            var ret = new int[split.Length];
            for (var i = 0; i < split.Length; ++i)
            {
                if (!int.TryParse(split[i], out var val) || val < 1 || val > 10)
                    return Array.Empty<int>();

                ret[i] = val - 1;
            }

            return ret;
        }

        private static readonly char[] SplitParams =
        {
            ' ',
        };

        private void OnRetainer(string command, string arguments)
        {
            var argumentParts = arguments.ToLowerInvariant().Split(SplitParams, 3);
            if (argumentParts.Length > 0 && argumentParts[0] == "cancel" || argumentParts[0] == "stop")
            {
                _retainers.Cancel();
                return;
            }

            if (arguments == string.Empty || argumentParts.Length < 2)
                Print("Use with [type of retainer] [resend|fetch|send] <target>, or [cancel|stop].");

            var type = argumentParts[0] switch
            {
                "firstbotanist" => RetainerType.FirstBotanist,
                "firstbtn"      => RetainerType.FirstBotanist,
                "btn1"          => RetainerType.FirstBotanist,
                "1btn"          => RetainerType.FirstBotanist,
                "1bot"          => RetainerType.FirstBotanist,
                "firstbot"      => RetainerType.FirstBotanist,
                "bot1"          => RetainerType.FirstBotanist,
                "botanist"      => RetainerType.AllBotanist,
                "botanists"     => RetainerType.AllBotanist,
                "allbotanist"   => RetainerType.AllBotanist,
                "allbotanists"  => RetainerType.AllBotanist,
                "btn"           => RetainerType.AllBotanist,
                "allbtn"        => RetainerType.AllBotanist,
                "bot"           => RetainerType.AllBotanist,
                "allbot"        => RetainerType.AllBotanist,
                "firstminer"    => RetainerType.FirstMiner,
                "firstmin"      => RetainerType.FirstMiner,
                "1min"          => RetainerType.FirstMiner,
                "min1"          => RetainerType.FirstMiner,
                "miner"         => RetainerType.AllMiner,
                "miners"        => RetainerType.AllMiner,
                "min"           => RetainerType.AllMiner,
                "allminer"      => RetainerType.AllMiner,
                "allminers"     => RetainerType.AllMiner,
                "allmin"        => RetainerType.AllMiner,
                "firstfisher"   => RetainerType.FirstFisher,
                "firstfsh"      => RetainerType.FirstFisher,
                "1fsh"          => RetainerType.FirstFisher,
                "fsh1"          => RetainerType.FirstFisher,
                "fisher"        => RetainerType.AllFisher,
                "fishers"       => RetainerType.AllFisher,
                "fsh"           => RetainerType.AllFisher,
                "allfisher"     => RetainerType.AllFisher,
                "allfishers"    => RetainerType.AllFisher,
                "allfsh"        => RetainerType.AllFisher,
                "firsthunter"   => RetainerType.FirstHunter,
                "1hunter"       => RetainerType.FirstHunter,
                "hunter1"       => RetainerType.FirstHunter,
                "hunter"        => RetainerType.AllHunter,
                "hunters"       => RetainerType.AllHunter,
                "allhunter"     => RetainerType.AllHunter,
                "allhunters"    => RetainerType.AllHunter,
                "firstgatherer" => RetainerType.FirstGatherer,
                "1gatherer"     => RetainerType.FirstGatherer,
                "gatherer1"     => RetainerType.FirstGatherer,
                "gatherer"      => RetainerType.AllGatherer,
                "allgatherer"   => RetainerType.AllGatherer,
                "gatherers"     => RetainerType.AllGatherer,
                "1"             => RetainerType.First,
                "first"         => RetainerType.First,
                "one"           => RetainerType.First,
                "all"           => RetainerType.All,
                _               => RetainerType.Name,
            };
            if (type == RetainerType.Name)
            {
                var indices = ParseIndices(argumentParts[0]);
                if (indices.Any())
                {
                    _retainers.SetRetainers(indices);
                    type = RetainerType.Indices;
                }
                else
                {
                    _retainers.SetRetainer(new CompareString(argumentParts[0], MatchType.CiContains));
                }
            }
            else
            {
                _retainers.SetRetainer(type);
            }

            switch (argumentParts[1])
            {
                case "resend":
                    _retainers.DoAllRetainers(RetainerMode.ResendWithGil);
                    break;
                case "fetch":
                    _retainers.DoAllRetainers(RetainerMode.LootWithGil);
                    break;
                case "send":
                    if (argumentParts.Length < 3)
                    {
                        Print("Please supply a new target when using send.");
                        return;
                    }

                    if (argumentParts[2] == "quick")
                        argumentParts[2] = "quick exploration";
                    _retainers.DoAllRetainers(RetainerMode.LootNewVentureWithGil, argumentParts[2]);

                    break;
                default:
                    Print("Use with [type of retainer] [resend|fetch|send] <target>, or [cancel|stop].");
                    break;
            }
        }

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
                    if (i != amount - 1)
                    {
                        var task = _interfaceManager.Add("RecipeNote", true, 5000);
                        task.Wait();
                        if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        {
                            _pluginInterface.Framework.Gui.Chat.PrintError($"Terminated after {i}/{amount} crafts, Crafting Log did not reopen.");
                            break;
                        }
                    }
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

        private unsafe void OnPeon(string command, string arguments)
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
                case "chocobo":
                    _chocobos.FeedAllChocobos();
                    break;
            }
        }
    }
}
