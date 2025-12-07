using System;
using UnityEngine;
using VContainer;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 配置注册器
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>将加载的配置注册到 DI 容器</item>
    ///   <item>使配置可通过构造函数注入到服务中</item>
    ///   <item>使用反射实现类型安全的动态注册</item>
    /// </list>
    /// 
    /// <para><b>设计原则</b>：</para>
    /// <list type="bullet">
    ///   <item>配置作为依赖项可被服务注入</item>
    ///   <item>每个配置类型只注册一次（单例模式）</item>
    ///   <item>支持任意数量的配置类型，无需修改代码</item>
    /// </list>
    /// 
    /// <para><b>使用示例</b>：</para>
    /// <code>
    /// // 在 CoreLifetimeScope.Configure() 中
    /// 
    /// // 1. 加载配置
    /// var loadResult = ConfigLoader.LoadFromManifest(_configManifest);
    /// 
    /// // 2. 注册到容器
    /// ConfigRegistry.RegisterToContainer(builder, loadResult);
    /// 
    /// // 3. 现在服务可以注入配置了
    /// builder.Register&lt;LogService&gt;(Lifetime.Singleton).As&lt;ILogService&gt;();
    /// //  ↑ LogService 的构造函数可以接收 LoggingConfig
    /// 
    /// // 服务端使用
    /// public class LogService : ILogService
    /// {
    ///     public LogService(LoggingConfig config) // 自动注入
    ///     {
    ///         _minimumLevel = config.minimumLogLevel;
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><b>工作原理</b>：</para>
    /// <list type="number">
    ///   <item>遍历 ConfigLoadResult 中的所有配置</item>
    ///   <item>获取每个配置的运行时类型</item>
    ///   <item>使用反射调用 RegisterInstance&lt;T&gt; 方法</item>
    ///   <item>将配置实例注册为该类型的单例</item>
    /// </list>
    /// 
    /// <para><b>注册策略</b>：</para>
    /// <list type="bullet">
    ///   <item>配置按具体类型注册（如 LoggingConfig）</item>
    ///   <item>不注册为 ScriptableObject 基类（避免歧义）</item>
    ///   <item>每个配置类型全局唯一（单例）</item>
    /// </list>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>此类是静态类，不持有状态</item>
    ///   <item>只能在 LifetimeScope.Configure() 阶段调用</item>
    ///   <item>同一配置多次注册会抛出异常（VContainer 行为）</item>
    ///   <item>配置必须在服务注册之前注册</item>
    /// </list>
    /// </summary>
    public static class ConfigRegistry
    {
        /// <summary>
        /// 将配置加载结果注册到 DI 容器
        /// </summary>
        /// <param name="builder">VContainer 容器构建器</param>
        /// <param name="loadResult">配置加载结果，包含所有成功加载的配置</param>
        public static void RegisterToContainer(IContainerBuilder builder, ConfigLoadResult loadResult)
        {
            if (builder == null)
            {
                Debug.LogError("[ConfigRegistry] ContainerBuilder 为 null，无法注册配置");
                return;
            }

            if (loadResult == null || loadResult.LoadedConfigs.Count == 0)
            {
                Debug.LogWarning("[ConfigRegistry] 配置加载结果为空，跳过注册");
                return;
            }

            Debug.Log($"[ConfigRegistry] 开始注册 {loadResult.LoadedConfigs.Count} 个配置到容器...");

            var successCount = 0;
            var failCount = 0;

            foreach (var kvp in loadResult.LoadedConfigs)
            {
                try
                {
                    RegisterConfig(builder, kvp.Key, kvp.Value);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConfigRegistry] 注册配置失败: {kvp.Key} - {ex.Message}");
                    failCount++;
                }
            }

            Debug.Log($"[ConfigRegistry] 配置注册完成 - 成功: {successCount}, 失败: {failCount}");
        }

        /// <summary>
        /// 注册单个配置到容器
        /// </summary>
        private static void RegisterConfig(IContainerBuilder builder, string configName, object config)
        {
            if (config == null)
            {
                Debug.LogWarning($"[ConfigRegistry] 配置实例为 null: {configName}");
                return;
            }

            var configType = config.GetType();

            try
            {
                // 使用反射调用泛型辅助方法
                var method = typeof(ConfigRegistry).GetMethod(
                    nameof(RegisterConfigGeneric),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                );

                if (method == null)
                {
                    throw new Exception("无法找到 RegisterConfigGeneric 方法");
                }

                var genericMethod = method.MakeGenericMethod(configType);
                genericMethod.Invoke(null, new object[] { builder, config, configName });

                Debug.Log($"[ConfigRegistry] ✓ 注册配置: {configName} ({configType.Name})");
            }
            catch (Exception ex)
            {
                throw new Exception($"注册配置 {configName} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 泛型辅助方法：注册配置实例到容器
        /// </summary>
        private static void RegisterConfigGeneric<T>(IContainerBuilder builder, object config, string configName) where T : ScriptableObject
        {
            // 转换为具体类型
            var typedConfig = config as T;
            if (typedConfig == null)
            {
                throw new Exception($"配置 {configName} 无法转换为类型 {typeof(T).Name}");
            }

            // 使用泛型扩展方法注册
            builder.RegisterInstance(typedConfig);
        }
    }
}
