using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Peon.Modules;

namespace Peon.Managers
{
    public class TurnInManager : WorkManager
    {
        private PtrSelectString    _leveKind;
        private PtrJournal         _journal;
        private PtrSelectString    _selectTurnIn;
        private PtrRequest         _request;
        private PtrContextIconMenu _requestFill;
        private PtrJournalResult   _result;

        private string _questName     = string.Empty;
        private string _questGiver    = string.Empty;
        private string _questAccepter = string.Empty;
        private int    _leveType;
        private int    _numTurnIns;

        public TurnInManager(TargetManager target, AddonWatcher addons, BotherHelper bothers, InterfaceManager iManager)
            : base(target, addons, bothers, iManager)
        { }

        protected override WorkState SetInitialState()
        {
            _leveKind = Interface.SelectString();
            if (_leveKind && _leveKind.ItemTexts().Any(t => t.Contains("Leves")))
                return WorkState.TurnInSelectLeveKindOpen;

            _journal = Interface.Journal();
            if (_journal)
                return WorkState.TurnInJournalOpen;

            _selectTurnIn = Interface.SelectIconString();
            if (_selectTurnIn)
                return WorkState.TurnInSelectTurnInOpen;

            _request = Interface.Request();
            if (_request)
                return WorkState.TurnInRequestOpen;

            _requestFill = Interface.ContextIconMenu();
            if (_requestFill)
                return WorkState.TurnInRequestIconOpen;

            _result = Interface.JournalResult();
            if (_result)
                return WorkState.TurnInJournalResultOpen;

            return WorkState.None;
        }

        public void TurnIn(string questName, string questGiver, string questAccepter, int leveType, int num = 1)
        {
            Debug.Assert(num > 0);
            Debug.Assert(questName.Any() && questGiver.Any() && questAccepter.Any() && questGiver != questAccepter);
            Debug.Assert(leveType == 0 || leveType == 1 || leveType == 2);
            _questName     = questName;
            _questGiver    = questGiver;
            _questAccepter = questAccepter;
            _numTurnIns    = num;
            _leveType      = leveType;
            DoWork(TryTurnIn);
        }

        private bool TryTurnIn()
        {
            return State switch
            {
                WorkState.None                     => ContactGiver(),
                WorkState.TurnInSelectLeveKindOpen => SelectLeveType(),
                WorkState.TurnInJournalOpen        => AcceptQuest(),
                WorkState.TurnInQuestAccepted      => ContactAccepter(),
                WorkState.TurnInSelectTurnInOpen   => SelectTurnIn(),
                WorkState.TurnInRequestOpen        => OpenRequestContext(),
                WorkState.TurnInRequestIconOpen    => FillRequest(),
                WorkState.TurnInRequestFilled      => TurnInItems(),
                WorkState.TurnInJournalResultOpen  => CompleteQuest(),
                WorkState.TurnInRepeatOrStop       => RepeatOrStop(),
                _                                  => throw new InvalidEnumArgumentException(),
            };
        }

        private bool SelectLeveType()
        {
            var task = Interface.Add("JournalDetail", true, DefaultTimeOut);
            if (!_leveKind.Select(_leveType))
                return Failure("Invalid leve type.");

            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open Quest Accept Journal.");

            _leveKind = IntPtr.Zero;
            _journal  = task.Result;
            State     = WorkState.TurnInJournalOpen;
            return true;
        }

        private unsafe bool AcceptQuest()
        {
            if (_journal.QuestTitle() != _questName)
                return Failure("Wrong quest selected.");

            _journal.Accept();

            var task = Interface.AddInverted("JournalDetail", false, DefaultTimeOut);
            var leve = (AddonGuildLeve*) Interface.GetUiObject("GuildLeve");
            var closeButton = leve->AtkUnitBase.UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
            Module.ClickAddon(leve, closeButton, EventType.Change, 6);

            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result != IntPtr.Zero)
                return Failure("Could not close Quest Accept Journal.");

            _journal = IntPtr.Zero;
            State    = WorkState.TurnInQuestAccepted;
            return true;
        }

        private bool SelectTurnIn()
        {
            return true;
        }
        private bool OpenRequestContext()
        {
            return true;
        }
        private bool FillRequest()
        {
            return true;
        }
        private bool TurnInItems()
        {
            return true;
        }
        private bool CompleteQuest()
        {
            return true;
        }
        private bool RepeatOrStop()
        {
            return true;
        }

        private bool ContactGiver()
        {
            var task = Interface.Add("SelectString", true, DefaultTimeOut);
            if (task.IsCompleted)
            {
                _leveKind = task.Result;
                State  = WorkState.TurnInSelectLeveKindOpen;
                return true;
            }

            var targetTask = Targets.Interact(_questGiver, DefaultTimeOut / 6);

            Wait(targetTask, DefaultTimeOut / 3);
            switch (targetTask.IsCompleted ? targetTask.Result : TargetingState.TimeOut)
            {
                case TargetingState.ActorNotFound:   return Failure($"Nobody named {_questGiver} in the vicinity.");
                case TargetingState.ActorNotInRange: return Failure($"Too far away from {_questGiver}.");
                case TargetingState.TimeOut:
                case TargetingState.Unknown:
                    return Failure("Unknown error.");
            }

            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure($"Could not contact {_questGiver}.");

            State  = WorkState.TurnInSelectLeveKindOpen;
            _leveKind = task.Result;
            return true;
        }

        private bool ContactAccepter()
        {
            var task = Interface.Add("SelectIconString", true, DefaultTimeOut);
            if (task.IsCompleted)
            {
                _selectTurnIn = task.Result;
                State     = WorkState.TurnInSelectTurnInOpen;
                return true;
            }

            var targetTask = Targets.Interact(_questAccepter, DefaultTimeOut / 6);

            Wait(targetTask, DefaultTimeOut / 3);
            switch (targetTask.IsCompleted ? targetTask.Result : TargetingState.TimeOut)
            {
                case TargetingState.ActorNotFound:   return Failure($"Nobody named {_questAccepter} in the vicinity.");
                case TargetingState.ActorNotInRange: return Failure($"Too far away from {_questAccepter}.");
                case TargetingState.TimeOut:
                case TargetingState.Unknown:
                    return Failure("Unknown error.");
            }

            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure($"Could not contact {_questAccepter}.");

            State     = WorkState.TurnInSelectTurnInOpen;
            _selectTurnIn = task.Result;
            return true;
        }
    }
}
