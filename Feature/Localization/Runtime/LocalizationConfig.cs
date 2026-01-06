using Core.Feature.Localization.Abstractions;
using UnityEngine;

namespace Core.Feature.Localization.Runtime
{
    /// <summary>
    /// 本地化配置 ScriptableObject
    /// </summary>
    /// <remarks>
    /// <para><b>用途</b>：配置默认表名、启动语言等设置</para>
    /// <para><b>位置</b>：Core/Configs/ 或通过 CoreConfigManifest 加载</para>
    /// <para><b>架构</b>：实现 ILocalizationOptions 接口，遵循依赖倒置原则</para>
    /// </remarks>
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Core/Localization/Config")]
    public class LocalizationConfig : ScriptableObject, ILocalizationOptions
    {
        [Header("默认设置")]
        [Tooltip("默认 String Table 名称")]
        [SerializeField] private string _defaultTableName = DefaultLocalizationOptions.DefaultTable;

        [Tooltip("Fallback 语言（当翻译缺失时）")]
        [SerializeField] private SupportedLocale _fallbackLocale = DefaultLocalizationOptions.DefaultLocale;

        [Header("启动设置")]
        [Tooltip("是否在启动时自动初始化")]
        [SerializeField] private bool _initializeOnStartup = true;

        [Tooltip("是否记住用户语言选择")]
        [SerializeField] private bool _rememberUserSelection = true;

        /// <inheritdoc/>
        public string DefaultTableName => _defaultTableName;

        /// <inheritdoc/>
        public string FallbackLocaleCode => _fallbackLocale.ToCode();

        /// <inheritdoc/>
        public bool InitializeOnStartup => _initializeOnStartup;

        /// <inheritdoc/>
        public bool RememberUserSelection => _rememberUserSelection;
    }
}
