// csharp
#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public partial class UIManager
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
      static  private void EnsureGameViewMatchesCanvas()
        {
            var uiroot = GameObject.Find("UIRoot")?.transform;
            if (!uiroot) return;

            var scaler = uiroot.GetComponent<CanvasScaler>();
            if (!scaler) return;

            var target = scaler.referenceResolution;
            if (target.x <= 0 || target.y <= 0) return;

            // 获取 GameView 尺寸
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gameViewType);
            if (gv == null) return;

            // 当前 Game 视图实际像素尺寸
            var currentSizeProp = gameViewType.GetProperty("currentGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var currentSizeObj = currentSizeProp?.GetValue(gv);
            var gwProp = currentSizeObj?.GetType().GetProperty("width");
            var ghProp = currentSizeObj?.GetType().GetProperty("height");
            var curW = (int)(gwProp?.GetValue(currentSizeObj) ?? 0);
            var curH = (int)(ghProp?.GetValue(currentSizeObj) ?? 0);

            // 若已匹配则无需操作
            if (curW == (int)target.x && curH == (int)target.y) return;

            // 访问 GameViewSizes
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
            var instanceProp = singletonType.GetProperty("instance");
            var sizesInstance = instanceProp.GetValue(null, null);

            var getGroup = sizesType.GetMethod("GetGroup");
            // 0:Standalone, 1:iOS, 2:Android 等，不同版本可能不同，这里选 Standalone
            var group = getGroup.Invoke(sizesInstance, new object[] { 0 });

            var groupType = group.GetType();
            var getDisplayTexts = groupType.GetMethod("GetDisplayTexts");
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var addCustomSize = groupType.GetMethod("AddCustomSize");
            var removeCustomSize = groupType.GetMethod("RemoveCustomSize");

            
            // 查找是否已有同尺寸
            int total = 0;

            #region Count管理
            var getTotalCount = groupType.GetMethod("GetTotalCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getTotalCount != null)
            {
                total= (int)getTotalCount.Invoke(group, null);
            }


            #endregion
          
          
            int foundIndex = -1;
            for (int i = 0; i < total; i++)
            {
                var gvs = getGameViewSize.Invoke(group, new object[] { i });
                var gvsType = gvs.GetType();
                var widthProp = gvsType.GetProperty("width");
                var heightProp = gvsType.GetProperty("height");
                int w = (int)widthProp.GetValue(gvs);
                int h = (int)heightProp.GetValue(gvs);
                if (w == (int)target.x && h == (int)target.y)
                {
                    foundIndex = i;
                    break;
                }
            }

            // 若未找到则创建一个固定分辨率
            if (foundIndex < 0)
            {
                
                var gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
                var gameViewSizeTypeEnum = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                var FixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");

                var ctor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                var name = $"Canvas_{(int)target.x}x{(int)target.y}";
                var newSize = ctor.Invoke(new object[] { FixedResolution, (int)target.x, (int)target.y, name });
                addCustomSize.Invoke(group, new object[] { newSize });

                // 重新统计，找到新索引
               total= (int)getTotalCount.Invoke(group, null);
                for (int i = 0; i < total; i++)
                {
                    var gvs = getGameViewSize.Invoke(group, new object[] { i });
                    var gvsType = gvs.GetType();
                    var widthProp = gvsType.GetProperty("width");
                    var heightProp = gvsType.GetProperty("height");
                    int w = (int)widthProp.GetValue(gvs);
                    int h = (int)heightProp.GetValue(gvs);
                    if (w == (int)target.x && h == (int)target.y)
                    {
                        foundIndex = i;
                        break;
                    }
                }
            }

            if (foundIndex >= 0)
            {
                // 设置 GameView 使用该尺寸
                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectedSizeIndexProp.SetValue(gv, foundIndex);
                
                Debug.Log($"自动设置GameView尺寸:{scaler.referenceResolution}");
            }
        }
    }
}
#endif
