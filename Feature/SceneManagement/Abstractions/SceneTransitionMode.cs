namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 标识可选的场景过渡方案，便于配置和可插拔扩展。
    /// </summary>
    public enum SceneTransitionMode
    {
        Cinematic = 0,
        Fade = 1,
        Shutter = 2,
        Noise = 3,
        None = 99
    }
}
