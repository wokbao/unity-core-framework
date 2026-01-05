using System;

namespace Core.Feature.TimeManagement.Abstractions
{
    /// <summary>
    /// 时间管理器接口
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>管理全局时间缩放</item>
    ///   <item>提供暂停/恢复功能</item>
    ///   <item>创建和管理计时器</item>
    /// </list>
    /// </remarks>
    public interface ITimeManager
    {
        /// <summary>
        /// 当前时间缩放（0-1-N）
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 受时间缩放影响的增量时间
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        /// 不受时间缩放影响的增量时间
        /// </summary>
        float UnscaledDeltaTime { get; }

        /// <summary>
        /// 当前是否处于暂停状态
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 暂停时间（TimeScale 设为 0）
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复时间（TimeScale 恢复到暂停前的值）
        /// </summary>
        void Resume();

        /// <summary>
        /// 创建一个计时器
        /// </summary>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="onComplete">完成时的回调</param>
        /// <param name="useUnscaledTime">是否使用不受缩放影响的时间</param>
        /// <returns>计时器实例</returns>
        ITimer CreateTimer(float duration, Action onComplete, bool useUnscaledTime = false);

        /// <summary>
        /// 创建一个重复计时器
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="onTick">每次触发时的回调</param>
        /// <param name="useUnscaledTime">是否使用不受缩放影响的时间</param>
        /// <returns>计时器实例</returns>
        ITimer CreateRepeatingTimer(float interval, Action onTick, bool useUnscaledTime = false);

        /// <summary>
        /// 取消所有计时器
        /// </summary>
        void CancelAllTimers();

        /// <summary>
        /// 当暂停状态改变时触发
        /// </summary>
        event Action<bool> OnPauseChanged;

        /// <summary>
        /// 当时间缩放改变时触发
        /// </summary>
        event Action<float> OnTimeScaleChanged;
    }
}
