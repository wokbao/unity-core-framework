namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// ISceneReadyHandlerRegistry 的默认实现。
    /// <para>
    /// 作为单例注册到 CoreLifetimeScope，所有场景级的 LifetimeScope
    /// 都可以通过 DI 获取并注册/取消注册自己的 ISceneReadyHandler。
    /// </para>
    /// </summary>
    public sealed class SceneReadyHandlerRegistry : Abstractions.ISceneReadyHandlerRegistry
    {
        private Abstractions.ISceneReadyHandler _currentHandler;

        public Abstractions.ISceneReadyHandler CurrentHandler => _currentHandler;

        public void Register(Abstractions.ISceneReadyHandler handler)
        {
            if (handler == null) return;

            _currentHandler = handler;
            UnityEngine.Debug.Log($"[SceneReadyHandlerRegistry] 已注册 Handler：{handler.GetType().Name}");
        }

        public void Unregister(Abstractions.ISceneReadyHandler handler)
        {
            if (_currentHandler == handler)
            {
                _currentHandler = null;
                UnityEngine.Debug.Log($"[SceneReadyHandlerRegistry] 已取消注册 Handler：{handler.GetType().Name}");
            }
        }
    }
}
