using Core.Feature.EventBus.Abstractions;
using UnityEngine;
using VContainer;

namespace Core.Tests
{
    /// <summary>
    /// 简单事件总线注入与发布/订阅验证。
    /// </summary>
    public sealed class EventBusTest : MonoBehaviour
    {
        [Inject] private IEventBus _eventBus;

        private void Start()
        {
            if (_eventBus == null)
            {
                Debug.LogError("❌ IEventBus 注入失败！");
                return;
            }

            var tokenHigh = _eventBus.Subscribe<SampleEvent>(e => Debug.Log($"High priority: {e.Message}"), priority: 10);
            var tokenLow = _eventBus.Subscribe<SampleEvent>(e => Debug.Log($"Low priority: {e.Message}"), priority: 0);

            _eventBus.Publish(new SampleEvent("Hello EventBus"));

            // 清理示例订阅
            tokenHigh.Dispose();
            tokenLow.Dispose();
        }

        private readonly struct SampleEvent
        {
            public string Message { get; }
            public SampleEvent(string message) => Message = message;
        }
    }
}
