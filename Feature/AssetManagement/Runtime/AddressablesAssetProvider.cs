using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Core.Feature.AssetManagement.Runtime
{
    /// <summary>
    /// 统一的 Addressables 访问入口，负责加载、缓存与释放。
    /// </summary>
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle> handleCache = new();
        private readonly HashSet<GameObject> spawnedInstances = new();

        public async Task<T> LoadAssetAsync<T>(string key, CancellationToken ct = default)
        {
            if (handleCache.TryGetValue(key, out var cachedHandle))
            {
                return (T)cachedHandle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            handleCache[key] = handle;

            try
            {
                if (ct.CanBeCanceled)
                {
                    using (ct.Register(() => Addressables.Release(handle)))
                    {
                        return await handle.Task.ConfigureAwait(false);
                    }
                }

                return await handle.Task.ConfigureAwait(false);
            }
            finally
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    handleCache.Remove(key);
                }
            }
        }

        public T LoadAssetSync<T>(string key)
        {
            if (handleCache.TryGetValue(key, out var cachedHandle))
            {
                return (T)cachedHandle.Result;
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

        public async Task PreloadAsync(IEnumerable<string> keys, CancellationToken ct = default)
        {
            foreach (var key in keys)
            {
                if (ct.IsCancellationRequested) break;
                if (handleCache.ContainsKey(key)) continue;
                await LoadAssetAsync<object>(key, ct).ConfigureAwait(false);
            }
        }

        public async Task<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldSpace = false,
            CancellationToken ct = default)
        {
            var handle = Addressables.InstantiateAsync(key, parent, worldSpace);

            if (ct.CanBeCanceled)
            {
                using (ct.Register(() => Addressables.Release(handle)))
                {
                    var instance = await handle.Task.ConfigureAwait(false);
                    spawnedInstances.Add(instance);
                    return instance;
                }
            }

            var go = await handle.Task.ConfigureAwait(false);
            spawnedInstances.Add(go);
            return go;
        }

        public void Release(string key)
        {
            if (!handleCache.TryGetValue(key, out var handle))
            {
                return;
            }

            Addressables.Release(handle);
            handleCache.Remove(key);
        }

        public void ReleaseInstance(GameObject instance)
        {
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
    }
}
