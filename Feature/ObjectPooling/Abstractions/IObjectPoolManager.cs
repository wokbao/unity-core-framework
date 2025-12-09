using System;
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
        T Rent<T>(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null);

        /// <summary>
        /// 归还一个普通对象到池中。
        /// </summary>
        void Return<T>(T instance);

        /// <summary>
        /// 租借一个 GameObject 实例，若池中为空则实例化 prefab。
        /// </summary>
        GameObject Rent(GameObject prefab, Transform parent = null, bool worldPositionStays = false);

        /// <summary>
        /// 归还 GameObject 到池中并禁用。
        /// </summary>
        void Return(GameObject instance);

        /// <summary>
        /// 预热某个 prefab 的对象池，预先创建指定数量的实例。
        /// </summary>
        void WarmUp(GameObject prefab, int count, Transform parent = null, bool worldPositionStays = false);

        /// <summary>
        /// 清理池中缓存的实例。
        /// </summary>
        void Clear(bool destroyGameObjects = true);
    }
}
