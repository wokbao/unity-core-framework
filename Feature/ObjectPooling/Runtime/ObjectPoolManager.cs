using System;
using System.Collections.Generic;
using Core.Feature.ObjectPooling.Abstractions;
using UnityEngine;

namespace Core.Feature.ObjectPooling.Runtime
{
    /// <summary>
    /// 对象池管理器的默认实现，支持普通 C# 对象和 Unity GameObject 的高效租借与归还。
    /// 该管理器自动为不同类型的对象创建和维护对应的对象池，避免频繁创建和销毁对象带来的性能开销。
    /// </summary>
    public sealed class ObjectPoolManager : IObjectPoolManager, IDisposable
    {
        /// <summary>
        /// 存储普通 C# 对象的对象池字典，键为对象类型，值为对应的对象池实例
        /// </summary>
        private readonly Dictionary<Type, IObjectPool> _objectPools = new();

        /// <summary>
        /// 存储 GameObject 的对象池字典，键为预制体的实例 ID，值为对应的游戏对象池实例
        /// </summary>
        private readonly Dictionary<int, GameObjectPool> _prefabPools = new();

        /// <summary>
        /// 从对象池中租借一个指定类型的普通 C# 对象
        /// </summary>
        /// <typeparam name="T">要租借的对象类型</typeparam>
        /// <param name="factory">当对象池为空时用于创建新对象的工厂方法</param>
        /// <param name="onRent">对象被租借时调用的回调函数（可选）</param>
        /// <param name="onReturn">对象被归还时调用的回调函数（可选）</param>
        /// <returns>租借到的对象实例</returns>
        /// <exception cref="ArgumentNullException">当 factory 为 null 时抛出</exception>
        public T Rent<T>(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var type = typeof(T);

            // 如果不存在该类型的对象池，则创建一个新的
            if (!_objectPools.TryGetValue(type, out var pool))
            {
                pool = new ObjectPool<T>(factory, onRent, onReturn);
                _objectPools[type] = pool;
            }

            // 从对象池中租借对象
            return ((ObjectPool<T>)pool).Rent();
        }

        /// <summary>
        /// 将一个普通 C# 对象归还到对应的对象池中
        /// </summary>
        /// <typeparam name="T">要归还的对象类型</typeparam>
        /// <param name="instance">要归还的对象实例</param>
        public void Return<T>(T instance)
        {
            var type = typeof(T);

            // 如果存在该类型的对象池，则归还对象
            if (_objectPools.TryGetValue(type, out var pool))
            {
                ((ObjectPool<T>)pool).Return(instance);
            }
        }

        /// <summary>
        /// 从对象池中租借一个 GameObject 实例
        /// </summary>
        /// <param name="prefab">用于创建实例的预制体</param>
        /// <param name="parent">实例化时的父变换（可选）</param>
        /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变（可选，默认为 false）</param>
        /// <returns>租借到的 GameObject 实例</returns>
        /// <exception cref="ArgumentNullException">当 prefab 为 null 时抛出</exception>
        public GameObject Rent(GameObject prefab, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var key = prefab.GetInstanceID();

            // 如果不存在该预制体的对象池，则创建一个新的
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, worldPositionStays);
                _prefabPools[key] = pool;
            }

