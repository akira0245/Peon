using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace Peon
{
    public class PeonTimers
    {
        private const string FileName = "timers.json";
        public enum MachineType : byte
        {
            Airship,
            Submarine,
        }

        public Dictionary<string, Dictionary<string, DateTime>> Retainers = new();

        public Dictionary<string, Dictionary<string, (DateTime, MachineType)>> Machines = new();

        public bool AddOrUpdateRetainer(string character, string retainer, DateTime time)
        {
            if (!Retainers.TryGetValue(character, out var retainerList))
            {
                retainerList         = new Dictionary<string, DateTime>(10);
                Retainers[character] = retainerList;
            }

            if (retainerList.TryGetValue(retainer, out var oldTime) && Math.Abs((oldTime - time).TotalMinutes) < 1)
                return false;
            retainerList[retainer] = time;
            return true;
        }

        public bool AddOrUpdateMachine(string freeCompany, string machine, DateTime time, MachineType type = MachineType.Submarine)
        {
            if (!Machines.TryGetValue(freeCompany, out var machineList))
            {
                machineList           = new Dictionary<string, (DateTime, MachineType)>(8);
                Machines[freeCompany] = machineList;
            }

            if (machineList.TryGetValue(machine, out var oldTime) && oldTime.Item1 == time)
                return false;
            machineList[machine] = (time, type);
            return true;
        }

        private static FileInfo GetFile()
            => new(Path.Combine(Dalamud.PluginInterface.ConfigDirectory.FullName, FileName));

        public void Save()
        {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetFile().FullName, data);
        }

        public static PeonTimers Load()
        {
            var file = GetFile();
            if (!file.Exists)
            {
                PeonTimers ret = new();
                ret.Save();
                return ret;
            }

            try
            {
                var data = File.ReadAllText(file.FullName);
                return JsonConvert.DeserializeObject<PeonTimers>(data);
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error loading timers:\n{e}");
                PeonTimers ret = new();
                ret.Save();
                return ret;
            }
        }
    }
}
