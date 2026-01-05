using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Feature.Save.Abstractions
{
    /// <summary>
    /// 数据持久化服务接口
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>提供多存档槽的数据保存/加载能力</item>
    ///   <item>支持可选的数据加密</item>
    ///   <item>支持存档元数据管理</item>
    /// </list>
    /// </remarks>
    public interface ISaveService
    {
        /// <summary>
        /// 异步保存数据到指定存档槽
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="slotId">存档槽标识</param>
        /// <param name="data">要保存的数据</param>
        /// <param name="ct">取消令牌</param>
        UniTask SaveAsync<T>(string slotId, T data, CancellationToken ct = default) where T : class;

        /// <summary>
        /// 异步加载指定存档槽的数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="slotId">存档槽标识</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>加载的数据，如果不存在则返回 null</returns>
        UniTask<T> LoadAsync<T>(string slotId, CancellationToken ct = default) where T : class;

        /// <summary>
        /// 检查存档槽是否存在
        /// </summary>
        /// <param name="slotId">存档槽标识</param>
        /// <returns>如果存在返回 true</returns>
        bool Exists(string slotId);

        /// <summary>
        /// 删除指定存档槽
        /// </summary>
        /// <param name="slotId">存档槽标识</param>
        void Delete(string slotId);

        /// <summary>
        /// 获取所有存档槽的信息
        /// </summary>
        /// <returns>存档槽信息列表</returns>
        IReadOnlyList<SaveSlotInfo> GetAllSlots();

        /// <summary>
        /// 获取指定存档槽的元信息
        /// </summary>
        /// <param name="slotId">存档槽标识</param>
        /// <returns>存档槽信息，如果不存在返回 null</returns>
        SaveSlotInfo GetSlotInfo(string slotId);
    }

    /// <summary>
    /// 存档槽信息
    /// </summary>
    public sealed class SaveSlotInfo
    {
        /// <summary>
        /// 存档槽标识
        /// </summary>
        public string SlotId { get; set; }

        /// <summary>
        /// 存档显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 最后保存时间
        /// </summary>
        public DateTime LastSaveTime { get; set; }

        /// <summary>
        /// 存档文件大小（字节）
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// 自定义元数据
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
