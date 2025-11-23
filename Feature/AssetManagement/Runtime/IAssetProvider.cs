using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Feature.AssetManagement.Runtime
{
    /// <summary>
    /// 抽象资源加载接口，封装 Addressables，便于后续替换为自研或远程方案。
    /// </summary>
    public interface IAssetProvider
    {
        Task<T> LoadAssetAsync<T>(string key, CancellationToken ct = default);

        T LoadAssetSync<T>(string key);

        Task PreloadAsync(IEnumerable<string> keys, CancellationToken ct = default);

        Task<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldSpace = false, CancellationToken ct = default);

        void Release(string key);

        void ReleaseInstance(GameObject instance);

        void Clear();
    }
}
