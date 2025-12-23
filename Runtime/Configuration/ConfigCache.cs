using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 配置缓存，用于在 Splash 场景预加载配置后，
    /// 在后续 LifetimeScope.Configure() 中快速读取。
    /// </summary>
    public static class ConfigCache
    {
        private static ConfigLoadResult _coreConfigs;
        private static ConfigLoadResult _gameConfigs;
        private static bool _isCoreLoaded;
        private static bool _isGameLoaded;

        /// <summary>
        /// 存储核心配置加载结果
        /// </summary>
        public static void SetCoreConfigs(ConfigLoadResult result)
        {
            _coreConfigs = result ?? throw new ArgumentNullException(nameof(result), "配置加载结果不能为 null");
            _isCoreLoaded = true;
            Debug.Log($"[ConfigCache] 核心配置已缓存，共 {result.LoadedConfigs.Count} 个配置");
        }

        /// <summary>
        /// 存储游戏配置加载结果
        /// </summary>
        public static void SetGameConfigs(ConfigLoadResult result)
        {
            _gameConfigs = result ?? throw new ArgumentNullException(nameof(result), "配置加载结果不能为 null");
            _isGameLoaded = true;
            Debug.Log($"[ConfigCache] 游戏配置已缓存，共 {result.LoadedConfigs.Count} 个配置");
        }

        /// <summary>
        /// 获取核心配置（如果未加载则返回 null）
        /// </summary>
        public static ConfigLoadResult GetCoreConfigs()
        {
            if (!_isCoreLoaded)
            {
                Debug.LogWarning("[ConfigCache] 核心配置尚未加载，可能是跳过了 Splash 场景");
                return null;
            }
            return _coreConfigs;
        }

        /// <summary>
        /// 获取游戏配置（如果未加载则返回 null）
        /// </summary>
        public static ConfigLoadResult GetGameConfigs()
        {
            if (!_isGameLoaded)
            {
                Debug.LogWarning("[ConfigCache] 游戏配置尚未加载，可能是跳过了 Splash 场景");
                return null;
            }
            return _gameConfigs;
        }

        /// <summary>
        /// 检查核心配置是否已加载
        /// </summary>
        public static bool IsCoreLoaded => _isCoreLoaded;

        /// <summary>
        /// 检查游戏配置是否已加载
        /// </summary>
        public static bool IsGameLoaded => _isGameLoaded;

        /// <summary>
        /// 清空缓存（仅用于测试或重新启动）
        /// </summary>
        public static void Clear()
        {
            _coreConfigs = null;
            _gameConfigs = null;
           _isCoreLoaded = false;
            _isGameLoaded = false;
            Debug.Log("[ConfigCache] 配置缓存已清空");
        }
    }
}
