using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Runtime
{
    /// <summary>
    /// Core_Bootstrap 的根 Scope，标记为常驻场景，注册全局服务。
    /// </summary>
    public sealed class CoreBootstrapLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private LifetimeScopeRegistrar[] registrars = System.Array.Empty<LifetimeScopeRegistrar>();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // 通过模块化 Registrar 拆分注册，便于维护。
            foreach (var registrar in registrars)
            {
                if (registrar == null)
                {
                    continue;
                }

                registrar.Register(builder);
            }
        }
    }
}
