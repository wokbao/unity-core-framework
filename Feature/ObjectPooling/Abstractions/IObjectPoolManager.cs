using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Feature.ObjectPooling.Abstractions
{
    /// <summary>
    /// 对象池管理器接口，支持普通对象与 GameObject 的租借与归还。
    /// </summary>
    public interface IObjectPoolManager
    {
        /// <summary>
        /// 租借一个普通对象，若池中为空则使用 factory 创建。
        /// </summary>
        /// <param name="maxCapacity">池的最大容量（默认 100）</param>
        T Rent<T>(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null, int maxCapacity = 100);

        /// <summary>
        /// 归还一个普通对象到池中。
        /// </summary>
        void Return<T>(T instance);

        /// <summary>
        /// 租借一个 GameObject 实例，若池中为空则实例化 prefab。
        /// </summary>
        /// <param name="onReset">对象归还时的重置回调（可选）</param>
        /// <param name="maxCapacity">池的最大容量（默认 50）</param>
        GameObject Rent(GameObject prefab, Transform parent = null, bool worldPositionStays = false, 
                        Action<GameObject> onReset = null, int maxCapacity = 50);

        /// <summary>
        /// 异步租借 GameObject（从 Addressables 加载）
        /// </summary>
        UniTask<GameObject> RentAsync(string addressableKey, Transform parent = null, 
                                       bool worldPositionStays = false, 
                                       Action<GameObject> onReset = null, 
                                       int maxCapacity = 50, 
                                       System.Threading.CancellationToken ct = default);

        /// <summary>
        /// 归还 GameObject 到池中并禁用。
        /// </summary>
        void Return(GameObject instance);

        /// <summary>
        /// 预热某个 prefab 的对象池，预先创建指定数量的实例。
        /// </summary>
        void WarmUp(GameObject prefab, int count, Transform parent = null, bool worldPositionStays = false);

        /// <summary>
        /// 获取 GameObject 对象池的统计信息
        /// </summary>
        PoolStatistics GetStatistics(GameObject prefab);

        /// <summary>
        /// 获取普通对象池的统计信息
        /// </summary>
        PoolStatistics GetStatistics<T>();

        /// <summary>
        /// 清理池中缓存的实例。
        /// </summary>
        void Clear(bool destroyGameObjects = true);
    }
}
