using System;

namespace Core.Feature.EventBus.Abstractions
{
    /// <summary>
    /// 简单事件总线，支持发布/订阅和可选优先级。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件。
        /// </summary>
        /// <param name="handler">事件处理委托。</param>
        /// <param name="priority">优先级，数值越大越先执行。</param>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <returns>用于取消订阅的 IDisposable。</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler, int priority = 0);

        /// <summary>
        /// 发布事件。
        /// </summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="evt">事件实例。</param>
        void Publish<TEvent>(TEvent evt);
    }
}
