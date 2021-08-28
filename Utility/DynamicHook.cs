using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dalamud.Hooking;
using Dalamud.Logging;

namespace Peon.Utility
{
    public abstract class DynamicHookBase : IDisposable
    {
        public readonly string Name;
        public readonly IntPtr Address;

        protected DynamicHookBase(string name, int offset)
        {
            Name    = name;
            Address = Dalamud.SigScanner.Module.BaseAddress + offset;
        }

        private static string ParamsToString(params dynamic?[] a)
            => string.Join("\n\t", a.Select((p, i) => p == null ? $"a{i} = null" : $"({p.GetType()}) a{i} = {ToString(p)}"));

        private static string ToString(dynamic a)
        {
            return a switch
            {
                byte b     => b.ToString("X2"),
                IntPtr ptr => ptr.ToString("X16"),
                ulong ul   => ul.ToString("X16"),
                _          => a.ToString(),
            };
        }

        protected Hook<T>? GetHook<T>(int offset) where T : Delegate
        {
            var methodInfo = typeof(T).GetMethod("Invoke")!;

            var methods = typeof(DynamicHookBase).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var chosenMethod = methods.First(m
                => methodInfo.ReturnType != typeof(void) == (m.ReturnType != typeof(void))
             && methodInfo.GetParameters().Length == m.GetParameters().Length);

            var types          = methodInfo.GetParameters().Select(p => p.ParameterType).Append(methodInfo.ReturnType).ToArray();
            var specificMethod = chosenMethod.MakeGenericMethod(types);
            var detour         = (T) specificMethod.CreateDelegate(typeof(T), this);

            try
            {
                var hook = new Hook<T>(Address, detour);
                hook.Enable();
                PluginLog.Debug($"[Hooks] Hooked {Name} on 0x{Address:X16} (+0x{offset:X})");
                return hook;
            }
            catch (Exception e)
            {
                PluginLog.Debug($"[Hooks] Could not hook {Name} on 0x{Address:X16} (+0x{offset:X}):\n{e}");
                return null;
            }
        }

        protected void Detour()
            => FullDetour();

        protected void Detour<T1>(T1 a1)
            => FullDetour(a1);

        protected void Detour<T1, T2>(T1 a1, T2 a2)
            => FullDetour(a1, a2);

        protected void Detour<T1, T2, T3>(T1 a1, T2 a2, T3 a3)
            => FullDetour(a1, a2, a3);

        protected void Detour<T1, T2, T3, T4>(T1 a1, T2 a2, T3 a3, T4 a4)
            => FullDetour(a1, a2, a3, a4);

        protected void Detour<T1, T2, T3, T4, T5>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
            => FullDetour(a1, a2, a3, a4, a5);

        protected void Detour<T1, T2, T3, T4, T5, T6>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            => FullDetour(a1, a2, a3, a4, a5, a6);

        protected void Detour<T1, T2, T3, T4, T5, T6, T7>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
            => FullDetour(a1, a2, a3, a4, a5, a6, a7);

        protected void Detour<T1, T2, T3, T4, T5, T6, T7, T8>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8)
            => FullDetour(a1, a2, a3, a4, a5, a6, a7, a8);

