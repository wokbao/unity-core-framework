using System;
using System.Collections.Generic;
using Core.Feature.EventBus.Abstractions;

namespace Core.Feature.EventBus.Runtime
{
    /// <summary>
    /// 默认事件总线实现，支持基于优先级的发布/订阅。
    /// </summary>
    public sealed class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var list))
            {
                list = new List<Subscription>();
                _subscriptions[type] = list;
            }

            var sub = new Subscription(priority, evt => handler((TEvent)evt), () => Unsubscribe(type, null));
            list.Add(sub);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return new SubscriptionToken(() => Unsubscribe(type, sub));
        }

        public void Publish<TEvent>(TEvent evt)
        {
            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            // 拷贝一份，避免发布期间修改列表导致异常
            var snapshot = list.ToArray();
            foreach (var sub in snapshot)
            {
                try
                {
                    sub.Handler?.Invoke(evt);
                }
                catch (Exception e)
                {
                    // 单个订阅者异常不应阻断其他订阅者
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        private void Unsubscribe(Type type, Subscription target)
        {
            if (!_subscriptions.TryGetValue(type, out var list))
            {
                return;
            }

            if (target == null)
            {
                list.Clear();
            }
            else
            {
                list.Remove(target);
            }

            if (list.Count == 0)
            {
                _subscriptions.Remove(type);
            }
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }

        private sealed class Subscription
        {
            public int Priority { get; }
            public Action<object> Handler { get; }
            public Action OnDispose { get; }

            public Subscription(int priority, Action<object> handler, Action onDispose)
            {
                Priority = priority;
                Handler = handler;
                OnDispose = onDispose;
            }
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private Action _dispose;

            public SubscriptionToken(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
