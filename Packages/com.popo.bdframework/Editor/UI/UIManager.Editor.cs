// csharp
#if UNITY_EDITOR
using System;
using System.Reflection;
using BDFramework.Editor.Unity3dEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public  class UIManagerEditor
    {
        /// <summary>
        /// 编辑器下脚本重载后自动调整 GameView 尺寸
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnEditorScriptsReloaded()
        {
            EnsureGameViewMatchesCanvas();
        }

        /// <summary>
        /// 检查Gameview
        /// </summary>
        static private void EnsureGameViewMatchesCanvas()
        {
            var uiroot = GameObject.Find("UIRoot")?.transform;
            if (!uiroot) return;

            var scaler = uiroot.GetComponent<CanvasScaler>();
            if (!scaler) return;

            var target = scaler.referenceResolution;
            if (target.x <= 0 || target.y <= 0) return;
            
            GameViewEditorEX.SetGameviewSize(target);
        }
    }
}
#endif
