using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Core.Feature.Localization.Abstractions
{
    /// <summary>
    /// 定义语言代码的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocaleCodeAttribute : Attribute
    {
        public string Code { get; }
        public LocaleCodeAttribute(string code) => Code = code;
    }

    /// <summary>
    /// 支持的语言区域
    /// </summary>
    /// <remarks>
    /// <para><b>扩展新语言</b>：仅需在此处添加枚举值，并标记 [LocaleCode("iso-code")] 即可。</para>
    /// </remarks>
    public enum SupportedLocale
    {
        [InspectorName("English (en)")]
        [LocaleCode("en")]
        English,

        [InspectorName("简体中文 (zh-Hans)")]
        [LocaleCode("zh-Hans")]
        ChineseSimplified,

        [InspectorName("繁體中文 (zh-Hant)")]
        [LocaleCode("zh-Hant")]
        ChineseTraditional
    }

    /// <summary>
    /// 语言区域扩展方法
    /// </summary>
    public static class LocaleExtensions
    {
        private static readonly Dictionary<SupportedLocale, string> LocaleToCodeMap;
        private static readonly Dictionary<string, SupportedLocale> CodeToLocaleMap;

        /// <summary>
        /// 静态构造函数：通过反射自动构建映射表
        /// </summary>
        static LocaleExtensions()
        {
            LocaleToCodeMap = new Dictionary<SupportedLocale, string>();
            CodeToLocaleMap = new Dictionary<string, SupportedLocale>();

            foreach (SupportedLocale locale in Enum.GetValues(typeof(SupportedLocale)))
            {
                var field = typeof(SupportedLocale).GetField(locale.ToString());
                var attr = field.GetCustomAttribute<LocaleCodeAttribute>();

                if (attr != null)
                {
                    // 注册映射
                    LocaleToCodeMap[locale] = attr.Code;

                    // 避免重复 key 报错（虽然理论上 code 不应重复）
                    if (!CodeToLocaleMap.ContainsKey(attr.Code))
                    {
                        CodeToLocaleMap[attr.Code] = locale;
                    }
                }
                else
                {
                    // 容错：如果忘记写特性，默认用 "en"
                    LocaleToCodeMap[locale] = "en";
                }
            }
        }

        public static string ToCode(this SupportedLocale locale)
            => LocaleToCodeMap.TryGetValue(locale, out var code) ? code : "en";

        public static SupportedLocale FromCode(string code)
            => CodeToLocaleMap.TryGetValue(code, out var locale) ? locale : SupportedLocale.English;
    }
}


