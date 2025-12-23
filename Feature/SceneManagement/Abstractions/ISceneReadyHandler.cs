using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景就绪处理器接口。
    /// <para>
    /// 实现此接口的 MonoBehaviour（通常是场景入口点或 UI 加载器）可以控制转场动画的结束时机。
    /// SceneService 会在加载场景后查找此接口，并等待 WaitForSceneReadyAsync 返回后再淡入屏幕。
    /// </para>
    /// </summary>
    public interface ISceneReadyHandler
    {
        /// <summary>
        /// 等待场景视觉就绪。
        /// 在此任务完成前，场景遮罩（Loading Screen）不会移除。
        /// </summary>
        /// <returns></returns>
        UniTask WaitForSceneReadyAsync();
    }
}
