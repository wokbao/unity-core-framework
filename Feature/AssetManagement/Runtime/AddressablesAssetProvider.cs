using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Feature.AssetManagement.Runtime
{
    /// <summary>
    /// 统一的 Addressables 访问入口，负责加载、缓存与释放。
    /// </summary>
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        // key -> Addressables 句柄缓存，便于重复加载时直接复用
        private readonly Dictionary<string, AsyncOperationHandle> handleCache = new();

        // 记录通过 Addressables 动态实例化的对象，便于统一释放
        private readonly HashSet<GameObject> spawnedInstances = new();

        // 异步加载资源（统一 entry point）：
        // - 先检查缓存：如果之前有人加载过，就沿用同一个句柄，避免重复发起 IO；
        // - 如果没有缓存：正式调用 Addressables.LoadAssetAsync，并把句柄存起来；
        // - 过程中要响应取消：一旦外界取消，就释放句柄、移除缓存，防止残留；
        // - 任意异常/失败也要清理缓存，避免之后拿到一个“坏句柄”。
        public async UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (handleCache.TryGetValue(key, out var cachedHandle))
            {
                return await AwaitCachedHandle<T>(key, cachedHandle, ct);
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            handleCache[key] = handle;
            var registration = RegisterCancellation(key, handle, ct);

            try
            {
                return await handle.Task;
            }
            catch
            {
                handleCache.Remove(key);
                throw;
            }
            finally
            {
                registration.Dispose();

                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    handleCache.Remove(key);
                }
            }
        }

        // 同步加载版本与异步逻辑保持一致：
        // - 如果已经有句柄，就直接 WaitForCompletion；
        // - 否则新建句柄并阻塞等待；
        // - 失败时必须移除缓存，下一次会重新加载。
        public T LoadAssetSync<T>(string key)
        {
            if (handleCache.TryGetValue(key, out var cachedHandle))
            {
                return CompleteCachedHandle<T>(key, cachedHandle);
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            handleCache[key] = handle;

            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                handleCache.Remove(key);
                return default;
            }

            return handle.Result;
        }

        // 预加载（常用于场景过场/缓存热点资源）：
        // - 逐个 key 触发 LoadAssetAsync；
        // - 遇到取消马上结束；
        // - 如果某个资源已经缓存就跳过，防止重复下载。
        public async UniTask PreloadAsync(IEnumerable<string> keys, CancellationToken ct = default)
        {
            foreach (var key in keys)
            {
                if (ct.IsCancellationRequested) break;
                if (handleCache.ContainsKey(key)) continue;
                await LoadAssetAsync<object>(key, ct);
            }
        }

        // Addressables 实例化 prefab（类似 Resources.Instantiate，但由 Addressables 管理内存）：
        // - 统一记录所有实例，方便 Clear/ReleaseInstance 时回收；
        // - 如果调用者传入取消，释放尚未完成的句柄，防止实例半途而废。
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldSpace = false,
            CancellationToken ct = default)
        {
            // Addressables.InstantiateAsync 自带引用计数，这里只负责记录实例用于释放
            var handle = Addressables.InstantiateAsync(key, parent, worldSpace);

            if (ct.CanBeCanceled)
            {
                using (ct.Register(() => Addressables.Release(handle)))
                {
                    var instance = await handle.Task;
                    spawnedInstances.Add(instance);
                    return instance;
                }
            }

            var go = await handle.Task;
            spawnedInstances.Add(go);
            return go;
        }

        // 主动释放缓存句柄：适合加载-使用-卸载的生命周期，释放后下一次会重新加载。
        public void Release(string key)
        {
            if (!handleCache.TryGetValue(key, out var handle))
            {
                return;
            }

            Addressables.Release(handle);
            handleCache.Remove(key);
        }

        // 释放实例：如果我们有记录，按正常路径 ReleaseInstance；
        // 如果没有（比如外部传入），也尝试释放，以免出现泄漏。
        public void ReleaseInstance(GameObject instance)
        {
            // 对实例释放做容错：若实例来自外部也尝试调用 Addressables 释放
            if (instance == null)
            {
                return;
            }

            if (spawnedInstances.Remove(instance))
            {
                Addressables.ReleaseInstance(instance);
            }
            else
            {
                // fallback：如果不在记录里，也尝试释放
                Addressables.ReleaseInstance(instance);
            }
        }

        // 一键清理：释放所有缓存句柄和所有实例，常用于场景退出 / 关卡切换。
        public void Clear()
        {
            foreach (var kv in handleCache)
            {
                Addressables.Release(kv.Value);
            }

            handleCache.Clear();

            foreach (var instance in spawnedInstances)
            {
                Addressables.ReleaseInstance(instance);
            }

            spawnedInstances.Clear();
        }

        // 给“正在加载”的句柄挂取消回调：
        // - 只有 Token 可取消时才注册；
        // - 如果加载已经结束（IsDone）就不做额外操作；
        // - 取消发生时，释放句柄并从缓存移除。
        private CancellationTokenRegistration RegisterCancellation<T>(string key,
            AsyncOperationHandle<T> handle, CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                return default;
            }

            return ct.Register(() =>
            {
                if (handle.IsDone)
                {
                    return;
                }

                Addressables.Release(handle);
                handleCache.Remove(key);
            });
        }

        // 复用缓存句柄时同样需要 await：
        // - 可靠地等到结果返回（无论谁发起的）；
        // - 如果过程中出现失败，也要顺手把缓存清掉。
        private async UniTask<T> AwaitCachedHandle<T>(string key, AsyncOperationHandle cachedHandle,
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            try
            {
                var result = await cachedHandle.Task;
                return (T)result;
            }
            finally
            {
                if (cachedHandle.Status == AsyncOperationStatus.Failed)
                {
                    handleCache.Remove(key);
                }
            }
        }

        // 同步等待缓存句柄完成：逻辑与异步版一致，只是改成 WaitForCompletion。
        private T CompleteCachedHandle<T>(string key, AsyncOperationHandle cachedHandle)
        {
            cachedHandle.WaitForCompletion();

            if (cachedHandle.Status == AsyncOperationStatus.Failed)
            {
                handleCache.Remove(key);
                return default;
            }

            return (T)cachedHandle.Result;
        }
    }
}
