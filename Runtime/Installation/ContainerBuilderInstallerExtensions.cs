using System;
using VContainer;
using VContainer.Unity;

namespace Core.Runtime.Installation
{
    /// <summary>
    /// 辅助在 Configure 阶段执行安装器并将其实例注册到容器。
    /// </summary>
    public static class ContainerBuilderInstallerExtensions
    {
        /// <summary>
        /// 通过工厂创建安装器、执行 Install，然后将实例注册为单例。
        /// </summary>
        public static TInstaller RegisterInstaller<TInstaller>(this IContainerBuilder builder, Func<TInstaller> factory)
            where TInstaller : IInstaller
        {
            var installer = factory();
            installer.Install(builder);
            builder.RegisterInstance(installer).As<IInstaller>();
            return installer;
        }
    }
}
