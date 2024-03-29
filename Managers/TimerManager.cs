﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Dalamud.Memory;
using Peon.Crops;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    internal unsafe struct AirshipTimer
    {
        [FieldOffset(0x00)]
        public uint TimeStamp;

        [FieldOffset(0x06)]
        public fixed byte RawName[0x10];

        public string Name
        {
            get
            {
                fixed (byte* name = RawName)
                {
                    return MemoryHelper.ReadStringNullTerminated((IntPtr) name);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    internal unsafe struct SubmersibleTimer
    {
        [FieldOffset(0x00)]
        public uint TimeStamp;

        [FieldOffset(0x08)]
        public fixed byte RawName[0x10];

        public string Name
        {
            get
            {
                fixed (byte* name = RawName)
                {
                    return MemoryHelper.ReadStringNullTerminated((IntPtr)name);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    internal unsafe struct AirshipStatus
    {
        [FieldOffset(0x08)]
        public uint TimeStamp;

        [FieldOffset(0x10)]
        public fixed byte RawName[0x10];

        public string Name
        {
            get
            {
                fixed (byte* name = RawName)
                {
                    return MemoryHelper.ReadStringNullTerminated((IntPtr) name);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x3C)]
    internal unsafe struct SubmersibleStatus
    {
        [FieldOffset(0x08)]
        public uint TimeStamp;

        [FieldOffset(0x16)]
        public fixed byte RawName[0x10];

        public string Name
        {
            get
            {
                fixed (byte* name = RawName)
                {
                    return MemoryHelper.ReadStringNullTerminated((IntPtr) name);
                }
            }
        }
    }

    public unsafe partial class TimerManager : IDisposable
    {
        private readonly InterfaceManager   _interface;
        private readonly PeonTimers         _timers;
        private readonly AddonWatcher       _watcher;
        private readonly RetainerContainer* _retainers;
        private          Retainer*          _retainerList;

        public readonly ushort AirshipTimerOpCode;
        public readonly ushort AirshipStatusOpCode;
        public readonly ushort SubmarineTimerOpCode;
        public readonly ushort SubmarineStatusOpCode;

        public TimerManager(InterfaceManager i, AddonWatcher watcher)
        {
            _interface            = i;
            _watcher              = watcher;
            _timers               = Peon.Timers;
            _retainers            = (RetainerContainer*) new StaticRetainerContainer(Dalamud.SigScanner).Address;
            _position             = Service<PositionInfoAddress>.Get();

            AirshipTimerOpCode    = 0x0166; // Dalamud.GameData.ServerOpCodes["AirshipTimers"]
            AirshipStatusOpCode   = 0x02FE; // Dalamud.GameData.ServerOpCodes["AirshipStatusList"]
            SubmarineTimerOpCode  = 0x0247; // Dalamud.GameData.ServerOpCodes["SubmarineTimers"]
            SubmarineStatusOpCode = 0x01EF; // Dalamud.GameData.ServerOpCodes["SubmarineStatusList"]

            if (Peon.Config.EnableTimers)
            {
                SetMessage();
                SetupPlants();
            }
        }

        public void SetMessage()
            => Dalamud.Network.NetworkMessage += NetworkMessage;

        public void RemoveMessage()
            => Dalamud.Network.NetworkMessage -= NetworkMessage;

        public bool UpdateRetainers()
        {
            if (Dalamud.ClientState.LocalPlayer == null || _retainers == null || _retainers->Ready != 1)
                return false;

            _retainerList = (Retainer*) _retainers->Retainers;

            var playerName =
                $"{Dalamud.ClientState.LocalPlayer!.Name} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";

            var changes = false;
            for (var i = 0; i < _retainers->RetainerCount; ++i)
            {
                var retainer = _retainerList[i];
                var name     = Marshal.PtrToStringUTF8((IntPtr) retainer.Name)!;
                var time     = retainer.VentureComplete == 0 ? DateTime.UnixEpoch : new DateTime((retainer.VentureComplete + 62135596800) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                changes |= _timers.AddOrUpdateRetainer(playerName, name, time);
            }

            return changes;
        }

        public void NetworkMessage(IntPtr data, ushort opCode, uint sourceId, uint targetId, NetworkMessageDirection direction)
        {
            string? fcName = null;

            var changes = false;
            if (opCode == SubmarineTimerOpCode)
            {
                var timer = (SubmersibleTimer*)data;
                for (var i = 0; i < 4; ++i)
                {
                    if (timer[i].RawName[0] == 0)
                        break;

                    fcName ??= $"{Dalamud.ClientState.LocalPlayer!.CompanyTag} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
                    var time = timer[i].TimeStamp == 0 ? DateTime.UnixEpoch : new DateTime((timer[i].TimeStamp + 62135596800) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                    changes |= _timers.AddOrUpdateMachine(fcName, timer[i].Name, time, MachineType.Submarine);
                }
            }
            else if (opCode == AirshipTimerOpCode)
            {
                var timer = (AirshipTimer*)data;
                for (var i = 0; i < 4; ++i)
                {
                    if (timer[i].RawName[0] == 0)
                        break;

                    fcName ??= $"{Dalamud.ClientState.LocalPlayer!.CompanyTag} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
                    var time = timer[i].TimeStamp == 0 ? DateTime.UnixEpoch : new DateTime((timer[i].TimeStamp + 62135596800) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                    changes |= _timers.AddOrUpdateMachine(fcName, timer[i].Name, time, MachineType.Airship);
                }
            }
            else if (opCode == SubmarineStatusOpCode)
            {
                var timer = (SubmersibleStatus*) data;
                for (var i = 0; i < 4; ++i)
                {
                    if (timer[i].RawName[0] == 0)
                        break;

                    fcName ??= $"{Dalamud.ClientState.LocalPlayer!.CompanyTag} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
                    var time = timer[i].TimeStamp == 0 ? DateTime.UnixEpoch : new DateTime((timer[i].TimeStamp + 62135596800) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                    changes |= _timers.AddOrUpdateMachine(fcName, timer[i].Name, time, MachineType.Submarine);
                }
            }
            else if (opCode == AirshipStatusOpCode)
            {
                var timer = (AirshipStatus*) data;
                for (var i = 0; i < 4; ++i)
                {
                    if (timer[i].RawName[0] == 0)
                        break;

                    fcName ??= $"{Dalamud.ClientState.LocalPlayer!.CompanyTag} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
                    var time = timer[i].TimeStamp == 0 ? DateTime.UnixEpoch : new DateTime((timer[i].TimeStamp + 62135596800) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                    changes |= _timers.AddOrUpdateMachine(fcName, timer[i].Name, time, MachineType.Airship);
                }
            }

            if (changes)
                _timers.SaveMachines();
        }

        public void Dispose()
        {
            Dalamud.Network.NetworkMessage -= NetworkMessage;
            DisposePlants();
        }
    }
}
