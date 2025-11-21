using VContainer;

namespace Core.Runtime
{
    /// <summary>
    /// 模块化注册基类，挂在同一场景物体上，由 CoreBootstrap 按顺序调用。
    /// </summary>
    public abstract class LifetimeScopeRegistrar : UnityEngine.MonoBehaviour
    {
        public abstract void Register(IContainerBuilder builder);
    }
}
