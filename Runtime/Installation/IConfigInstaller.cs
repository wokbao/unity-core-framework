

using VContainer.Unity;

namespace Core.Runtime.Installation
{
    /// <summary>
    /// 可加载配置并完成依赖注册的安装器约束。
    /// </summary>
    public interface IConfigInstaller<TConfig> : IInstaller
    {
        TConfig Config { get; }
    }

    /// <summary>
    /// 带 Options 的安装器约束，便于对配置选项进行类型约束。
    /// </summary>
    public interface IConfigInstaller<TConfig, TOptions> : IConfigInstaller<TConfig>
    {
        TOptions Options { get; }
    }
}
