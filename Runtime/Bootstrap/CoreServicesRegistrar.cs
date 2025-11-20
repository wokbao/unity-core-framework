using VContainer;

namespace FoodStreet.Core.Bootstrap
{
    /// <summary>
    /// Core 层服务注册（时间、事件、日志等），挂到 CoreBootstrap 物体上。
    /// </summary>
    public sealed class CoreServicesRegistrar : LifetimeScopeRegistrar
    {
        public override void Register(IContainerBuilder builder)
        {
            // builder.Register<TimeService>(Lifetime.Singleton);
            // builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            // builder.Register<ILoggingService, LoggingService>(Lifetime.Singleton);
        }
    }
}
