using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.Runtime;
using Core.Feature.Logging.ScriptableObjects;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Core.Bootstrap
{
    //作为 Core_Bootstrap 常驻根 Scope，负责事件/日志/网络/配置等基础设施并为其他 Scope 提供父容器。
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private LoggingConfig loggingConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterLogging(builder);

            // TODO: 其他 Core 系统注册
        }

        private void RegisterLogging(IContainerBuilder builder)
        {
            // 兜底：Inspector 没拖的话，从 Resources 里找
            if (loggingConfig == null)
            {
                loggingConfig = Resources.Load<LoggingConfig>("LoggingConfig");

                if (loggingConfig == null)
                {
                    Debug.LogWarning("未找到 LoggingConfig，日志系统将使用默认配置。");
                }
            }

            if (loggingConfig != null)
            {
                builder.RegisterInstance(loggingConfig);
            }

            // 根据配置启用 Unity 控制台输出
            if (loggingConfig == null || loggingConfig.enableUnityConsoleOutput)
            {
                builder.Register<UnityLogSink>(Lifetime.Singleton)
                    .As<ILogSink>();
            }

            // TODO：如果以后有 FileLogSink，就在这里判断 enableFileOutput 再注册

            builder.Register<LogService>(Lifetime.Singleton)
                .As<ILogService>();
        }
    }
}
