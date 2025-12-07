using System.Collections.Generic;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 可验证的配置接口
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>定义配置自我验证的标准协议</item>
    ///   <item>允许配置在加载后检查自身完整性</item>
    ///   <item>提供友好的错误信息用于调试</item>
    /// </list>
    /// 
    /// <para><b>设计原则</b>：</para>
    /// <list type="bullet">
    ///   <item>配置拥有自我验证的能力（职责在配置本身）</item>
    ///   <item>验证失败不抛出异常，而是返回错误列表</item>
    ///   <item>支持多条错误消息，方便一次性发现所有问题</item>
    /// </list>
    /// 
    /// <para><b>使用示例</b>：</para>
    /// <code>
    /// [CreateAssetMenu(...)]
    /// public class LoggingConfig : ScriptableObject, IValidatableConfig
    /// {
    ///     public LogLevel minimumLogLevel;
    ///     public bool enableUnityConsoleOutput;
    ///     
    ///     public bool Validate(out List&lt;string&gt; errors)
    ///     {
    ///         errors = new List&lt;string&gt;();
    ///         
    ///         // 验证日志等级范围
    ///         if (minimumLogLevel &lt; LogLevel.Debug || minimumLogLevel &gt; LogLevel.Critical)
    ///         {
    ///             errors.Add($"最小日志等级 {minimumLogLevel} 超出有效范围");
    ///         }
    ///         
    ///         // 验证至少有一个输出通道
    ///         if (!enableUnityConsoleOutput)
    ///         {
    ///             errors.Add("必须至少启用一个日志输出通道");
    ///         }
    ///         
    ///         return errors.Count == 0;
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><b>验证时机</b>：</para>
    /// <list type="bullet">
    ///   <item>配置加载后：由 ConfigLoader 自动调用（如果 EnableValidation 为 true）</item>
    ///   <item>编辑器中：可在 OnValidate 中调用，实时反馈错误</item>
    ///   <item>单元测试：测试配置的有效性</item>
    /// </list>
    /// 
    /// <para><b>最佳实践</b>：</para>
    /// <list type="bullet">
    ///   <item>检查必填字段不为空</item>
    ///   <item>检查数值在合理范围内</item>
    ///   <item>检查引用不为 null（如果是必需的）</item>
    ///   <item>检查配置项之间的逻辑关系</item>
    ///   <item>提供清晰的错误消息，明确告知如何修复</item>
    /// </list>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>验证应该是幂等的（多次调用结果一致）</item>
    ///   <item>不要在验证中修改配置状态</item>
    ///   <item>验证失败不应抛出异常，而是返回 false</item>
    ///   <item>错误消息应该尽量详细，便于快速定位问题</item>
    /// </list>
    /// </summary>
    public interface IValidatableConfig
    {
        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        /// <param name="errors">
        /// 输出参数，包含所有验证错误的详细信息。
        /// 如果验证通过，列表为空。
        /// </param>
        /// <returns>
        /// true 表示配置有效；false 表示配置无效，应检查 errors 参数获取详情。
        /// </returns>
        bool Validate(out List<string> errors);
    }
}