            // 从对象池中租借 GameObject
            return pool.Rent(parent, worldPositionStays);
        }

        /// <summary>
        /// 将一个 GameObject 实例归还到对应的对象池中
        /// </summary>
        /// <param name="instance">要归还的 GameObject 实例</param>
        public void Return(GameObject instance)
        {
            if (instance == null) return;

            // 遍历所有 GameObject 池，查找该实例所属的对象池
            foreach (var pool in _prefabPools.Values)
            {
                if (pool.Owns(instance))
                {
                    pool.Return(instance);
                    return;
                }
            }

            // 如果未找到归属的对象池，直接销毁该实例以避免内存泄漏
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
        }

        /// <summary>
        /// 预热指定数量的 GameObject 实例到对象池中
        /// </summary>
        /// <param name="prefab">用于创建实例的预制体</param>
        /// <param name="count">要预热的实例数量</param>
        /// <param name="parent">实例化时的父变换（可选）</param>
        /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变（可选，默认为 false）</param>
        /// <exception cref="ArgumentNullException">当 prefab 为 null 时抛出</exception>
        public void WarmUp(GameObject prefab, int count, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var key = prefab.GetInstanceID();

            // 如果不存在该预制体的对象池，则创建一个新的
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, worldPositionStays);
                _prefabPools[key] = pool;
            }

            // 预热指定数量的实例
            pool.WarmUp(count, parent, worldPositionStays);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        /// <param name="destroyGameObjects">是否销毁所有 GameObject 实例（默认为 true）</param>
        public void Clear(bool destroyGameObjects = true)
        {
            // 清空普通对象池
            _objectPools.Clear();

            // 如果需要销毁 GameObject，则清空所有游戏对象池
            if (destroyGameObjects)
            {
                foreach (var pool in _prefabPools.Values)
                {
                    pool.Clear();
                }
            }

            // 清空游戏对象池字典
            _prefabPools.Clear();
        }

        /// <summary>
        /// 释放资源，清空所有对象池并销毁所有 GameObject 实例
        /// </summary>
        public void Dispose()
        {
            Clear(destroyGameObjects: true);
        }

        /// <summary>
        /// 对象池接口，用于统一管理不同类型的对象池
        /// </summary>
        private interface IObjectPool
        {
        }

        /// <summary>
        /// 泛型对象池类，用于管理普通 C# 对象
        /// </summary>
        /// <typeparam name="T">对象池管理的对象类型</typeparam>
        private sealed class ObjectPool<T> : IObjectPool
        {
            /// <summary>
            /// 存储对象的栈，使用栈实现后进先出的对象复用策略
            /// </summary>
            private readonly Stack<T> _stack = new();

            /// <summary>
            /// 创建新对象的工厂方法
            /// </summary>
            private readonly Func<T> _factory;

            /// <summary>
            /// 对象被租借时调用的回调函数
            /// </summary>
            private readonly Action<T> _onRent;

            /// <summary>
            /// 对象被归还时调用的回调函数
            /// </summary>
            private readonly Action<T> _onReturn;

            /// <summary>
            /// 初始化对象池实例
            /// </summary>
            /// <param name="factory">创建新对象的工厂方法</param>
            /// <param name="onRent">对象被租借时的回调函数</param>
            /// <param name="onReturn">对象被归还时的回调函数</param>
            public ObjectPool(Func<T> factory, Action<T> onRent, Action<T> onReturn)
            {
                _factory = factory;
                _onRent = onRent;
                _onReturn = onReturn;
            }

            /// <summary>
            /// 从对象池中租借一个对象
            /// </summary>
            /// <returns>租借到的对象实例</returns>
            public T Rent()
            {
                // 如果栈不为空，从栈中弹出一个对象；否则使用工厂方法创建新对象
                var instance = _stack.Count > 0 ? _stack.Pop() : _factory();

                // 调用租借回调函数
                _onRent?.Invoke(instance);

                return instance;
            }

            /// <summary>
            /// 将对象归还到对象池中
            /// </summary>
            /// <param name="instance">要归还的对象实例</param>
            public void Return(T instance)
            {
                // 调用归还回调函数
                _onReturn?.Invoke(instance);

                // 将对象推入栈中
                _stack.Push(instance);
            }
        }

        /// <summary>
        /// GameObject 对象池类，用于管理 Unity 游戏对象
        /// </summary>
        private sealed class GameObjectPool
        {
            /// <summary>
            /// 用于创建实例的预制体
            /// </summary>
            private readonly GameObject _prefab;

            /// <summary>
            /// 存储所有已创建的 GameObject 实例的列表，用于跟踪和管理所有实例
            /// </summary>
            private readonly List<GameObject> _all = new();

            /// <summary>
            /// 存储可用（未被租借）GameObject 实例的栈
            /// </summary>
            private readonly Stack<GameObject> _stack = new();

            /// <summary>
            /// 默认的父变换，用于在归还时重置 GameObject 的父对象
            /// </summary>
            private readonly Transform _defaultParent;

            /// <summary>
            /// 默认的世界位置保持设置，用于在归还时重置 GameObject 的位置
            /// </summary>
            private readonly bool _defaultWorldPositionStays;

            /// <summary>
            /// 初始化 GameObject 对象池实例
            /// </summary>
            /// <param name="prefab">用于创建实例的预制体</param>
            /// <param name="parent">默认的父变换</param>
            /// <param name="worldPositionStays">默认的世界位置保持设置</param>
            public GameObjectPool(GameObject prefab, Transform parent, bool worldPositionStays)
            {
                _prefab = prefab;
                _defaultParent = parent;
                _defaultWorldPositionStays = worldPositionStays;
            }

            /// <summary>
            /// 从对象池中租借一个 GameObject 实例
            /// </summary>
            /// <param name="parent">实例化时的父变换</param>
            /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变</param>
            /// <returns>租借到的 GameObject 实例</returns>
            public GameObject Rent(Transform parent, bool worldPositionStays)
            {
                // 如果栈不为空，从栈中弹出一个 GameObject；否则创建新实例
                var instance = _stack.Count > 0 ? _stack.Pop() : CreateInstance(parent, worldPositionStays);
                if (instance == null)
                {
                    return null;
                }

                // 如果指定了父变换且与当前父变换不同，则更新父变换
                if (parent != null && instance.transform.parent != parent)
                {
                    instance.transform.SetParent(parent, worldPositionStays);
                }

                // 激活 GameObject
                instance.SetActive(true);
                return instance;
            }

            /// <summary>
            /// 将 GameObject 实例归还到对象池中
            /// </summary>
            /// <param name="instance">要归还的 GameObject 实例</param>
            public void Return(GameObject instance)
            {
                if (instance == null) return;

                // 停用 GameObject
                instance.SetActive(false);

                // 重置父变换
                instance.transform.SetParent(_defaultParent, _defaultWorldPositionStays);

                // 将 GameObject 推入栈中
                _stack.Push(instance);
            }

            /// <summary>
            /// 预热指定数量的 GameObject 实例到对象池中
            /// </summary>
            /// <param name="count">要预热的实例数量</param>
            /// <param name="parent">实例化时的父变换</param>
            /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变</param>
            public void WarmUp(int count, Transform parent, bool worldPositionStays)
            {
                // 创建指定数量的实例并推入栈中
                for (var i = 0; i < count; i++)
                {
                    var instance = CreateInstance(parent, worldPositionStays);
                    if (instance != null)
                    {
                        instance.SetActive(false);
                        _stack.Push(instance);
                    }
                }
            }

            /// <summary>
            /// 检查指定的 GameObject 实例是否归当前对象池所有
            /// </summary>
            /// <param name="instance">要检查的 GameObject 实例</param>
            /// <returns>如果实例属于当前对象池则返回 true，否则返回 false</returns>
            public bool Owns(GameObject instance)
            {
                return _all.Contains(instance);
            }

            /// <summary>
            /// 清空对象池并销毁所有 GameObject 实例
            /// </summary>
            public void Clear()
            {
                // 销毁所有 GameObject 实例
                foreach (var go in _all)
                {
                    if (go != null)
                    {
                        UnityEngine.Object.Destroy(go);
                    }
                }

                // 清空实例列表和栈
                _all.Clear();
                _stack.Clear();
            }

            /// <summary>
            /// 创建一个新的 GameObject 实例
            /// </summary>
            /// <param name="parent">实例化时的父变换</param>
            /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变</param>
            /// <returns>创建的 GameObject 实例</returns>
            private GameObject CreateInstance(Transform parent, bool worldPositionStays)
            {
                // 确定最终的父变换
                var targetParent = parent != null ? parent : _defaultParent;

                // 实例化 GameObject
                var instance = UnityEngine.Object.Instantiate(_prefab, targetParent, worldPositionStays);

                // 将实例添加到管理列表中
                _all.Add(instance);

                return instance;
            }
        }
    }
}