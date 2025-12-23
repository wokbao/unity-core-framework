namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 加载状态快照。
    /// </summary>
    public readonly struct LoadingState
    {
        public bool IsLoading { get; }
        public bool ShouldShowUi { get; }
        public float Progress { get; }
        public string Description { get; }
        public int ActiveOperations { get; }

        public LoadingState(bool isLoading, bool shouldShowUi, float progress, string description, int activeOperations)
        {
            IsLoading = isLoading;
            ShouldShowUi = shouldShowUi;
            Progress = progress;
            Description = description;
            ActiveOperations = activeOperations;
        }
    }
}
