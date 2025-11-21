namespace Core.Feature.Logging.Abstractions
{
    /// <summary>
    /// 统一的日志分类，按功能域划分，便于过滤与分析。
    /// </summary>
    public enum LogCategory
    {
        Core = 0,
        Gameplay = 1,
        Menu = 2,
        UI = 3,
        Audio = 4,
        Network = 5,
        Persistence = 6,
        Analytics = 7,
        Input = 8,
        Other = 99
    }
}
