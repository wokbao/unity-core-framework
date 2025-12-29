using Core.Feature.Logging.Abstractions;

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
        private readonly ILogService _logService;
        private Abstractions.ISceneReadyHandler _currentHandler;

        public Abstractions.ISceneReadyHandler CurrentHandler => _currentHandler;

        public SceneReadyHandlerRegistry(ILogService logService)
        {
            _logService = logService;
        }

        public void Register(Abstractions.ISceneReadyHandler handler)
        {
            if (handler == null) return;

            _currentHandler = handler;
            _logService?.Debug(LogCategory.Core, $"已注册 SceneReadyHandler：{handler.GetType().Name}");
        }

        public void Unregister(Abstractions.ISceneReadyHandler handler)
        {
            if (_currentHandler == handler)
            {
                _currentHandler = null;
                _logService?.Debug(LogCategory.Core, $"已取消注册 SceneReadyHandler：{handler.GetType().Name}");
            }
        }
    }
}
