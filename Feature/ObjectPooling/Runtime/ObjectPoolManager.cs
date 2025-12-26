using System;
using System.Collections.Generic;
using System.Threading;
using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Abstractions;
using Core.Feature.ObjectPooling.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Feature.ObjectPooling.Runtime
{
    /// <summary>
    /// 对象池管理器的默认实现，支持普通 C# 对象和 Unity GameObject 的高效租借与归还。
    /// 该管理器自动为不同类型的对象创建和维护对应的对象池，避免频繁创建和销毁对象带来的性能开销。
    /// </summary>
    public sealed class ObjectPoolManager : IObjectPoolManager, IDisposable
    {
        private readonly IAssetProvider _assetProvider;
        private readonly ILogService _logService;

        /// <summary>
        /// 存储普通 C# 对象的对象池字典，键为对象类型，值为对应的对象池实例
        /// </summary>
        private readonly Dictionary<Type, IObjectPool> _objectPools = new();

        /// <summary>
        /// 存储 GameObject 的对象池字典，键为预制体的实例 ID，值为对应的游戏对象池实例
        /// </summary>
        private readonly Dictionary<int, GameObjectPool> _prefabPools = new();

        /// <summary>
        /// GameObject 实例到对象池的反向映射（优化 Return 性能）
        /// </summary>
        private readonly Dictionary<GameObject, GameObjectPool> _instanceToPool = new();

        /// <summary>
        /// Addressable Key 到预制体的缓存映射
        /// </summary>
        private readonly Dictionary<string, GameObject> _addressableCache = new();

        public ObjectPoolManager(IAssetProvider assetProvider, ILogService logService)
        {
            _assetProvider = assetProvider;
            _logService = logService;
        }

        /// <summary>
        /// 从对象池中租借一个指定类型的普通 C# 对象
        /// </summary>
        /// <typeparam name="T">要租借的对象类型</typeparam>
        /// <param name="factory">当对象池为空时用于创建新对象的工厂方法</param>
        /// <param name="onRent">对象被租借时调用的回调函数（可选）</param>
        /// <param name="onReturn">对象被归还时调用的回调函数（可选）</param>
        /// <returns>租借到的对象实例</returns>
        /// <exception cref="ArgumentNullException">当 factory 为 null 时抛出</exception>
        public T Rent<T>(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null, int maxCapacity = 100)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory), "工厂方法不能为 null");
            var type = typeof(T);

            // 如果不存在该类型的对象池，则创建一个新的
            if (!_objectPools.TryGetValue(type, out var pool))
            {
                pool = new ObjectPool<T>(factory, onRent, onReturn, maxCapacity);
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
        public GameObject Rent(GameObject prefab, Transform parent = null, bool worldPositionStays = false,
                               Action<GameObject> onReset = null, int maxCapacity = 50)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab), "预制体不能为 null");
            var key = prefab.GetInstanceID();

            // 如果不存在该预制体的对象池，则创建一个新的
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(this, prefab, parent, worldPositionStays, onReset, maxCapacity);
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

            // 【优化1：反向映射】O(1) 查找，替代 O(n*m) 遍历
            if (_instanceToPool.TryGetValue(instance, out var pool))
            {
                pool.Return(instance);
                return;
            }

            // 如果未找到归属的对象池，直接销毁该实例以避免内存泄漏
            _logService?.Warning(LogCategory.Core, $"[对象池] 归还的对象 {instance.name} 不属于任何对象池，已销毁");
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
        public async UniTask<GameObject> RentAsync(string addressableKey, Transform parent = null,
                                                     bool worldPositionStays = false,
                                                     Action<GameObject> onReset = null,
                                                     int maxCapacity = 50,
                                                     CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(addressableKey))
                throw new ArgumentException("Addressable Key 不能为空", nameof(addressableKey));

            // 检查缓存
            if (!_addressableCache.TryGetValue(addressableKey, out var prefab))
            {
                // 【优化5：异步支持】通过 IAssetProvider 加载
                prefab = await _assetProvider.LoadAssetAsync<GameObject>(addressableKey, ct);

                if (prefab == null)
                    throw new InvalidOperationException($"无法从 Addressable Key '{addressableKey}' 加载预制体");

                _addressableCache[addressableKey] = prefab;
            }

            return Rent(prefab, parent, worldPositionStays, onReset, maxCapacity);
        }

        public void WarmUp(GameObject prefab, int count, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab), "预制体不能为 null");
            var key = prefab.GetInstanceID();

            // 如果不存在该预制体的对象池，则创建一个新的
            if (!_prefabPools.TryGetValue(key, out var pool))
            {
                pool = new GameObjectPool(this, prefab, parent, worldPositionStays, null, 50);
                _prefabPools[key] = pool;
            }

            // 预热指定数量的实例
            pool.WarmUp(count, parent, worldPositionStays);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        /// <param name="destroyGameObjects">是否销毁所有 GameObject 实例（默认为 true）</param>
        public PoolStatistics GetStatistics(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab), "预制体不能为 null");
            var key = prefab.GetInstanceID();

            return _prefabPools.TryGetValue(key, out var pool)
                ? pool.GetStatistics()
                : default;
        }

        public PoolStatistics GetStatistics<T>()
        {
            var type = typeof(T);
            return _objectPools.TryGetValue(type, out var pool)
                ? ((ObjectPool<T>)pool).GetStatistics()
                : default;
        }

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

            // 清空游戏对象池字典和反向映射
            _prefabPools.Clear();
            _instanceToPool.Clear();

            // 释放 Addressables 缓存
            foreach (var key in _addressableCache.Keys)
            {
                _assetProvider.Release(key);
            }
            _addressableCache.Clear();
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
            private readonly Stack<T> _stack = new();
            private readonly Func<T> _factory;
            private readonly Action<T> _onRent;
            private readonly Action<T> _onReturn;

            /// <summary>
            /// 【优化2：容量限制】池的最大容量
            /// </summary>
            private readonly int _maxCapacity;

            /// <summary>
            /// 【优化4：统计信息】
            /// </summary>
            private int _totalCreated;
            private int _rentCount;
            private int _returnCount;

            /// <summary>
            /// 初始化对象池实例
            /// </summary>
            /// <param name="factory">创建新对象的工厂方法</param>
            /// <param name="onRent">对象被租借时的回调函数</param>
            /// <param name="onReturn">对象被归还时的回调函数</param>
            public ObjectPool(Func<T> factory, Action<T> onRent, Action<T> onReturn, int maxCapacity)
            {
                _factory = factory;
                _onRent = onRent;
                _onReturn = onReturn;
                _maxCapacity = maxCapacity;
            }

            /// <summary>
            /// 从对象池中租借一个对象
            /// </summary>
            /// <returns>租借到的对象实例</returns>
            public T Rent()
            {
                _rentCount++;

                T instance;
                if (_stack.Count > 0)
                {
                    instance = _stack.Pop();
                }
                else
                {
                    instance = _factory();
                    _totalCreated++;
                }

                _onRent?.Invoke(instance);
                return instance;
            }

            /// <summary>
            /// 将对象归还到对象池中
            /// </summary>
            /// <param name="instance">要归还的对象实例</param>
            public void Return(T instance)
            {
                _returnCount++;
                _onReturn?.Invoke(instance);

                // 【优化2：容量限制】超过容量则不缓存，让 GC 回收
                if (_stack.Count < _maxCapacity)
                {
                    _stack.Push(instance);
                }
            }

            public PoolStatistics GetStatistics()
            {
                return new PoolStatistics
                {
                    TotalCreated = _totalCreated,
                    ActiveCount = _totalCreated - _stack.Count,
                    IdleCount = _stack.Count,
                    RentCount = _rentCount,
                    ReturnCount = _returnCount
                };
            }
        }

        /// <summary>
        /// GameObject 对象池类，用于管理 Unity 游戏对象
        /// </summary>
        private sealed class GameObjectPool
        {
            private readonly ObjectPoolManager _manager;
            private readonly GameObject _prefab;
            private readonly List<GameObject> _all = new();
            private readonly Stack<GameObject> _stack = new();
            private readonly Transform _defaultParent;
            private readonly bool _defaultWorldPositionStays;

            /// <summary>
            /// 【优化3：重置回调】对象归还时的重置回调
            /// </summary>
            private readonly Action<GameObject> _onReset;

            /// <summary>
            /// 【优化2：容量限制】池的最大容量
            /// </summary>
            private readonly int _maxCapacity;

            /// <summary>
            /// 【优化4：统计信息】
            /// </summary>
            private int _totalCreated;
            private int _rentCount;
            private int _returnCount;

            /// <summary>
            /// 初始化 GameObject 对象池实例
            /// </summary>
            /// <param name="prefab">用于创建实例的预制体</param>
            /// <param name="parent">默认的父变换</param>
            /// <param name="worldPositionStays">默认的世界位置保持设置</param>
            public GameObjectPool(ObjectPoolManager manager, GameObject prefab, Transform parent,
                                  bool worldPositionStays, Action<GameObject> onReset, int maxCapacity)
            {
                _manager = manager;
                _prefab = prefab;
                _defaultParent = parent;
                _defaultWorldPositionStays = worldPositionStays;
                _onReset = onReset;
                _maxCapacity = maxCapacity;
            }

            /// <summary>
            /// 从对象池中租借一个 GameObject 实例
            /// </summary>
            /// <param name="parent">实例化时的父变换</param>
            /// <param name="worldPositionStays">设置父变换时是否保持世界位置不变</param>
            /// <returns>租借到的 GameObject 实例</returns>
            public GameObject Rent(Transform parent, bool worldPositionStays)
            {
                _rentCount++;

                GameObject instance;
                if (_stack.Count > 0)
                {
                    instance = _stack.Pop();
                }
                else
                {
                    instance = CreateInstance(parent, worldPositionStays);
                    if (instance == null) return null;
                }

                if (parent != null && instance.transform.parent != parent)
                {
                    instance.transform.SetParent(parent, worldPositionStays);
                }

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

                _returnCount++;
                instance.SetActive(false);

                // 【优化3：重置回调】归还时重置对象状态
                _onReset?.Invoke(instance);

                instance.transform.SetParent(_defaultParent, _defaultWorldPositionStays);

                // 【优化2：容量限制】超过容量则销毁，不缓存
                if (_stack.Count < _maxCapacity)
                {
                    _stack.Push(instance);
                }
                else
                {
                    // 超过容量，销毁对象
                    _all.Remove(instance);
                    _manager._instanceToPool.Remove(instance);
                    UnityEngine.Object.Destroy(instance);
                }
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
            public PoolStatistics GetStatistics()
            {
                return new PoolStatistics
                {
                    TotalCreated = _totalCreated,
                    ActiveCount = _all.Count - _stack.Count,
                    IdleCount = _stack.Count,
                    RentCount = _rentCount,
                    ReturnCount = _returnCount
                };
            }

            private GameObject CreateInstance(Transform parent, bool worldPositionStays)
            {
                var targetParent = parent != null ? parent : _defaultParent;
                var instance = UnityEngine.Object.Instantiate(_prefab, targetParent, worldPositionStays);

                _totalCreated++;
                _all.Add(instance);

                // 【优化1：反向映射】注册实例到池的映射
                _manager._instanceToPool[instance] = this;

                return instance;
            }
        }
    }
}