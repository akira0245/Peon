using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    internal static unsafe class Module
    {
        private const int MaxSize = 0x4800;

        public static byte** GlobalData;
        public static int    Offset;

        public static void Initialize()
        {
            GlobalData = (byte**) Marshal.AllocHGlobal(MaxSize).ToPointer();
            Offset     = 0;
        }

        private static byte** GetLocalData()
        {
            var ret = GlobalData + Offset / sizeof(byte*);
            if (Offset < MaxSize - 2 * 0x48)
                Offset += 0x48;
            else
                Offset = 0;
            Debug.Assert((ulong) ret < (ulong) GlobalData + MaxSize - 0x48);
            return ret;
        }

        public static void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr) GlobalData);
        }

        public static T* Cast<T>(IntPtr ptr) where T : unmanaged
            => (T*) ptr.ToPointer();

        public static string TextNodeToString(AtkTextNode* node)
            => MemoryHelper.ReadStringNullTerminated((IntPtr) node->NodeText.StringPtr)!;

        public static string ImageNodeToTexture(AtkImageNode* node)
        {
            var texInfo = node->PartsList->Parts[node->PartId].UldAsset;
            return texInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
        }

        public static void** ObtainVTable(void* addon)
            => ((AtkEventListener*) addon)->vfunc;


        public delegate void ReceiveEventDelegate(void* atkUnit, ushort eventType, int which, void* source, void* unused);

        public delegate bool ListCallbackDelegate(AtkComponentListItemRenderer* listItem);

        public readonly struct ClickHelper
        {
            public readonly byte** Data;

            public ClickHelper(void* window, void* target)
            {
                Data    = GetLocalData();
                Data[0] = null;
                Data[1] = (byte*) target;
                Data[2] = (byte*) window;
                Data[3] = null;
                Data[4] = null;
                Data[5] = null;
                Data[6] = null;
                Data[7] = null;
                Data[8] = null;
            }
        }

        public readonly struct EventData
        {
            public readonly byte** Data;

            public EventData(AtkComponentListItemRenderer* pointer, ushort idx)
            {
                Data    = GetLocalData();
                Data[0] = (byte*) pointer;
                Data[1] = null;
                Data[2] = (byte*) (idx | ((ulong) idx << 48));
            }

            public EventData(void* dragDropNode, void* unk)
            {
                Data    = GetLocalData();
                Data[0] = (byte*) unk;
                Data[1] = (byte*) dragDropNode;
                Data[2] = (byte*) 0x0805;
            }

            public EventData(int toNewValue, int fromValue = 0)
            {
                Data    = GetLocalData();
                Data[0] = (byte*) toNewValue;
                Data[1] = (byte*) fromValue;
                Data[2] = null;
            }

            public EventData(bool rightClick)
            {
                Data    = GetLocalData();
                Data[0] = (byte*) 0x0001000000000000;
                Data[1] = null;
                Data[2] = null;
            }

            public static EventData CreateEmpty()
                => new(null, 0);
        }

        public static ReceiveEventDelegate ObtainReceiveEventDelegate(void* addon)
        {
            var table = ObtainVTable(addon);
            var ptr   = table[2];
            PluginLog.Verbose("Using Vfunc[2] at +0x{Ptr:X16}", (long) ptr - Peon.BaseAddress);
            return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(new IntPtr(ptr));
        }

        public static void ClickAddon(void* addon, void* target, EventType type, int which, void* eventData, void* helper)
        {
            var receiveEvent = ObtainReceiveEventDelegate(addon);
            receiveEvent(addon, (ushort) type, which, helper, eventData);
        }

        public static void ClickAddon(void* addon, void* target, EventType type, int which, void* eventData)
        {
            var helper = new ClickHelper(addon, target);
            ClickAddon(addon, target, type, which, eventData, helper.Data);
        }

        public static void ClickAddonHelper(void* addon, void* target, EventType type, int which, void* helper)
        {
            var eventData = EventData.CreateEmpty();
            ClickAddon(addon, target, type, which, eventData.Data, helper);
        }

        public static void ClickAddon(void* addon, void* target, EventType type, int which)
        {
            var eventData = EventData.CreateEmpty();
            ClickAddon(addon, target, type, which, eventData.Data);
        }

        public static bool ClickList(void* addon, AtkComponentNode* node, int idx, int value = 0)
        {
            var list = (AtkComponentList*) node->Component;

            if (idx < 0 || idx >= list->ListLength)
                return false;

            var data   = new EventData(list->ItemRendererList[idx].AtkComponentListItemRenderer, (ushort) idx);
            var helper = new ClickHelper(addon, node);
            helper.Data[5] = (byte*) 0x40023;
            ClickAddon(addon, node, EventType.ListIndexChange, value, data.Data, helper.Data);
            return true;
        }

        public static bool ClickList(void* addon, AtkComponentNode* node, ListCallbackDelegate callback, int value = 0)
        {
            var list = (AtkComponentList*) node->Component;
            for (var i = 0; i < list->ListLength; ++i)
            {
                var renderer = list->ItemRendererList[i].AtkComponentListItemRenderer;
                if (!callback(renderer))
                    continue;

                var data   = new EventData(renderer, (ushort) i);
                var helper = new ClickHelper(addon, node);
                helper.Data[5] = (byte*) 0x40023;

                ClickAddon(addon, node, EventType.ListIndexChange, value, data.Data, helper.Data);
                return true;
            }

            return false;
        }
    }
}
