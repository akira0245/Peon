using System;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    // Function VTable[65] for everything inheriting from AtkUnitBase.
    public delegate void OnAddonChangeDelegate(IntPtr a, int b, IntPtr c);

    public sealed class TextErrorOnChange : SeFunctionBase<OnAddonChangeDelegate>
    {
        public TextErrorOnChange(SigScanner sigScanner)
            : base(sigScanner, "83 FA 03 0F 8E 9A 01 00 00 4C 8B DC 53 56 48 81 EC 98 00 00 00 48 8B 05 E4 DB D8 00", 9)
        { }
    }
}
