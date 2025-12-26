using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 通用配置加载器
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>根据配置清单批量加载所有配置</item>
    ///   <item>处理配置加载失败时的降级逻辑</item>
    ///   <item>执行配置验证（如果配置实现了 IValidatableConfig）</item>
    ///   <item>提供详细的加载日志和统计信息</item>
    /// </list>
    /// 
    /// <para><b>设计特点</b>：</para>
    /// <list type="bullet">
    ///   <item>基于清单的自动化加载（声明式，无需修改代码）</item>
    ///   <item>支持优先级控制（确保依赖关系正确）</item>
    ///   <item>支持环境隔离（开发/生产环境）</item>
    ///   <item>失败降级到默认配置（保证启动不被阻塞）</item>
    ///   <item>使用反射实现类型安全的泛型加载</item>
    /// </list>
    /// 
    /// <para><b>使用示例</b>：</para>
    /// <code>
    /// // 在 CoreLifetimeScope.Configure() 中
    /// var result = ConfigLoader.LoadFromManifest(_configManifest);
    /// 
    /// // 检查加载结果
    /// if (result.FailedConfigs.Count > 0)
    /// {
    ///     Debug.LogWarning($"部分配置加载失败: {string.Join(", ", result.FailedConfigs)}");
    /// }
    /// 
    /// // 注册到容器
    /// ConfigRegistry.RegisterToContainer(builder, result);
    /// </code>
    /// 
    /// <para><b>加载流程</b>：</para>
    /// <list type="number">
    ///   <item>读取配置清单，过滤环境不匹配的配置</item>
    ///   <item>按优先级排序（Priority 值越小越优先）</item>
    ///   <item>逐个加载配置（使用反射调用泛型方法）</item>
    ///   <item>对每个配置执行验证（如果启用且配置实现了 IValidatableConfig）</item>
    ///   <item>记录成功/失败统计，输出加载摘要</item>
    /// </list>
    /// 
    /// <para><b>错误处理策略</b>：</para>
    /// <list type="bullet">
    ///   <item>类型解析失败：记录错误，跳过该配置</item>
    ///   <item>Addressables 加载失败：尝试创建默认配置</item>
    ///   <item>默认配置创建失败：记录错误，返回 null</item>
    ///   <item>验证失败：记录警告，但不阻止使用（配置已加载）</item>
    /// </list>
    /// 
    /// <para><b>性能考虑</b>：</para>
    /// <list type="bullet">
    ///   <item>所有加载都是同步的（使用 WaitForCompletion）</item>
    ///   <item>适合启动时一次性加载少量配置（5-10个）</item>
    ///   <item>不适合运行时频繁调用或加载大量配置</item>
    /// </list>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>ConfigTypeName 必须是完整的类型名（含命名空间）</item>
    ///   <item>配置类必须继承自 ScriptableObject</item>
    ///   <item>加载顺序由 Priority 决定，相同 Priority 的顺序不保证</item>
    ///   <item>此加载器是静态类，不持有状态，线程安全</item>
    /// </list>
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 根据清单加载所有配置
        /// </summary>
        /// <param name="manifest">配置清单，包含所有需要加载的配置条目</param>
        /// <returns>配置加载结果，包含成功和失败的配置信息</returns>
        public static ConfigLoadResult LoadFromManifest(IConfigManifest manifest)
        {
            var result = new ConfigLoadResult();

            if (manifest == null || manifest.Entries.Count == 0)
            {
                Debug.LogWarning("[ConfigLoader] 配置清单为空，跳过配置加载");
                return result;
            }

            // 过滤并按优先级排序
            var sortedEntries = manifest.Entries
                .Where(ShouldLoadEntry)
                .OrderBy(e => e.Priority)
                .ToList();

            Debug.Log($"[ConfigLoader] 开始加载 {sortedEntries.Count} 个配置...");

            // 批量加载
            foreach (var entry in sortedEntries)
            {
                var enableValidation = GetEnableValidation(manifest);
                LoadConfigEntry(entry, result, enableValidation);
            }

            // 汇总报告
            LogLoadingSummary(result);

            return result;
        }

        /// <summary>
        /// 异步版本：根据清单加载所有配置
        /// </summary>
        /// <param name="manifest">配置清单，包含所有需要加载的配置条目</param>
        /// <param name="loadingService">可选：加载服务，用于报告进度</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>配置加载结果，包含成功和失败的配置信息</returns>
        public static async Cysharp.Threading.Tasks.UniTask<ConfigLoadResult> LoadFromManifestAsync(
            IConfigManifest manifest,
            Core.Feature.Loading.Abstractions.ILoadingService loadingService = null,
            System.Threading.CancellationToken ct = default)
        {
            var result = new ConfigLoadResult();

            if (manifest == null || manifest.Entries.Count == 0)
            {
                Debug.LogWarning("[ConfigLoader] 配置清单为空，跳过配置加载");
                return result;
            }

            // 过滤并按优先级排序
            var sortedEntries = manifest.Entries
                .Where(ShouldLoadEntry)
                .OrderBy(e => e.Priority)
                .ToList();

            Debug.Log($"[ConfigLoader] 开始异步加载 {sortedEntries.Count} 个配置...");

            // ⚠️ 关键：调用 Begin() 来触发 LoadingService.IsLoading = true
            // 这会让 LoadingHud 显示出来
            using var loadingScope = loadingService?.Begin("加载配置");

            // 异步批量加载
            var enableValidation = GetEnableValidation(manifest);
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];

                // 报告细粒度进度
                var progress = (float)(i + 1) / sortedEntries.Count;
                var desc = $"加载配置 {entry.Name} ({i + 1}/{sortedEntries.Count})";
                loadingService?.ReportProgress(progress, desc);

                // 异步加载单个配置
                await LoadConfigEntryAsync(entry, result, enableValidation, ct);
            }

            // 汇总报告
            LogLoadingSummary(result);

            return result;
        }

        /// <summary>
        /// 加载单个配置条目
        /// </summary>
        private static void LoadConfigEntry(
            ConfigManifest.ConfigEntry entry,
            ConfigLoadResult result,
            bool enableValidation)
        {
            try
            {
                // 解析类型
                var configType = Type.GetType(entry.ConfigTypeName);
                if (configType == null)
                {
                    throw new Exception($"无法找到类型: {entry.ConfigTypeName}");
                }

                // 检查是否继承自 ScriptableObject
                if (!typeof(ScriptableObject).IsAssignableFrom(configType))
                {
                    throw new Exception($"配置类型 {entry.ConfigTypeName} 必须继承自 ScriptableObject");
                }

                // 使用反射调用泛型方法
                var method = typeof(ConfigLoader).GetMethod(
                    nameof(LoadConfigGeneric),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                );
                var genericMethod = method.MakeGenericMethod(configType);

                var config = genericMethod.Invoke(null, new object[] { entry, enableValidation });

                if (config != null)
                {
                    result.LoadedConfigs[entry.Name] = config;
                    Debug.Log($"[ConfigLoader] ✓ 成功加载: {entry.Name} ({configType.Name})");
                }
                else
                {
                    throw new Exception("加载返回 null");
                }
            }
            catch (Exception ex)
            {
                result.FailedConfigs.Add(entry.Name);

                var logLevel = entry.IsRequired ? LogType.Error : LogType.Warning;
                Debug.LogFormat(logLevel, LogOption.NoStacktrace, null,
                    "[ConfigLoader] ✗ 加载失败: {0} - {1}", entry.Name, ex.Message);
            }
        }

        /// <summary>
        /// 异步加载单个配置条目
        /// </summary>
        private static async Cysharp.Threading.Tasks.UniTask LoadConfigEntryAsync(
            ConfigManifest.ConfigEntry entry,
            ConfigLoadResult result,
            bool enableValidation,
            System.Threading.CancellationToken ct)
        {
            try
            {
                // 解析类型
                var configType = Type.GetType(entry.ConfigTypeName);
                if (configType == null)
                {
                    throw new Exception($"无法找到类型: {entry.ConfigTypeName}");
                }

                // 检查是否继承自 ScriptableObject
                if (!typeof(ScriptableObject).IsAssignableFrom(configType))
                {
                    throw new Exception($"配置类型 {entry.ConfigTypeName} 必须继承自 ScriptableObject");
                }

                // ⚠️ 规范豁免：ConfigLoader 在 DI 容器构建前运行，允许直接调用 Addressables
                // 业务代码必须使用 IAssetProvider 接口
                var handle = Addressables.LoadAssetAsync<ScriptableObject>(entry.AddressableKey);

                // 等待加载完成
                var config = await handle.ToUniTask(cancellationToken: ct);

                if (config == null)
                {
                    throw new Exception($"Addressables 加载返回 null，Key: {entry.AddressableKey}");
                }

                result.LoadedConfigs[entry.Name] = config;
                Debug.Log($"[ConfigLoader] ✓ 异步加载成功: {entry.Name} ({configType.Name})");
            }
            catch (Exception ex)
            {
                result.FailedConfigs.Add(entry.Name);

                var logLevel = entry.IsRequired ? LogType.Error : LogType.Warning;
                Debug.LogFormat(logLevel, LogOption.NoStacktrace, null,
                    "[ConfigLoader] ✗ 异步加载失败: {0} - {1}", entry.Name, ex.Message);
            }
        }

        /// <summary>
        /// 泛型配置加载方法（通过反射调用）
        /// </summary>
        private static TConfig LoadConfigGeneric<TConfig>(
            ConfigManifest.ConfigEntry entry,
            bool enableValidation)
            where TConfig : ScriptableObject
        {
            // ⚠️ 规范豁免：ConfigLoader 在 DI 容器构建前运行，允许直接调用 Addressables
            // 业务代码必须使用 IAssetProvider 接口
            var handle = Addressables.LoadAssetAsync<TConfig>(entry.AddressableKey);
            var config = handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded || config == null)
            {
                Debug.LogWarning($"[ConfigLoader] Addressables 加载失败: {entry.Name}，尝试创建默认配置");
                // 尝试创建默认配置
                return TryCreateDefaultConfig<TConfig>(entry.Name);
            }

            // 配置验证
            if (enableValidation)
            {
                ValidateConfig(config, entry.Name);
            }

            return config;
        }

        /// <summary>
        /// 异步泛型配置加载方法（通过反射调用）
        /// </summary>
        private static async Cysharp.Threading.Tasks.UniTask<TConfig> LoadConfigGenericAsync<TConfig>(
            ConfigManifest.ConfigEntry entry,
            bool enableValidation,
            System.Threading.CancellationToken ct)
            where TConfig : ScriptableObject
        {
            // ⚠️ 规范豁免：ConfigLoader 在 DI 容器构建前运行，允许直接调用 Addressables
            // 业务代码必须使用 IAssetProvider 接口
            var handle = Addressables.LoadAssetAsync<TConfig>(entry.AddressableKey);
            var config = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded || config == null)
            {
                Debug.LogWarning($"[ConfigLoader] Addressables 异步加载失败: {entry.Name}，尝试创建默认配置");
                // 尝试创建默认配置
                return TryCreateDefaultConfig<TConfig>(entry.Name);
            }

            // 配置验证
            if (enableValidation)
            {
                ValidateConfig(config, entry.Name);
            }

            return config;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private static TConfig TryCreateDefaultConfig<TConfig>(string configName)
            where TConfig : ScriptableObject
        {
            try
            {
                Debug.LogWarning($"[ConfigLoader] 创建默认配置: {configName}");
                return ScriptableObject.CreateInstance<TConfig>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigLoader] 无法创建默认配置: {configName} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private static void ValidateConfig(ScriptableObject config, string configName)
        {
            if (config is IValidatableConfig validatable)
            {
                if (!validatable.Validate(out var errors))
                {
                    var errorMessage = string.Join("\n  - ", errors);
                    Debug.LogWarning($"[ConfigLoader] 配置验证失败: {configName}\n  - {errorMessage}");
                }
                else
                {
                    Debug.Log($"[ConfigLoader] 配置验证通过: {configName}");
                }
            }
        }

        /// <summary>
        /// 判断是否应该加载此条目（根据环境）
        /// </summary>
        private static bool ShouldLoadEntry(ConfigManifest.ConfigEntry entry)
        {
            // 验证条目有效性
            if (!entry.IsValid(out var error))
            {
                Debug.LogWarning($"[ConfigLoader] 跳过无效配置: {error}");
                return false;
            }

#if UNITY_EDITOR
            // 开发环境：跳过仅生产环境的配置
            return !entry.ProductionOnly;
#else
            // 生产环境：跳过仅开发环境的配置
            return !entry.DevOnly;
#endif
        }

        /// <summary>
        /// 从不同 manifest 获取验证开关，默认开启。
        /// </summary>
        private static bool GetEnableValidation(IConfigManifest manifest)
        {
            if (manifest is ConfigManifest coreManifest)
            {
                return coreManifest.EnableValidation;
            }

            return true;
        }

        /// <summary>
        /// 输出加载摘要
        /// </summary>
        private static void LogLoadingSummary(ConfigLoadResult result)
        {
            var total = result.LoadedConfigs.Count + result.FailedConfigs.Count;
            var successRate = total > 0 ? (result.LoadedConfigs.Count * 100 / total) : 100;

            Debug.Log("[ConfigLoader] === 配置加载完成 ===");
            Debug.Log($"[ConfigLoader] 总计: {total} | 成功: {result.LoadedConfigs.Count} | 失败: {result.FailedConfigs.Count} | 成功率: {successRate}%");

            if (result.LoadedConfigs.Count > 0)
            {
                Debug.Log($"[ConfigLoader] 已加载配置: {string.Join(", ", result.LoadedConfigs.Keys)}");
            }

            if (result.FailedConfigs.Count > 0)
            {
                Debug.LogWarning($"[ConfigLoader] 失败配置: {string.Join(", ", result.FailedConfigs)}");
            }
        }
    }

    /// <summary>
    /// 配置加载结果
    /// 
    /// <para><b>用途</b>：</para>
    /// <list type="bullet">
    ///   <item>存储加载成功的配置实例</item>
    ///   <item>记录加载失败的配置名称</item>
    ///   <item>提供给 ConfigRegistry 用于注册到 DI 容器</item>
    /// </list>
    /// </summary>
    public class ConfigLoadResult
    {
        /// <summary>
        /// 成功加载的配置字典（配置名 -> 配置实例）
        /// </summary>
        public Dictionary<string, object> LoadedConfigs = new();

        /// <summary>
        /// 加载失败的配置名称列表
        /// </summary>
        public List<string> FailedConfigs = new();

        /// <summary>
        /// 是否所有配置都加载成功
        /// </summary>
        public bool AllSucceeded => FailedConfigs.Count == 0;

        /// <summary>
        /// 获取指定类型的配置
        /// </summary>
        public TConfig Get<TConfig>(string configName) where TConfig : ScriptableObject
        {
            if (LoadedConfigs.TryGetValue(configName, out var config))
            {
                return config as TConfig;
            }
            return null;
        }
    }
}
