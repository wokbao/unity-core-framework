namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景就绪处理器注册接口。
    /// <para>
    /// 场景级组件（如 MenuLifetimeScope）通过此接口注册自己为 ISceneReadyHandler，
    /// SceneService 在场景加载完成后会查询当前注册的 handler 并等待其就绪。
    /// </para>
    /// <para>
    /// 这是一个替代静态字段的 DI 友好方案，便于测试和避免状态污染。
    /// </para>
    /// </summary>
    public interface ISceneReadyHandlerRegistry
    {
        /// <summary>
        /// 当前注册的场景就绪处理器（可能为 null）。
        /// </summary>
        ISceneReadyHandler CurrentHandler { get; }

        /// <summary>
        /// 注册当前场景的就绪处理器。
        /// 通常在场景 LifetimeScope 的 Awake 中调用。
        /// </summary>
        /// <param name="handler">要注册的处理器</param>
        void Register(ISceneReadyHandler handler);

        /// <summary>
        /// 取消注册当前场景的就绪处理器。
        /// 通常在场景 LifetimeScope 的 OnDestroy 中调用。
        /// </summary>
        /// <param name="handler">要取消注册的处理器</param>
        void Unregister(ISceneReadyHandler handler);
    }
}
