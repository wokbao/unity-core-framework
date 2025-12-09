using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// 简单的常驻助手：挂到需要跨场景保留的对象上。
    /// </summary>
    public sealed class DontDestroyOnLoadHelper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
