using System;
using System.Collections.Generic;
using Core.Feature.ObjectPooling.Abstractions;
using UnityEngine;

namespace Core.Feature.ObjectPooling.Runtime
{
    /// <summary>
    /// 默认对象池管理器，支持普通对象与 GameObject 的租借/归还。
    /// </summary>
    public sealed class ObjectPoolManager : IObjectPoolManager, IDisposable
    {
        private readonly Dictionary<Type, IObjectPool> _objectPools = new();
        private readonly Dictionary<int, GameObjectPool> _prefabPools = new();

        public T Rent<T>(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var type = typeof(T);
            if (!_objectPools.TryGetValue(type, out var pool))
            {
                pool = new ObjectPool<T>(factory, onRent, onReturn);
                _objectPools[type] = pool;
            }

            return ((ObjectPool<T>)pool).Rent();
        }

        public void Return<T>(T instance)
        {
            var type = typeof(T);
            if (_objectPools.TryGetValue(type, out var pool))
            {
                ((ObjectPool<T>)pool).Return(instance);
            }
        }

        public GameObject Rent(GameObject prefab, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var key = prefab.GetInstanceID();
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, worldPositionStays);
                _prefabPools[key] = pool;
            }

            return pool.Rent(parent, worldPositionStays);
        }

        public void Return(GameObject instance)
        {
            if (instance == null) return;
            var key = instance.GetInstanceID();

            // 通过缓存的 pool 查找
            foreach (var pool in _prefabPools.Values)
            {
                if (pool.Owns(instance))
                {
                    pool.Return(instance);
                    return;
                }
            }

            // 未找到归属时，直接禁用以避免悬挂实例
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
        }

        public void WarmUp(GameObject prefab, int count, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var key = prefab.GetInstanceID();
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, worldPositionStays);
                _prefabPools[key] = pool;
            }

            pool.WarmUp(count, parent, worldPositionStays);
        }

        public void Clear(bool destroyGameObjects = true)
        {
            _objectPools.Clear();

            if (destroyGameObjects)
            {
                foreach (var pool in _prefabPools.Values)
                {
                    pool.Clear();
                }
            }

            _prefabPools.Clear();
        }

        public void Dispose()
        {
            Clear(destroyGameObjects: true);
        }

        private interface IObjectPool { }

        private sealed class ObjectPool<T> : IObjectPool
        {
            private readonly Stack<T> _stack = new();
            private readonly Func<T> _factory;
            private readonly Action<T> _onRent;
            private readonly Action<T> _onReturn;

            public ObjectPool(Func<T> factory, Action<T> onRent, Action<T> onReturn)
            {
                _factory = factory;
                _onRent = onRent;
                _onReturn = onReturn;
            }

            public T Rent()
            {
                var instance = _stack.Count > 0 ? _stack.Pop() : _factory();
                _onRent?.Invoke(instance);
                return instance;
            }

            public void Return(T instance)
            {
                _onReturn?.Invoke(instance);
                _stack.Push(instance);
            }
        }

        private sealed class GameObjectPool
        {
            private readonly GameObject _prefab;
            private readonly List<GameObject> _all = new();
            private readonly Stack<GameObject> _stack = new();
            private readonly Transform _defaultParent;
            private readonly bool _defaultWorldPositionStays;

            public GameObjectPool(GameObject prefab, Transform parent, bool worldPositionStays)
            {
                _prefab = prefab;
                _defaultParent = parent;
                _defaultWorldPositionStays = worldPositionStays;
            }

            public GameObject Rent(Transform parent, bool worldPositionStays)
            {
                var instance = _stack.Count > 0 ? _stack.Pop() : CreateInstance(parent, worldPositionStays);
                if (instance == null)
                {
                    return null;
                }

                if (parent != null && instance.transform.parent != parent)
                {
                    instance.transform.SetParent(parent, worldPositionStays);
                }

                instance.SetActive(true);
                return instance;
            }

            public void Return(GameObject instance)
            {
                if (instance == null) return;
                instance.SetActive(false);
                instance.transform.SetParent(_defaultParent, _defaultWorldPositionStays);
                _stack.Push(instance);
            }

            public void WarmUp(int count, Transform parent, bool worldPositionStays)
            {
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

            public bool Owns(GameObject instance)
            {
                return _all.Contains(instance);
            }

            public void Clear()
            {
                foreach (var go in _all)
                {
                    if (go != null)
                    {
                        UnityEngine.Object.Destroy(go);
                    }
                }
                _all.Clear();
                _stack.Clear();
            }

            private GameObject CreateInstance(Transform parent, bool worldPositionStays)
            {
                var targetParent = parent != null ? parent : _defaultParent;
                var instance = UnityEngine.Object.Instantiate(_prefab, targetParent, worldPositionStays);
                _all.Add(instance);
                return instance;
            }
        }
    }
}
