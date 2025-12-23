using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Core.Feature.AssetManagement.Runtime
{
    /// <summary>
    /// 抽象资源加载接口，封装 Addressables，便于后续替换为自研或远程方案。
    /// </summary>
    public interface IAssetProvider
    {
        UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default);

        T LoadAssetSync<T>(string key);

        UniTask PreloadAsync(IEnumerable<string> keys, CancellationToken ct = default);

        UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldSpace = false, CancellationToken ct = default);

        void Release(string key);

        void ReleaseInstance(GameObject instance);

        void Clear();

        UniTask<SceneInstance> LoadSceneAsync(string key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, CancellationToken ct = default);

        UniTask UnloadSceneAsync(SceneInstance scene, CancellationToken ct = default);
    }
}
