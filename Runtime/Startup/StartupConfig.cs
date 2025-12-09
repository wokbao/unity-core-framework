using System.Collections.Generic;
using Core.Runtime.Configuration;
using UnityEngine;

namespace Core.Runtime.Startup
{
    [CreateAssetMenu(fileName = "StartupConfig", menuName = "Core/Startup Config")]
    public sealed class StartupConfig : ScriptableObject, IValidatableConfig
    {
        [Tooltip("首个业务场景的 Addressables Key，例如 Game_Menu_Main")]
        public string firstSceneKey = "Game_Menu_Main";

        [Tooltip("加载首个场景时是否显示 Loading UI")] public bool useLoadingScreen = true;

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            if (string.IsNullOrWhiteSpace(firstSceneKey))
            {
                errors.Add("firstSceneKey 不能为空");
            }

            return errors.Count == 0;
        }
    }
}
