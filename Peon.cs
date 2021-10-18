using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Peon.Bothers;
using Peon.Crafting;
using Peon.Gui;
using Peon.Managers;
using Peon.Modules;
using Peon.SeFunctions;
using Peon.Utility;
using CommandManager = Peon.Managers.CommandManager;
using Module = Peon.Modules.Module;

namespace Peon
{
    public class Peon : IDalamudPlugin
    {
        public static string Version = "";

        public string Name
            => "Peon";

        public static PeonConfiguration Config = null!;
        public static PeonTimers        Timers = null!;

        public readonly Interface        Interface;
        public readonly InterfaceManager InterfaceManager;

        public readonly AddonWatcher    Addons;
        public readonly BotherHelper    OhBother;
        public readonly InputManager    InputManager;
        public readonly TargetManager   Targeting;
        public readonly RetainerManager Retainers;
        public readonly LoginManager    Login;
        public readonly ChocoboManager  Chocobos;
        public readonly CommandManager  Commands;
        public readonly Crafter         Crafter;
        public readonly LoginBar        LoginBar;
        public readonly Localization    Localization;
        public readonly DebuggerCheck   DebuggerCheck;
        public readonly HookManager     Hooks = new();
        public readonly BoardManager    Board;
        public readonly TimerManager    TimerManager;
        public readonly TimerWindow     TimerWindow;

        public static long BaseAddress;

        private static void Print(string s)
            => Dalamud.Chat.Print(s);

        public void SetupServices()
        {
            // Addresses.
            Service<GetBaseUiObject>.Set(Dalamud.SigScanner);
            Service<PositionInfoAddress>.Set(Dalamud.SigScanner);
            Service<GetUiObjectByName>.Set(Dalamud.SigScanner);
            Service<InputKey>.Set(Dalamud.SigScanner);
            Service<SelectStringOnSetup>.Set(Dalamud.SigScanner);
            Service<TalkOnUpdate>.Set(Dalamud.SigScanner);
            Service<TextErrorOnChange>.Set(Dalamud.SigScanner);
            Service<YesNoOnSetup>.Set(Dalamud.SigScanner);
            Service<JournalResultOnSetup>.Set(Dalamud.SigScanner);
            Service<SelectStringReceiveEvent>.Set(Dalamud.SigScanner);
            Service<SelectYesnoReceiveEvent>.Set(Dalamud.SigScanner);
        }

        public unsafe Peon(DalamudPluginInterface pluginInterface)
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

            Dalamud.Initialize(pluginInterface);
            DebuggerCheck = new DebuggerCheck(Dalamud.SigScanner);
            DebuggerCheck.NopOut();
            Module.Initialize();

            Localization = new Localization();
            LazyString.SetLocalization(Localization);
            Config    = PeonConfiguration.Load();
            Timers    = PeonTimers.Load();
            SetupServices();
            Interface        = new Interface(this);
            InterfaceManager = new InterfaceManager();
            Addons           = new AddonWatcher();
            InputManager     = new InputManager();
            Targeting        = new TargetManager(InputManager, Addons, InterfaceManager);
            OhBother         = new BotherHelper(Addons);
            Retainers        = new RetainerManager(Targeting, Addons!, OhBother, InterfaceManager!);
            Login            = new LoginManager(OhBother, InterfaceManager!);
            Chocobos         = new ChocoboManager(Targeting, Addons, OhBother, InterfaceManager);
            Commands         = new CommandManager(Dalamud.SigScanner);
            Crafter          = new Crafter(Commands, InterfaceManager, false);
            BaseAddress      = Dalamud.SigScanner.Module.BaseAddress.ToInt64();
            LoginBar         = new LoginBar(Login, InterfaceManager);
            Board            = new BoardManager(Targeting, Addons, OhBother, InterfaceManager);
            TimerManager     = new TimerManager(InterfaceManager, Addons);
            TimerWindow      = new TimerWindow(TimerManager);

            _itemSheet = Dalamud.GameData.GetExcelSheet<Item>()!;
            _items     = new Dictionary<string, (Item, byte)>((int) _itemSheet.RowCount);
            var shopSheet = Dalamud.GameData.GetExcelSheet<GilShopItem>()!;
            foreach (var i in _itemSheet)
                _items[i.Name.ToString().ToLowerInvariant()] = (i, 0);

            var recipeSheet = Dalamud.GameData.GetExcelSheet<Recipe>()!;
            _recipes = new Dictionary<string, Recipe>((int) recipeSheet.RowCount);
            foreach (var r in recipeSheet)
                _recipes[r.ItemResult.Value!.Name.ToString().ToLowerInvariant()] = r;

            Dalamud.Commands.AddHandler("/retainer", new CommandInfo(OnRetainer)
            {
                HelpMessage = "Send Retainers, either 'all' or a comma-separated list of indices.",
                ShowInHelp  = true,
            });

