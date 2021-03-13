using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Peon.Managers;

namespace Peon.Utility
{
    public class TimeOutList : IDisposable
    {
        public struct EventHandlerResetter
        {
            public OnAddonEventDelegate Delegate;
            public OnAddonEventTimeOut? TimeOutHandler;
            public int                  TimeOut;
            public AddonEvent           EventType;
        }

        private readonly DalamudPluginInterface _pluginInterface;
        private readonly AddonWatcher           _addons;

        private readonly LinkedList<EventHandlerResetter> _timeOutList  = new();
        private          int                              _frameCounter = 0;

        public TimeOutList(DalamudPluginInterface pluginInterface, AddonWatcher addons)
        {
            _pluginInterface = pluginInterface;
            _addons          = addons;
        }

        public void Dispose()
        {
            if (_timeOutList.Count == 0)
                return;

            foreach (var resetter in _timeOutList)
                _addons[resetter.EventType] -= resetter.Delegate;

            _timeOutList.Clear();
            _frameCounter                            =  0;
            _pluginInterface.Framework.OnUpdateEvent -= OnUpdateEvent;
        }

        public LinkedListNode<EventHandlerResetter> AddEvent(AddonEvent addonEvent,    OnAddonEventDelegate eventHandler
            , int                                                       timeOutFrames, OnAddonEventTimeOut? timeOutHandler = null)
        {
            var node = _timeOutList.AddLast(new EventHandlerResetter
            {
                EventType      = addonEvent,
                Delegate       = eventHandler,
                TimeOut        = timeOutFrames + _frameCounter,
                TimeOutHandler = timeOutHandler,
            });

            if (_timeOutList.Count == 1)
                _pluginInterface.Framework.OnUpdateEvent += OnUpdateEvent;

            return node;
        }

        public void RemoveEvent(LinkedListNode<EventHandlerResetter> node)
        {
            if (_timeOutList.Count == 1)
            {
                _frameCounter                            =  0;
                _pluginInterface.Framework.OnUpdateEvent -= OnUpdateEvent;
            }

            _timeOutList.Remove(node);
        }

        private void OnUpdateEvent(object _)
        {
            ++_frameCounter;

            var node = _timeOutList.First;
            while (node != null)
            {
                var next  = node.Next;
                var value = node.Value;
                if (_frameCounter > value.TimeOut)
                {
                    _addons[value.EventType] -= value.Delegate;
                    node.Value.TimeOutHandler?.Invoke();
                    RemoveEvent(node);
                }

                node = next;
            }
        }
    }
}
