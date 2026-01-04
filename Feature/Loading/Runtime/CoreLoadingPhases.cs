namespace Core.Feature.Loading.Runtime
{
    /// <summary>
    /// 定义加载流程中的标准阶段名称常量。
    /// </summary>
    public static class CoreLoadingPhases
    {
        /// <summary>
        /// 卸载当前场景阶段
        /// </summary>
        public const string Phase_Unload = "Loading_Phase_Unload_Scene";

        /// <summary>
        /// 播放转场动画（淡出）阶段
        /// </summary>
        public const string Phase_Transition_Out = "Loading_Phase_Transition_Out";

        /// <summary>
        /// 加载场景资源阶段
        /// </summary>
        public const string Phase_Load_Asset = "Loading_Phase_Load_Asset";

        /// <summary>
        /// 等待场景就绪（初始化）阶段
        /// </summary>
        public const string Phase_Wait_Ready = "Loading_Phase_Wait_Ready";
    }
}
