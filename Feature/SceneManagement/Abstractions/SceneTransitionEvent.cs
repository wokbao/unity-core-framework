namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景切换阶段的简单事件载体，方便 UI/音频订阅。
    /// </summary>
    public readonly struct SceneTransitionEvent
    {
        public string FromScene { get; }
        public string ToScene { get; }

        public SceneTransitionEvent(string fromScene, string toScene)
        {
            FromScene = fromScene;
            ToScene = toScene;
        }
    }
}