            Dalamud.Commands.AddHandler("/login", new CommandInfo(OnLogin)
            {
                HelpMessage =
                    "Log from your current character to another character on the same server. Use \"/login next\" or \"/login previous\" to log to the next or previous character in the list.",
                ShowInHelp = true,
            });

            Dalamud.Commands.AddHandler("/craft", new CommandInfo(OnCraft)
            {
                HelpMessage =
                    "Log from your current character to another character on the same server. Use \"/login next\" or \"/login previous\" to log to the next or previous character in the list.",
                ShowInHelp = true,
            });

            Dalamud.Commands.AddHandler("/peon", new CommandInfo(OnPeon)
            {
                HelpMessage = "",
                ShowInHelp  = true,
            });

            Dalamud.Commands.AddHandler("/recipe", new CommandInfo(OnRecipe)
            {
                HelpMessage = "",
                ShowInHelp  = false,
            });

            Dalamud.Commands.AddHandler("/dev", new CommandInfo(OnDev)
            {
                HelpMessage = "",
                ShowInHelp  = false,
            });

            Dalamud.Commands.AddHandler("/xlkill", new CommandInfo(OnKill)
            {
                HelpMessage = "",
                ShowInHelp  = false,
            });
            Hooks.SetHooks();
        }


        private static void OnKill(string command, string _)
        {
            Process.GetCurrentProcess().Kill();
        }

        public void Dispose()
        {
            TimerManager.Dispose();
            TimerWindow.Dispose();
            Hooks.Dispose();
            LoginBar?.Dispose();
            OhBother?.Dispose();
            Addons?.Dispose();
            InterfaceManager?.Dispose();
            Interface?.Dispose();
            Dalamud.Commands.RemoveHandler("/recipe");
            Dalamud.Commands.RemoveHandler("/retainer");
            Dalamud.Commands.RemoveHandler("/peon");
            Dalamud.Commands.RemoveHandler("/login");
            Dalamud.Commands.RemoveHandler("/craft");
            Dalamud.Commands.RemoveHandler("/dev");
            Dalamud.Commands.RemoveHandler("/xlkill");
            Module.Dispose();
            DebuggerCheck.Dispose();
        }

        private unsafe void OnDev(string command, string arguments)
        {
            var split = arguments.Split(new[]
            {
                ' ',
            }, 2);
            if (arguments.Length < 2)
                Dalamud.Chat.Print("Please use with [sig|off|abs|hooks] [<Signature>|<Absolute Address>|<Offset>|<On|Off>].");
            switch (split[0].ToLowerInvariant())
            {
                case "test":
                {
                    var info  = Service<PositionInfoAddress>.Get();
                    var house = info.House;
                    if (house == 0)
                        Dalamud.Chat.Print($"{info.Zone} Ward {info.Ward}, {info.Plot} {(info.Subdivision ? "(Subdivision)" : "")}");
                    else
                        Dalamud.Chat.Print($"{info.Zone} Ward {info.Ward}, {info.House} Floor {info.Floor}");
                    break;
                }
                case "sig":
                    ProgramHelper.ScanSig(split[1].Trim());
                    return;
                case "off":
                    if (IntPtr.TryParse(split[1].Trim(), out var ptr))
                        ProgramHelper.PrintOffset(ptr);
                    else
                        Dalamud.Chat.PrintError($"Could not parse {split[1]} as a pointer.");
                    return;
                case "abs":
                    if (int.TryParse(split[1].Trim(), out var offset))
                        ProgramHelper.PrintAbsolute(offset);
                    else
                        Dalamud.Chat.PrintError($"Could not parse {split[1]} as an integer.");
                    return;
                case "hooks":
                    split = split[1].Split(new[]
                    {
                        ' ',
                    }, 2);
                    switch (split[0].ToLowerInvariant())
                    {
                        case "on":
                            if (split.Length > 1)
                                Hooks.Enable(split[1]);
                            else
                                Hooks.EnableAll();
                            return;
                        case "off":
                            if (split.Length > 1)
                                Hooks.Disable(split[1]);
                            else
                                Hooks.DisableAll();
                            return;
                        default:
                            Dalamud.Chat.PrintError("Use hooks with On or Off.");
                            return;
                    }
                default:
                    Dalamud.Chat.Print("Please use with [sig|off|abs|hooks] [<Signature>|<Absolute Address>|<Offset>|<On|Off>].");
                    return;
            }
        }

        private static int[] ParseIndices(string text)
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

        private readonly ExcelSheet<Item>                 _itemSheet;
        private readonly Dictionary<string, (Item, byte)> _items;
        private readonly Dictionary<string, Recipe>       _recipes;

