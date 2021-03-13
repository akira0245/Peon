using System;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    public delegate IntPtr GetBaseUiObjectDelegate();

    public sealed class GetBaseUiObject : SeFunctionBase<GetBaseUiObjectDelegate>
    {
        public GetBaseUiObject(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF")
        { }
    }
}
