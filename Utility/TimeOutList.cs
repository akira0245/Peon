﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin;

namespace Peon.Managers
{
    public class TimeOutList<TRet, TInfo> : IDisposable
    {
        private readonly struct WaitBlock
        {
            public readonly TInfo                      Infos;
            public readonly ulong                      TimeOut;
            public readonly TaskCompletionSource<TRet> Task;

            public WaitBlock(TInfo infos, ulong currentTime, int timeOutMs, TaskCompletionSource<TRet> task)
            {
                Infos   = infos;
                TimeOut = currentTime + (ulong) timeOutMs;
                Task    = task;
            }
        }

        protected readonly DalamudPluginInterface _pluginInterface;
        private readonly   LinkedList<WaitBlock>  _waitList = new();
        protected          ulong                  _currentTime;

        public TimeOutList(DalamudPluginInterface pluginInterface)
            => _pluginInterface = pluginInterface;

        protected virtual TRet OnCheck(TInfo info)
            => throw new NotImplementedException();

        protected virtual bool RetIsValid(TRet ret, TInfo info)
            => throw new NotImplementedException();

        protected virtual void OnTimeout(TInfo info, TaskCompletionSource<TRet> task)
            => throw new NotImplementedException();

        protected virtual string ToString(TInfo info)
            => throw new NotImplementedException();

        protected virtual string ToString(TRet ret)
            => throw new NotImplementedException();

        protected Task<TRet> Add(TInfo info, int timeOutMs)
        {
            var ret = OnCheck(info);
            TaskCompletionSource<TRet> task = new();
            if (RetIsValid(ret, info))
            {
                task.SetResult(ret);
                return task.Task;
            }

            _currentTime = (ulong) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            WaitBlock block = new(info, _currentTime, timeOutMs, task);
            lock (_waitList)
            {
                if (_waitList.Count == 0)
                    _pluginInterface.Framework.OnUpdateEvent += OnFrameworkUpdate;

                _waitList.AddLast(block);
            }

            return task.Task;
        }

        public virtual void Dispose()
        {
            if (_waitList.Count > 0)
            {
                _pluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
                foreach (var x in _waitList)
                    x.Task.SetCanceled();
            }
            
            _waitList.Clear();
        }

        private void RemoveNode(LinkedListNode<WaitBlock> node)
        {
            lock (_waitList)
            {
                _waitList.Remove(node);

                if (_waitList.Count != 0)
                    return;

                _pluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            }
        }

        protected void RemoveNode(Predicate<TInfo> predicate)
        {
            lock (_waitList)
            {
                var node = _waitList.First;
                while (node != null)
                {
                    var next   = node.Next;
                    if (predicate(node.Value.Infos))
                        RemoveNode(node);
                    node       = next;
                }
            }
        }

        private void OnFrameworkUpdate(object _)
        {
            _currentTime = (ulong) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var node = _waitList.First;
            while (node != null)
            {
                var next   = node.Next;
                var block = node.Value;
                if (block.TimeOut < _currentTime)
                {
                    PluginLog.Verbose("[{TimeOutList:l}] Wait for {Name} timed out.", GetType().Name, ToString(node.Value.Infos));
                    OnTimeout(block.Infos, block.Task);
                    RemoveNode(node);
                }

                var ret = OnCheck(node.Value.Infos);
                if (RetIsValid(ret, node.Value.Infos))
                {
                    PluginLog.Verbose("[{TimeOutList:l}] Wait for {Name} returned {Result}.", GetType().Name, ToString(node.Value.Infos), ToString(ret));
                    node.Value.Task.SetResult(ret);
                    RemoveNode(node);
                }

                node       = next;
            }
        }
    }
}