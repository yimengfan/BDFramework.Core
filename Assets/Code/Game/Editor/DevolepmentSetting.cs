using System;
using UnityEditor;
using UnityEngine;

namespace Code.Game.Editor
{
    /// <summary>
    /// 环境设置
    /// </summary>
    [InitializeOnLoad]
    static public class DevolepmentSetting
    {
        static Vector2Int screenView = new Vector2Int(1080, 1920);

        static DevolepmentSetting()
        {
            EditorApplication.update += SetGameViewScreen;
        }
        
        /// <summary>
        /// 设置gameview窗口大小
        /// </summary>
        static void SetGameViewScreen()
        {
            EditorApplication.update -= SetGameViewScreen;
            // var idx = GameViewUtils.FindSize(GameViewUtils.GetCurrentGroupType(), screenView.x, screenView.y);
            // if (idx < 0)
            // {
            //     GameViewUtils.AddCustomSize(GameViewUtils.GameViewSizeType.AspectRatio, GameViewUtils.GetCurrentGroupType(), screenView.x, screenView.y, "BDFrameDemoSize");
            //   
            // }
            var idx = GameViewUtils.FindSize(GameViewUtils.GetCurrentGroupType(), screenView.x, screenView.y);
            if (idx >= 0)
            {
                try
                {
                    GameViewUtils.SetSize(idx);
                }
                catch (Exception e)
                {
                    Debug.LogError("设置窗口[1080*1920]失败 :" + screenView);
                }
            }
            else
            {
                Debug.LogError("请设置窗口大小为:" + screenView);
            }
        }
        
    }
}
