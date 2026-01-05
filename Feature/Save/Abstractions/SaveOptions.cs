namespace Core.Feature.Save.Abstractions
{
    /// <summary>
    /// 存档保存选项
    /// </summary>
    public sealed class SaveOptions
    {
        /// <summary>
        /// 默认选项
        /// </summary>
        public static SaveOptions Default { get; } = new();

        /// <summary>
        /// 是否加密存档数据
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// 加密密钥（仅当 Encrypt 为 true 时使用）
        /// </summary>
        /// <remarks>
        /// 如果未设置，将使用设备唯一标识符作为密钥
        /// </remarks>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// 是否使用漂亮的 JSON 格式（便于调试）
        /// </summary>
        public bool PrettyPrint { get; set; } = true;

        /// <summary>
        /// 存档文件后缀
        /// </summary>
        public string FileExtension { get; set; } = ".sav";
    }
}
