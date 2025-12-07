using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 核心配置清单
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>声明所有需要在启动时加载的配置</item>
    ///   <item>定义配置的优先级和依赖关系</item>
    ///   <item>支持多环境配置（开发/测试/生产）</item>
    ///   <item>提供配置加载的元数据</item>
    /// </list>
    /// 
    /// <para><b>设计原则</b>：</para>
    /// <list type="bullet">
    ///   <item>声明式配置：通过 Inspector 可视化管理，无需修改代码</item>
    ///   <item>类型安全：使用完整类型名确保正确的类型解析</item>
    ///   <item>环境隔离：支持不同环境加载不同配置</item>
    ///   <item>优先级控制：确保有依赖关系的配置按正确顺序加载</item>
    /// </list>
    /// 
    /// <para><b>使用示例</b>：</para>
    /// <code>
    /// // 1. 在 Unity 中创建清单
    /// // 右键 → Create → Core → Configuration → Manifest
    /// 
    /// // 2. 在 Inspector 中添加配置条目
    /// Name: "LoggingConfig"
    /// AddressableKey: "Core/Configs/LoggingConfig"
    /// ConfigTypeName: "Core.Feature.Logging.ScriptableObjects.LoggingConfig"
    /// IsRequired: true
    /// Priority: 0
    /// 
    /// // 3. 在 CoreLifetimeScope 中引用此清单
    /// [SerializeField] private ConfigManifest _coreConfigManifest;
    /// </code>
    /// 
    /// <para><b>扩展配置步骤</b>：</para>
    /// <list type="number">
    ///   <item>创建新的配置 ScriptableObject 类</item>
    ///   <item>在 Addressables 中设置该配置资源</item>
    ///   <item>在清单中添加新的 ConfigEntry（仅需在 Inspector 操作）</item>
    ///   <item>无需修改任何代码！</item>
    /// </list>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>ConfigTypeName 必须是完整的类型名（包含命名空间）</item>
    ///   <item>Priority 数字越小越优先加载</item>
    ///   <item>IsRequired 为 true 的配置加载失败会记录错误</item>
    ///   <item>环境标志（DevOnly/ProductionOnly）互斥，不能同时为 true</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigManifest", menuName = "Core/Configuration/Manifest", order = 0)]
    public class ConfigManifest : ScriptableObject
    {
        [Header("配置条目")]
        [Tooltip("所有需要加载的核心配置")]
        public List<ConfigEntry> Entries = new();

        [Header("加载设置")]
        [Tooltip("配置加载失败时是否阻塞启动")]
        public bool BlockOnFailure = false;

        [Tooltip("启用配置验证（实现 IValidatableConfig 的配置会自动验证）")]
        public bool EnableValidation = true;

        /// <summary>
        /// 配置条目定义
        /// </summary>
        [System.Serializable]
        public class ConfigEntry
        {
            [Header("基本信息")]
            [Tooltip("配置名称（用于日志和识别）")]
            public string Name;

            [Tooltip("Addressable Key（资源地址）")]
            public string AddressableKey;

            [Tooltip("配置类型的完整名称（包含命名空间）\n例如: Core.Feature.Logging.ScriptableObjects.LoggingConfig")]
            [TextArea(1, 3)]
            public string ConfigTypeName;

            [Header("加载控制")]
            [Tooltip("是否必需（失败时是否记录错误）")]
            public bool IsRequired = true;

            [Tooltip("加载优先级（数字越小越优先，0 最优先）")]
            [Range(0, 100)]
            public int Priority = 0;

            [Header("环境控制")]
            [Tooltip("仅在开发环境加载（Editor 模式）")]
            public bool DevOnly = false;

            [Tooltip("仅在生产环境加载（非 Editor 模式）")]
            public bool ProductionOnly = false;

            /// <summary>
            /// 验证配置条目的有效性
            /// </summary>
            public bool IsValid(out string errorMessage)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    errorMessage = "配置名称不能为空";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AddressableKey))
                {
                    errorMessage = $"配置 '{Name}' 的 AddressableKey 不能为空";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ConfigTypeName))
                {
                    errorMessage = $"配置 '{Name}' 的 ConfigTypeName 不能为空";
                    return false;
                }

                if (DevOnly && ProductionOnly)
                {
                    errorMessage = $"配置 '{Name}' 的 DevOnly 和 ProductionOnly 不能同时为 true";
                    return false;
                }

                errorMessage = null;
                return true;
            }
        }

        /// <summary>
        /// 验证清单的完整性
        /// </summary>
        private void OnValidate()
        {
            if (Entries == null) return;

            foreach (var entry in Entries)
            {
                if (!entry.IsValid(out var error))
                {
                    Debug.LogWarning($"[ConfigManifest] 配置条目无效: {error}", this);
                }
            }
        }
    }
}
