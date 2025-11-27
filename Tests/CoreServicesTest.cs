using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Abstractions;
using UnityEngine;
using VContainer;

namespace Core.Tests
{
    /// <summary>
    /// 简单的测试组件，用于验证 Core 层服务的依赖注入是否正常工作
    /// </summary>
    public class CoreServicesTest : MonoBehaviour
    {
        [Inject]
        private ILogService _logService;

        [Inject]
        private IAssetProvider _assetProvider;

        private void Start()
        {
            // 测试日志服务
            if (_logService != null)
            {
                _logService.Information(
                    LogCategory.System,
                    "✅ CoreServicesTest: ILogService 注入成功！");

                _logService.Debug(
                    LogCategory.System,
                    $"✅ IAssetProvider 注入成功：{_assetProvider != null}");
            }
            else
            {
                Debug.LogError("❌ ILogService 注入失败！");
            }

            // 测试资源提供器
            if (_assetProvider != null)
            {
                Debug.Log("✅ IAssetProvider 注入成功！");
            }
            else
            {
                Debug.LogError("❌ IAssetProvider 注入失败！");
            }
        }
    }
}
