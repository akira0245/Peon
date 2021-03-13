using System;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    public delegate void InputKeyDelegate(byte unknown, uint flags, uint keyCode, ulong unknownSource);

    public sealed class InputKey : SeFunctionBase<InputKeyDelegate>
    {
        public InputKey(SigScanner sigScanner)
            : base(sigScanner, "89 ?? ?? ?? 55 56 57 41 ?? 41 ?? 48 ?? ?? ?? ?? 48 81 ?? ?? ?? ?? ?? 48 8B ?? ?? ?? ?? ?? 48 33 ?? ?? 89 ?? ?? 4D")
        { }
    }
}
