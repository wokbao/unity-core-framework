using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Save.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Feature.Save.Runtime
{
    /// <summary>
    /// 本地文件存档服务实现
    /// </summary>
    /// <remarks>
    /// <para><b>存储位置</b>：<c>Application.persistentDataPath/Saves/</c></para>
    /// <para><b>文件格式</b>：JSON + 可选 AES 加密</para>
    /// </remarks>
    public sealed class SaveService : ISaveService, IDisposable
    {
        private const string SaveDirectory = "Saves";
        private const string MetadataFileName = "_metadata.json";

        private readonly ILogService _logService;
        private readonly SaveOptions _options;
        private readonly string _savePath;
        private readonly object _lock = new();

        private Dictionary<string, SaveSlotInfo> _slotCache;

        /// <summary>
        /// 初始化存档服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="options">存档选项（可选）</param>
        public SaveService(ILogService logService, SaveOptions options = null)
        {
            _logService = logService;
            _options = options ?? SaveOptions.Default;
            _savePath = Path.Combine(Application.persistentDataPath, SaveDirectory);

            EnsureSaveDirectoryExists();
            LoadMetadataCache();

            _logService?.Information(LogCategory.Core, $"存档服务初始化完成，存档路径：{_savePath}");
        }

        /// <inheritdoc/>
        public async UniTask SaveAsync<T>(string slotId, T data, CancellationToken ct = default) where T : class
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("存档槽 ID 不能为空", nameof(slotId));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ct.ThrowIfCancellationRequested();

            var filePath = GetSlotFilePath(slotId);

            try
            {
                // 序列化为 JSON
                var json = _options.PrettyPrint
                    ? JsonUtility.ToJson(data, true)
                    : JsonUtility.ToJson(data);

                // 可选加密
                var content = _options.Encrypt ? Encrypt(json) : json;

                // 写入文件
                await UniTask.SwitchToThreadPool();
                ct.ThrowIfCancellationRequested();

                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, ct);

                await UniTask.SwitchToMainThread(ct);

                // 更新元数据
                UpdateSlotMetadata(slotId, filePath);

                _logService?.Information(LogCategory.Core, $"存档保存成功：{slotId}");
            }
            catch (OperationCanceledException)
            {
                _logService?.Debug(LogCategory.Core, $"存档保存被取消：{slotId}");
                throw;
            }
            catch (Exception ex)
            {
                _logService?.Error(LogCategory.Core, $"存档保存失败：{slotId}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async UniTask<T> LoadAsync<T>(string slotId, CancellationToken ct = default) where T : class
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("存档槽 ID 不能为空", nameof(slotId));

            ct.ThrowIfCancellationRequested();

            var filePath = GetSlotFilePath(slotId);

            if (!File.Exists(filePath))
            {
                _logService?.Debug(LogCategory.Core, $"存档不存在：{slotId}");
                return null;
            }

            try
            {
                await UniTask.SwitchToThreadPool();
                ct.ThrowIfCancellationRequested();

                var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);

                await UniTask.SwitchToMainThread(ct);

                // 可选解密
                var json = _options.Encrypt ? Decrypt(content) : content;

                // 反序列化
                var data = JsonUtility.FromJson<T>(json);

                _logService?.Information(LogCategory.Core, $"存档加载成功：{slotId}");
                return data;
            }
            catch (OperationCanceledException)
            {
                _logService?.Debug(LogCategory.Core, $"存档加载被取消：{slotId}");
                throw;
            }
            catch (Exception ex)
            {
                _logService?.Error(LogCategory.Core, $"存档加载失败：{slotId}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public bool Exists(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return false;
            return File.Exists(GetSlotFilePath(slotId));
        }

        /// <inheritdoc/>
        public void Delete(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("存档槽 ID 不能为空", nameof(slotId));

            var filePath = GetSlotFilePath(slotId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logService?.Information(LogCategory.Core, $"存档已删除：{slotId}");
            }

            lock (_lock)
            {
                _slotCache?.Remove(slotId);
            }

            SaveMetadataCache();
        }

        /// <inheritdoc/>
        public IReadOnlyList<SaveSlotInfo> GetAllSlots()
        {
            lock (_lock)
            {
                return _slotCache?.Values.OrderByDescending(s => s.LastSaveTime).ToList()
                       ?? new List<SaveSlotInfo>();
            }
        }

        /// <inheritdoc/>
        public SaveSlotInfo GetSlotInfo(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;

            lock (_lock)
            {
                return _slotCache?.TryGetValue(slotId, out var info) == true ? info : null;
            }
        }

        public void Dispose()
        {
            SaveMetadataCache();
        }

        #region Private Methods

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        private string GetSlotFilePath(string slotId)
        {
            var fileName = $"{slotId}{_options.FileExtension}";
            return Path.Combine(_savePath, fileName);
        }

        private void UpdateSlotMetadata(string slotId, string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            var slotInfo = new SaveSlotInfo
            {
                SlotId = slotId,
                DisplayName = slotId,
                LastSaveTime = DateTime.Now,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            };

            lock (_lock)
            {
                _slotCache ??= new Dictionary<string, SaveSlotInfo>();
                _slotCache[slotId] = slotInfo;
            }

            SaveMetadataCache();
        }

        private void LoadMetadataCache()
        {
            var metadataPath = Path.Combine(_savePath, MetadataFileName);

            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = File.ReadAllText(metadataPath, Encoding.UTF8);
                    var wrapper = JsonUtility.FromJson<SlotMetadataWrapper>(json);

                    lock (_lock)
                    {
                        _slotCache = wrapper?.Slots?.ToDictionary(s => s.SlotId)
                                     ?? new Dictionary<string, SaveSlotInfo>();
                    }
                }
                catch (Exception ex)
                {
                    _logService?.Warning(LogCategory.Core, $"加载存档元数据失败，将重建：{ex.Message}");
                    _slotCache = new Dictionary<string, SaveSlotInfo>();
                }
            }
            else
            {
                _slotCache = new Dictionary<string, SaveSlotInfo>();
            }
        }

        private void SaveMetadataCache()
        {
            try
            {
                var metadataPath = Path.Combine(_savePath, MetadataFileName);

                List<SaveSlotInfo> slots;
                lock (_lock)
                {
                    slots = _slotCache?.Values.ToList() ?? new List<SaveSlotInfo>();
                }

                var wrapper = new SlotMetadataWrapper { Slots = slots };
                var json = JsonUtility.ToJson(wrapper, true);

                File.WriteAllText(metadataPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logService?.Warning(LogCategory.Core, $"保存存档元数据失败：{ex.Message}");
            }
        }

        private string Encrypt(string plainText)
        {
            var key = GetEncryptionKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV + 加密数据
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        private string Decrypt(string cipherText)
        {
            var key = GetEncryptionKey();
            var fullBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = key;

            // 提取 IV
            var iv = new byte[16];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            aes.IV = iv;

            // 提取加密数据
            var encryptedBytes = new byte[fullBytes.Length - 16];
            Buffer.BlockCopy(fullBytes, 16, encryptedBytes, 0, encryptedBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        private byte[] GetEncryptionKey()
        {
            var keySource = string.IsNullOrEmpty(_options.EncryptionKey)
                ? SystemInfo.deviceUniqueIdentifier
                : _options.EncryptionKey;

            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
        }

        #endregion

        #region Serialization Helpers

        [Serializable]
        private class SlotMetadataWrapper
        {
            public List<SaveSlotInfo> Slots;
        }

        #endregion
    }
}
