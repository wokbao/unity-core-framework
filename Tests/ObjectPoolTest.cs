using Core.Feature.ObjectPooling.Abstractions;
using UnityEngine;

namespace Core.Tests
{
    /// <summary>
    /// 用于验证 IObjectPoolManager 是否正确注入并基本可用。
    /// 挂到场景后查看 Console 输出。
    /// </summary>
    public class ObjectPoolTest : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;

        [Inject]
        private IObjectPoolManager _poolManager;

        private void Start()
        {
            if (_poolManager == null)
            {
                Debug.LogError("❌ IObjectPoolManager 注入失败！");
                return;
            }

            Debug.Log("✅ IObjectPoolManager 注入成功，开始简单租借/归还测试。");

            if (_prefab != null)
            {
                var go = _poolManager.Rent(_prefab);
                Debug.Log($"租借到实例: {go.name}");
                _poolManager.Return(go);
                Debug.Log("实例已归还到池。");
            }
            else
            {
                var value = _poolManager.Rent(() => new int[4]);
                Debug.Log($"租借到数组长度: {value.Length}");
                _poolManager.Return(value);
            }
        }
    }
}