        protected void Detour<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8, T9 a9)
            => FullDetour(a1, a2, a3, a4, a5, a6, a7, a8, a9);

        protected TRet DetourRet<TRet>()
            => FullDetourRet();

        protected TRet? DetourRet<T1, TRet>(T1 a1)
            => FullDetourRet(a1);

        protected TRet? DetourRet<T1, T2, TRet>(T1 a1, T2 a2)
            => FullDetourRet(a1, a2);

        protected TRet? DetourRet<T1, T2, T3, TRet>(T1 a1, T2 a2, T3 a3)
            => FullDetourRet(a1, a2, a3);

        protected TRet? DetourRet<T1, T2, T3, T4, TRet>(T1 a1, T2 a2, T3 a3, T4 a4)
            => FullDetourRet(a1, a2, a3, a4);

        protected TRet? DetourRet<T1, T2, T3, T4, T5, TRet>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
            => FullDetourRet(a1, a2, a3, a4, a5);

        protected TRet? DetourRet<T1, T2, T3, T4, T5, T6, TRet>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            => FullDetourRet(a1, a2, a3, a4, a5, a6);

        protected TRet? DetourRet<T1, T2, T3, T4, T5, T6, T7, TRet>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
            => FullDetourRet(a1, a2, a3, a4, a5, a6, a7);

        protected TRet? DetourRet<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8)
            => FullDetourRet(a1, a2, a3, a4, a5, a6, a7, a8);

        protected TRet? DetourRet<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8, T9 a9)
            => FullDetourRet(a1, a2, a3, a4, a5, a6, a7, a8, a9);

        protected virtual void FullDetour(params dynamic?[] a)
            => throw new NotImplementedException();

        protected virtual dynamic FullDetourRet(params dynamic?[] a)
            => throw new NotImplementedException();

        protected void FullDetourBase(Delegate original, Delegate? pre, Delegate? post, params dynamic?[] a)
        {
            pre?.DynamicInvoke(a);
            PluginLog.Information(a.Length == 0 ? $"{Name} called" : $"{Name} called with\n\t{ParamsToString(a)}");
            original.DynamicInvoke(a);
            pre?.DynamicInvoke(a);
        }

        protected dynamic FullDetourRetBase(Delegate original, Delegate? pre, Delegate? post, params dynamic?[] a)
        {
            var s = ParamsToString(a);
            pre?.DynamicInvoke(a);
            var ret = original.DynamicInvoke(a);
            PluginLog.Information(s.Length == 0
                ? $"{Name} called\n({ret!.GetType()}) ret = {ret}."
                : $"{Name} called with\n\t{s}\n\t{(ret == null ? "ret == null" : $"({ret.GetType()}) ret = {ToString(ret)}")}");

            post?.DynamicInvoke(a.Append(ret).ToArray());
            return ret!;
        }

        public virtual void Enable()
        { }

        public virtual void Disable()
        { }

        public virtual void Dispose()
        { }
    }

    public class DynamicHook<T> : DynamicHookBase where T : Delegate
    {
        public readonly Delegate? PreAction;
        public readonly Delegate? PostAction;
        public readonly Hook<T>?  Hook;

        public DynamicHook(string name, int offset, Delegate? pre = null, Delegate? post = null)
            : base(name, offset)
        {
            PreAction  = pre;
            PostAction = post;
            Hook       = GetHook<T>(offset);
        }

        protected override void FullDetour(params dynamic?[] a)
            => FullDetourBase(Hook!.Original, PreAction, PostAction, a);

        protected override dynamic FullDetourRet(params dynamic?[] a)
            => FullDetourRetBase(Hook!.Original, PreAction, PostAction, a);

        public override void Enable()
            => Hook?.Enable();

        public override void Disable()
            => Hook?.Disable();

        public override void Dispose()
            => Hook?.Dispose();
    }

    public class HookManager : IDisposable
    {
        private readonly List<DynamicHookBase> _hooks = new();

        private bool _enabled;

        public void EnableAll()
        {
            if (_enabled)
                return;

            _enabled = true;
            foreach (var hook in _hooks)
                hook.Enable();
        }

        public void DisableAll()
        {
            if (!_enabled)
                return;

            _enabled = false;
            foreach (var hook in _hooks)
                hook.Disable();
        }

        public void Enable(string name)
            => _hooks.FirstOrDefault(h => h.Name == name)?.Enable();

        public void Disable(string name)
            => _hooks.FirstOrDefault(h => h.Name == name)?.Disable();

        public DynamicHook<T>? Create<T>(string name, int offset, bool enabled, Delegate? pre = null, Delegate? post = null) where T : Delegate
        {
            var preExisting = _hooks.FirstOrDefault(h => h.Name == name);
            if (preExisting != null)
            {
                PluginLog.Error($"[Hooking] Key {name} already exists.");
                return null;
            }

            try
            {
                var hook = new DynamicHook<T>(name, offset, pre, post);
                if (!_enabled || !enabled)
                    hook.Disable();
                _hooks.Add(hook);
                return hook;
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not create hook {name}:\n{e}");
            }

            return null;
        }

        public void Dispose()
        {
            foreach (var hook in _hooks)
            {
                hook.Disable();
                hook.Dispose();
            }

            _hooks.Clear();
        }
    }
}
