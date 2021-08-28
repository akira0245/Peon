using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    internal static unsafe class Module
    {
        public static T* Cast<T>(IntPtr ptr) where T : unmanaged
            => (T*) ptr.ToPointer();

        public static string TextNodeToString(AtkTextNode* node)
            => Marshal.PtrToStringAnsi(new IntPtr(node->NodeText.StringPtr))!;

        public static string ImageNodeToTexture(AtkImageNode* node)
        {
            var texInfo = node->PartsList->Parts[node->PartId].UldAsset;
            return texInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
        }

        public static void** ObtainVTable(void* addon)
            => ((AtkEventListener*) addon)->vfunc;


        public delegate void ReceiveEventDelegate(void* atkUnit, ushort eventType, int which, void* source, void* unused);

        public delegate bool ListCallbackDelegate(AtkComponentListItemRenderer* listItem);

        public readonly struct ClickHelper : IDisposable
        {
            public readonly byte** Data;

            public ClickHelper(void* window, void* target)
            {
                Data    = (byte**) Marshal.AllocHGlobal(0x40).ToPointer();
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

            public void Dispose()
            {
                var ptr = new IntPtr(Data);
                Task.Run(() =>
                {
                    Task.Delay(10000).Wait();
                    Marshal.FreeHGlobal(ptr);
                });
            }
        }

        public readonly struct EventData : IDisposable
        {
            public readonly byte** Data;

            public EventData(AtkComponentListItemRenderer* pointer, ushort idx)
            {
                Data    = (byte**) Marshal.AllocHGlobal(0x18).ToPointer();
                Data[0] = (byte*) pointer;
                Data[1] = null;
                Data[2] = (byte*) (idx | ((ulong) idx << 48));
            }

            public EventData(void* dragDropNode, void* unk)
            {
                Data    = (byte**) Marshal.AllocHGlobal(0x18).ToPointer();
                Data[0] = (byte*) unk;
                Data[1] = (byte*) dragDropNode;
                Data[2] = (byte*) 0x0805;
            }

            public EventData(int toNewValue, int fromValue = 0)
            {
                Data    = (byte**) Marshal.AllocHGlobal(0x18).ToPointer();
                Data[0] = (byte*) toNewValue;
                Data[1] = (byte*) fromValue;
            }

            public void Dispose()
            {
                var ptr = new IntPtr(Data);
                Task.Run(() =>
                {
                    Task.Delay(10000).Wait();
                    Marshal.FreeHGlobal(ptr);
                });
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

        public static void ClickAddon(void* addon, void* target, EventType type, int which, void* eventData)
        {
            var       receiveEvent = ObtainReceiveEventDelegate(addon);
            using var helper       = new ClickHelper(addon, target);
            receiveEvent(addon, (ushort) type, which, helper.Data, eventData);
        }

        public static void ClickAddon(void* addon, void* target, EventType type, int which)
        {
            using var eventData = EventData.CreateEmpty();
            ClickAddon(addon, target, type, which, eventData.Data);
        }

        public static bool ClickList(void* addon, AtkComponentNode* node, int idx, int value = 0)
        {
            var list = (AtkComponentList*) node->Component;

            if (idx < 0 || idx >= list->ListLength)
                return false;

            using var data = new EventData(list->ItemRendererList[idx].AtkComponentListItemRenderer, (ushort) idx);
            ClickAddon(addon, node, EventType.ListIndexChange, value, data.Data);
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

                using var data = new EventData(renderer, (ushort) i);
                ClickAddon(addon, node, EventType.ListIndexChange, value, data.Data);
                return true;
            }

            return false;
        }
    }
}
