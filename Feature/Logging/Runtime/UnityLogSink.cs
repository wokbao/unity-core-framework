using Core.Feature.Logging.Abstractions;
using UnityEngine;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 将日志输出到 Unity 控制台的接收器。
    /// </summary>
    public sealed class UnityLogSink : ILogSink
    {
        public void Write(LogEntry entry)
        {
            var text = entry.ToString();

            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Information:
                    Debug.Log(text);
                    break;

                case LogLevel.Warning:
                    Debug.LogWarning(text);
                    break;

                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(text);
                    break;
            }
        }
    }
}
