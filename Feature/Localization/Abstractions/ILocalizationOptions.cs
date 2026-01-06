namespace Core.Feature.Localization.Abstractions
{
    /// <summary>
    /// 本地化服务配置选项接口
    /// </summary>
    /// <remarks>
    /// <para><b>设计模式</b>：依赖倒置原则 (DIP)</para>
    /// <para><b>用途</b>：Core 层定义接口，Game 层提供具体实现（ScriptableObject 配置）</para>
    /// <para><b>优势</b>：</para>
    /// <list type="bullet">
    ///   <item>Core 层保持通用性，不依赖具体配置类</item>
    ///   <item>Game 层可以自由选择配置源（SO、JSON、远程服务器）</item>
    ///   <item>便于单元测试 Mock</item>
    /// </list>
    /// </remarks>
    public interface ILocalizationOptions
    {
        /// <summary>
        /// 默认 String Table 名称
        /// </summary>
        /// <remarks>当调用不指定表名的 GetText 时使用此表</remarks>
        string DefaultTableName { get; }

        /// <summary>
        /// Fallback 语言代码
        /// </summary>
        /// <remarks>当翻译缺失时回退到此语言（ISO 639-1 代码，如 "en"、"zh-Hans"）</remarks>
        string FallbackLocaleCode { get; }

        /// <summary>
        /// 是否在启动时自动初始化本地化系统
        /// </summary>
        bool InitializeOnStartup { get; }

        /// <summary>
        /// 是否记住用户的语言选择
        /// </summary>
        /// <remarks>启用后会将用户选择保存到 PlayerPrefs</remarks>
        bool RememberUserSelection { get; }
    }
}
