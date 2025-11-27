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
        private TextMeshProUGUI _textArea;

        [Header("显示设置")]
        [Tooltip("是否在游戏开始时默认显示面板")]
        [SerializeField]
        private bool _visibleOnStart = false;

        [Tooltip("用于切换日志面板显示/隐藏的按键")]
        [SerializeField]
        private KeyCode _toggleKey = KeyCode.F1;

        [Tooltip("显示的最小日志等级（低于此等级的日志不会显示在面板中）")]
        [SerializeField]
        private LogLevel _minimumLevelFilter = LogLevel.Debug;

        [Tooltip("最多保留的日志条数，超过后会从最旧的开始丢弃")]
        [SerializeField]
        private int _maxEntryCount = 200;

        [Header("分类过滤（可选）")]
        [Tooltip("如果为空，则显示所有分类；如果设置了值，则只显示这些分类的日志")]
        [SerializeField]
        private LogCategory[] _enabledCategories = Array.Empty<LogCategory>();

        private readonly List<LogEntry> _entries = new List<LogEntry>();

        private ILogService _logService;
        private IDisposable _subscription;

        private static readonly StringBuilder _sharedBuilder = new StringBuilder(2048);

        [Inject]
        public void Construct(ILogService service)
        {
            _logService = service ?? throw new ArgumentNullException(nameof(service));
        }

        private void Awake()
        {
            if (_textArea == null)
            {
                _textArea = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_textArea == null)
            {
                Debug.LogWarning("LogPanel: 未找到 TextMeshProUGUI 引用，请在 Inspector 中手动绑定。");
            }

            gameObject.SetActive(_visibleOnStart);
        }

        private void Start()
        {
            if (_logService == null)
            {
                Debug.LogWarning("LogPanel: ILogService 未注入，日志面板将无法工作。");
                return;
            }

            _subscription = _logService.LogStream
                .Subscribe(OnLogReceived);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }
        }

        private void OnLogReceived(LogEntry entry)
        {
            // 等级过滤
            if (entry.Level < _minimumLevelFilter)
            {
                return;
            }

            // 分类过滤（如果配置了）
            if (!IsCategoryEnabled(entry.Category))
            {
                return;
            }

            _entries.Add(entry);

            // 控制最大条数
            if (_entries.Count > _maxEntryCount)
            {
                var overflow = _entries.Count - _maxEntryCount;
                _entries.RemoveRange(0, overflow);
            }

            RebuildText();
        }

        private bool IsCategoryEnabled(LogCategory category)
        {
            if (_enabledCategories == null || _enabledCategories.Length == 0)
            {
                // 未配置分类过滤 → 视为全部启用
                return true;
            }

            var count = _enabledCategories.Length;
            for (var index = 0; index < count; index += 1)
            {
                if (_enabledCategories[index] == category)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildText()
        {
            if (_textArea == null)
            {
                return;
            }

            _sharedBuilder.Clear();

            var count = _entries.Count;
            for (var index = 0; index < count; index += 1)
            {
                _sharedBuilder.AppendLine(_entries[index].ToString());
            }

            _textArea.text = _sharedBuilder.ToString();
        }
    }
}
