using System;
using Peon.Utility;

namespace Peon
{
    public static class HookManagerExtension
    {
        private delegate IntPtr Delegate2Ptr_Ptr(IntPtr a1, IntPtr a2);

        public static void SetHooks(this HookManager hooks)
        {
            hooks.Create<Delegate2Ptr_Ptr>("ResourceUnload", 0x1B2E60, false);
        }
    }
}
