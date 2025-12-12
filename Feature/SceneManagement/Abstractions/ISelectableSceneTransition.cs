namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 可被选择的场景过渡实现，用于将具体方案与配置模式关联。
    /// </summary>
    public interface ISelectableSceneTransition : ISceneTransition
    {
        SceneTransitionMode Mode { get; }
    }
}
