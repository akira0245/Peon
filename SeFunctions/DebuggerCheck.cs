using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;

namespace Peon.SeFunctions
{
    public class DebuggerCheck : SeFunctionBase<Action>, IDisposable
    {
        public DebuggerCheck(SigScanner sigScanner)
            : base(sigScanner, "FF 15 ?? ?? ?? ?? 85 C0 74 11")
        {
            ReadProcessMemory(Process.GetCurrentProcess().Handle, Address, _originalBytes, NopBytes.Length, out _);
            PluginLog.LogVerbose($"Storing debugger check bytes as {string.Join(" ", _originalBytes.Select(b => b.ToString("X2")))}");
        }

        public void NopOut()
        {
            if (Address == IntPtr.Zero)
                return;

            WriteProcessMemory(Process.GetCurrentProcess().Handle, Address, NopBytes, NopBytes.Length, out _);
            PluginLog.Verbose($"Overwriting debugger check with {string.Join(" ", NopBytes.Select(b => b.ToString("X2")))}");
        }

        public void Restore()
        {
            if (Address == IntPtr.Zero)
                return;

            WriteProcessMemory(Process.GetCurrentProcess().Handle, Address, _originalBytes, _originalBytes.Length, out _);
            PluginLog.Verbose($"Overwriting debugger check with {string.Join(" ", _originalBytes.Select(b => b.ToString("X2")))}");
        }

        public void Dispose()
            => Restore();

        private readonly byte[] _originalBytes = new byte[NopBytes.Length];

        private static readonly byte[] NopBytes =
        {
            0x31,
            0xC0,
            0x90,
            0x90,
            0x90,
            0x90,
        };

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
    }
}
