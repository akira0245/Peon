using System;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    public delegate IntPtr GetUiObjectByNameDelegate(IntPtr baseUIObj, string name, int index);

    public sealed class GetUiObjectByName : SeFunctionBase<GetUiObjectByNameDelegate>
    {
        public GetUiObjectByName(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 48 8B CF 48 89 87 ?? ?? 00 00 E8 ?? ?? ?? ?? 41 B8 01 00 00 00")
        { }
    }
}
