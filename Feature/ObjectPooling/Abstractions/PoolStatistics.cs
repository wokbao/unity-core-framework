namespace Core.Feature.ObjectPooling.Abstractions
{
    /// <summary>
    /// 对象池统计信息
    /// </summary>
    public struct PoolStatistics
    {
        /// <summary>
        /// 总创建数（所有生命周期内创建的对象）
        /// </summary>
        public int TotalCreated;

        /// <summary>
        /// 当前激活数（正在使用的对象）
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// 当前空闲数（池中可用的对象）
        /// </summary>
        public int IdleCount;

        /// <summary>
        /// 总租借次数
        /// </summary>
        public int RentCount;

        /// <summary>
        /// 总归还次数
        /// </summary>
        public int ReturnCount;

        /// <summary>
        /// 命中率（从池中获取 / 总租借次数）
        /// </summary>
        public float HitRate => RentCount > 0 ? (float)(RentCount - TotalCreated) / RentCount : 0f;
    }
}
