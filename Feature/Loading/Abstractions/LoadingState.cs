namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 加载状态快照。
    /// </summary>
    public readonly struct LoadingState
    {
        public bool IsLoading { get; }
        public float Progress { get; }
        public string Description { get; }
        public int ActiveOperations { get; }

        public LoadingState(bool isLoading, float progress, string description, int activeOperations)
        {
            IsLoading = isLoading;
            Progress = progress;
            Description = description;
            ActiveOperations = activeOperations;
        }
    }
}
