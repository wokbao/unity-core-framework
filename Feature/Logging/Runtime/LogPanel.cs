using System;
using System.Collections.Generic;
using System.Text;
using Core.Feature.Logging.Abstractions;
using TMPro;
using R3;
using UnityEngine;
using VContainer;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 游戏内日志面板。
    /// 订阅日志流，并将日志显示在 TextMeshPro 文本中。
    /// 支持按等级过滤、最大条数限制、按键开关显示。
    /// </summary>
    public sealed class LogPanel : MonoBehaviour
    {
        [Header("UI 引用")]
        [Tooltip("用于显示日志文本的 TextMeshProUGUI 组件")]
        [SerializeField]
        private TextMeshProUGUI textArea;

        [Header("显示设置")]
        [Tooltip("是否在游戏开始时默认显示面板")]
        [SerializeField]
        private bool visibleOnStart = false;

        [Tooltip("用于切换日志面板显示/隐藏的按键")]
        [SerializeField]
        private KeyCode toggleKey = KeyCode.F1;

        [Tooltip("显示的最小日志等级（低于此等级的日志不会显示在面板中）")]
        [SerializeField]
        private LogLevel minimumLevelFilter = LogLevel.Debug;

        [Tooltip("最多保留的日志条数，超过后会从最旧的开始丢弃")]
        [SerializeField]
        private int maxEntryCount = 200;

        [Header("分类过滤（可选）")]
        [Tooltip("如果为空，则显示所有分类；如果设置了值，则只显示这些分类的日志")]
        [SerializeField]
        private LogCategory[] enabledCategories = Array.Empty<LogCategory>();

        private readonly List<LogEntry> entries = new List<LogEntry>();

        private ILogService logService;
        private IDisposable subscription;

        private static readonly StringBuilder SharedBuilder = new StringBuilder(2048);

        [Inject]
        public void Construct(ILogService logService)
        {
            this.logService = logService;
        }

        private void Awake()
        {
            if (textArea == null)
            {
                textArea = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (textArea == null)
            {
                Debug.LogWarning("LogPanel: 未找到 TextMeshProUGUI 引用，请在 Inspector 中手动绑定。");
            }

            gameObject.SetActive(visibleOnStart);
        }

        private void Start()
        {
            if (logService == null)
            {
                Debug.LogWarning("LogPanel: ILogService 未注入，日志面板将无法工作。");
                return;
            }

            subscription = logService.LogStream
                .Subscribe(OnLogReceived);
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }
        }

        private void OnLogReceived(LogEntry entry)
        {
            // 等级过滤
            if (entry.Level < minimumLevelFilter)
            {
                return;
            }

            // 分类过滤（如果配置了）
            if (!IsCategoryEnabled(entry.Category))
            {
                return;
            }

            entries.Add(entry);

            // 控制最大条数
            if (entries.Count > maxEntryCount)
            {
                var overflow = entries.Count - maxEntryCount;
                entries.RemoveRange(0, overflow);
            }

            RebuildText();
        }

        private bool IsCategoryEnabled(LogCategory category)
        {
            if (enabledCategories == null || enabledCategories.Length == 0)
            {
                // 未配置分类过滤 → 视为全部启用
                return true;
            }

            var count = enabledCategories.Length;
            for (var index = 0; index < count; index += 1)
            {
                if (enabledCategories[index] == category)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildText()
        {
            if (textArea == null)
            {
                return;
            }

            SharedBuilder.Clear();

            var count = entries.Count;
            for (var index = 0; index < count; index += 1)
            {
                SharedBuilder.AppendLine(entries[index].ToString());
            }

            textArea.text = SharedBuilder.ToString();
        }
    }
}
