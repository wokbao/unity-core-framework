namespace Core.Feature.Localization.Abstractions
{
    /// <summary>
    /// 默认本地化配置选项（Null Object Pattern）
    /// </summary>
    /// <remarks>
    /// <para><b>设计模式</b>：Null Object Pattern</para>
    /// <para><b>用途</b>：当未提供配置时，提供合理的默认值</para>
    /// <para><b>优势</b>：</para>
    /// <list type="bullet">
    ///   <item>避免服务内部的 null 检查</item>
    ///   <item>提供明确的默认行为</item>
    ///   <item>简化单元测试</item>
    /// </list>
    /// </remarks>
    public sealed class DefaultLocalizationOptions : ILocalizationOptions
    {
        #region 默认值常量

        /// <summary>
        /// 默认 String Table 名称
        /// </summary>
        public const string DefaultTable = "SharedStrings";

        /// <summary>
        /// 默认 Fallback 语言
        /// </summary>
        public const SupportedLocale DefaultLocale = SupportedLocale.English;

        #endregion

        /// <summary>
        /// 单例实例
        /// </summary>
        public static readonly DefaultLocalizationOptions Instance = new();

        /// <inheritdoc/>
        public string DefaultTableName => DefaultTable;

        /// <inheritdoc/>
        public string FallbackLocaleCode => DefaultLocale.ToCode();

        /// <inheritdoc/>
        public bool InitializeOnStartup => true;

        /// <inheritdoc/>
        public bool RememberUserSelection => true;

        private DefaultLocalizationOptions() { }
    }
}

