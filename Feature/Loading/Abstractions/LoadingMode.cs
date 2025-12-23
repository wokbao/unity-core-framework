namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 定义加载操作的模式
    /// </summary>
    public enum LoadingMode
    {
        /// <summary>
        /// 前台模式（默认）：会触发全局 Loading HUD，阻断用户操作。
        /// 适用于：场景切换、登录、关键数据加载。
        /// </summary>
        Foreground = 0,

        /// <summary>
        /// 后台模式：静默执行，不显示全局 Loading HUD，不阻断用户操作。
        /// 适用于：预加载资源、自动保存、后台同步。
        /// </summary>
        Background = 1
    }
}