        private void OnRecipe(string command, string arguments)
        {
            if (!arguments.Any())
            {
                Print("Please enter recipe name.");
                return;
            }

            void PrintItem(string pre, Item it, byte sold)
                => Print(
                    $"{pre}{it.Name:32}{(it.IsUntradable ? " - Untradable" : "")}{(sold == 1 ? $"- {it.PriceMid} gil{(sold == 2 ? "*" : "")}." : " - Not sold.")}")
            ;

            void PrintRecipe(string pre, Recipe r)
            {
                PrintItem(pre, r.ItemResult.Value!, _items[r.ItemResult.Value!.Name.ToString().ToLowerInvariant()].Item2);
                foreach (var mat in r.UnkStruct5.Where(u => u.AmountIngredient > 0))
                {
                    var it = _itemSheet.GetRow((uint) mat.ItemIngredient)!;
                    if (_recipes.TryGetValue(it.Name.ToString().ToLowerInvariant(), out var subr))
                        PrintRecipe($"{pre} -- {mat.AmountIngredient} x ", subr);
                    else
                        PrintItem($"{pre} -- {mat.AmountIngredient} x ", it, _items[it.Name.ToString().ToLowerInvariant()].Item2);
                }
            }

            var lower = arguments.ToLowerInvariant();
            if (!_recipes.TryGetValue(lower, out var recipe))
            {
                if (!_items.TryGetValue(lower, out var item))
                {
                    Print($"Could not identify {arguments}.");
                    return;
                }

                PrintItem("", item.Item1, item.Item2);
                return;
            }

            PrintRecipe("", recipe);
        }

        private void OnRetainer(string command, string arguments)
        {
            var argumentParts = arguments.ToLowerInvariant().Split(SplitParams, 3);
            if (argumentParts.Length > 0 && argumentParts[0] == "cancel" || argumentParts[0] == "stop")
            {
                Retainers.Cancel();
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
                    Retainers.SetRetainers(indices);
                else
                    Retainers.SetRetainer(new CompareString(argumentParts[0], MatchType.CiContains));
            }
            else
            {
                Retainers.SetRetainer(type);
            }

            switch (argumentParts[1])
            {
                case "resend":
                    Retainers.DoAllRetainers(RetainerMode.ResendWithGil);
                    break;
                case "fetch":
                    Retainers.DoAllRetainers(RetainerMode.LootWithGil);
                    break;
                case "send":
                    if (argumentParts.Length < 3)
                    {
                        Print("Please supply a new target when using send.");
                        return;
                    }

                    if (argumentParts[2] == "quick")
                        argumentParts[2] = "quick exploration";
                    Retainers.DoAllRetainers(RetainerMode.LootNewVentureWithGil, argumentParts[2]);

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
                Crafter.Cancel();
                return;
            }

            var split = arguments.Split(',');

            if (!Config.CraftingMacros.TryGetValue(split[0], out var macro))
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
                    Crafter.RestartCraft();
                    await Crafter.CompleteCraft(macro);
                    if (i != amount - 1)
                    {
                        var task = InterfaceManager.Add("RecipeNote", true, 5000);
                        task.Wait();
                        if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        {
                            Dalamud.Chat.PrintError(
                                $"Terminated after {i}/{amount} crafts, Crafting Log did not reopen.");
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
                    Login.NextCharacter(defaultTimeOut);
                    return;
                case "previous":
                case "prev":
                    Login.PreviousCharacter(defaultTimeOut);
                    return;
                default:
                    Login.LogTo(arguments, defaultTimeOut);
                    return;
            }
        }

        private unsafe void OnPeon(string command, string arguments)
        {
            var argumentParts = arguments.Split(',');
            if (arguments == string.Empty || argumentParts.Length < 1)
            {
                Interface.SetVisible();
                return;
            }

            switch (argumentParts[0])
            {
                case "cancel":
                    Retainers!.Cancel();
                    Chocobos.Cancel();
                    Board.Cancel();
                    break;
                case "allturnin":
                    Task.Run(() =>
                    {
                        Task.Run(() =>
                        {
                            var ptr = Dalamud.GameGui.GetAddonByName("GrandCompanySupplyList", 1);
                            if (ptr == IntPtr.Zero)
                                return;

                            PtrGrandCompanySupplyList s = ptr;
                            while (s && s.Count > 0)
                            {
                                s.Select(0);
                                var task = InterfaceManager.Add("GrandCompanySupplyReward", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                ((PtrGrandCompanySupplyReward) task.Result).Deliver();
                                Task.Delay(75).Wait();
                                task = InterfaceManager.Add("GrandCompanySupplyList", false, 3000);
                                task.SafeWait();
                                if (task.IsCanceled)
                                    return;

                                s = task.Result;
                            }
                        });
                    });
                    break;
                case "chocobo":
                    Chocobos.FeedAllChocobos();
                    break;
                case "buyplot":
                    Board.StartBuying(100, 150, true, false, true);
                    break;
                case "buyplotkill":
                    Board.StartBuying(100, 150, true, true);
                    break;
            }
        }
    }
}
