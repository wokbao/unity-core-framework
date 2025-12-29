using Core.Feature.SceneManagement.Abstractions;
using UnityEngine;
using VContainer;

namespace Core.Tests
{
    /// <summary>
    /// 场景管理服务测试脚本
    /// 用于验证 ISceneService 是否正确注入并能正常工作
    /// </summary>
    public class SceneServiceTest : MonoBehaviour
    {
        [Inject]
        private ISceneService _sceneService;

        private void Start()
        {
            if (_sceneService != null)
            {
                Debug.Log($"✅ ISceneService 注入成功！当前场景: {_sceneService.CurrentSceneKey}");

                // 可以在这里测试加载其他场景
                // await _sceneService.LoadSceneAsync("YourSceneName");
            }
            else
            {
                Debug.LogError("❌ ISceneService 注入失败！");
            }
        }
    }
}
