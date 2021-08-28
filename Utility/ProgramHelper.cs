using System;

namespace Peon
{
    public static class ProgramHelper
    {
        public static void ScanSig(string sig)
        {
            try
            {
                var ptr = Dalamud.SigScanner.ScanText(sig);
                if (ptr != IntPtr.Zero)
                    Dalamud.Chat.Print(
                        $"Found \"{sig}\" at 0x{ptr:X16}, offset +0x{GetOffset(ptr):X}");
            }
            catch (Exception)
            {
                Dalamud.Chat.Print($"Could not find \"{sig}\"");
            }
        }

        public static ulong GetOffset(IntPtr absoluteAddress)
            => (ulong) (absoluteAddress.ToInt64() - Dalamud.SigScanner.Module.BaseAddress.ToInt64());

        public static void PrintOffset(IntPtr absoluteAddress)
            => Dalamud.Chat.Print($"0x{absoluteAddress:X16}, offset +0x{GetOffset(absoluteAddress):X}");

        public static IntPtr GetAbsoluteAddress(int offset)
            => Dalamud.SigScanner.Module.BaseAddress + offset;

        public static void PrintAbsolute(int offset)
            => Dalamud.Chat.Print($"0x{GetAbsoluteAddress(offset):X16}, offset +0x{offset:X}");
    }
}
