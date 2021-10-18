using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using Newtonsoft.Json;
using Peon.Crops;
using Peon.Managers;

namespace Peon
{
    using MachineDict = Dictionary<string, Dictionary<string, (DateTime, MachineType)>>;
    using RetainerDict = Dictionary<string, Dictionary<string, DateTime>>;

    public enum MachineType : byte
    {
        Airship,
        Submarine,
    }

    public class PeonTimers
    {
        private const string FileNameMachines  = "timers_machines.json";
        private const string FileNameRetainers = "timers_retainers.json";
        private const string FileNameCrops     = "timers_crops.json";

        public RetainerDict Retainers = new();
        public MachineDict  Machines  = new();
        public CropTimers   Crops     = new();

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

            if (machineList.TryGetValue(machine, out var oldTime) && Math.Abs((oldTime.Item1 - time).TotalMinutes) < 1)
                return false;

            machineList[machine] = (time, type);
            return true;
        }

        private static FileInfo GetFileMachines()
            => new(Path.Combine(Dalamud.PluginInterface.ConfigDirectory.FullName, FileNameMachines));

        private static FileInfo GetFileRetainers()
            => new(Path.Combine(Dalamud.PluginInterface.ConfigDirectory.FullName, FileNameRetainers));

        private static FileInfo GetFileCrops()
            => new(Path.Combine(Dalamud.PluginInterface.ConfigDirectory.FullName, FileNameCrops));

        public void SaveMachines()
        {
            var data = JsonConvert.SerializeObject(Machines, Formatting.Indented);
            File.WriteAllText(GetFileMachines().FullName, data);
        }

        public void SaveRetainers()
        {
            var data = JsonConvert.SerializeObject(Retainers, Formatting.Indented);
            File.WriteAllText(GetFileRetainers().FullName, data);
        }

        public void SaveCrops()
        {
            var data = JsonConvert.SerializeObject(Crops, Formatting.Indented);
            File.WriteAllText(GetFileCrops().FullName, data);
        }

        private void LoadRetainers()
        {
            var file = GetFileRetainers();
            if (!file.Exists)
            {
                SaveRetainers();
            }
            else
            {
                try
                {
                    var data = File.ReadAllText(file.FullName);
                    Retainers = JsonConvert.DeserializeObject<RetainerDict>(data);
                }
                catch(Exception e)
                {
                    PluginLog.Error($"Error loading retainer timers:\n{e}");
                    Retainers = new RetainerDict();
                    SaveRetainers();
                }
            }
        }

        private void LoadMachines()
        {
            var file = GetFileMachines();
            if (!file.Exists)
            {
                SaveMachines();
            }
            else
            {
                try
                {
                    var data = File.ReadAllText(file.FullName);
                    Machines = JsonConvert.DeserializeObject<MachineDict>(data);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Error loading machine timers:\n{e}");
                    Machines = new MachineDict();
                    SaveMachines();
                }
            }
        }

        private void LoadCrops()
        {
            var file = GetFileCrops();
            if (!file.Exists)
            {
                SaveCrops();
            }
            else
            {
                try
                {
                    var data = File.ReadAllText(file.FullName);
                    Crops = JsonConvert.DeserializeObject<CropTimers>(data);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Error loading crop timers:\n{e}");
                    Crops = new CropTimers();
                    SaveRetainers();
                }
            }
        }

        public static PeonTimers Load()
        {
            PeonTimers ret  = new();
            ret.LoadRetainers();
            ret.LoadMachines();
            ret.LoadCrops();
            return ret;
        }
    }
}
