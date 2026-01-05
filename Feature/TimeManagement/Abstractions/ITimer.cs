using System;

namespace Core.Feature.TimeManagement.Abstractions
{
    /// <summary>
    /// 计时器接口
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// 已经过的时间（秒）
        /// </summary>
        float ElapsedTime { get; }

        /// <summary>
        /// 计时器是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 计时器是否已完成
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 是否为重复计时器
        /// </summary>
        bool IsRepeating { get; }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复计时器
        /// </summary>
        void Resume();

        /// <summary>
        /// 取消计时器
        /// </summary>
        void Cancel();

        /// <summary>
        /// 重置计时器到初始状态
        /// </summary>
        void Reset();
    }
}
