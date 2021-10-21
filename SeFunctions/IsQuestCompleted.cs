using System;
using Dalamud.Game;

namespace Peon.SeFunctions
{
    public delegate byte IsQuestCompletedDelegate(ushort questId);

    public sealed class IsQuestCompleted : SeFunctionBase<IsQuestCompletedDelegate>
    {
        public bool Check(ushort questId)
            => (byte) Invoke(questId)! == 1;

        public IsQuestCompleted(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 41 88 84 2C ?? ?? ?? ??")
        { }
    }
}
