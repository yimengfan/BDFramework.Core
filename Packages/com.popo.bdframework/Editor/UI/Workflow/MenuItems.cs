using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.UI.Workflow
{
    /// <summary>
    /// ui辅助按钮
    /// </summary>
    static public class MenuItems
    {
        [MenuItem("GameObject/UI工作流/1.创建UIPrefab",false, 1)]
        //[MenuItem("GameObject/建筑/升级Building_Mat组件2222", false, 20)]
        static public void  _01_createUIPefab()
        {
            var templeteGUID = "d25ba607a4a7bc740be5c7838d063260";
            var path = AssetDatabase.GUIDToAssetPath(templeteGUID);
            Debug.Log("templete path:" + path);

            //加载设置父节点:
            if (Selection.activeGameObject)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance_go = PrefabUtility.InstantiatePrefab(go) as GameObject;
                PrefabUtility.UnpackPrefabInstance(instance_go, unpackMode: PrefabUnpackMode.Completely, InteractionMode.UserAction);
                instance_go.transform.SetParent(Selection.activeGameObject.transform, false);
                
                //聚焦选择
                Selection.activeGameObject = instance_go; // 选择 GameObject
                SceneView.lastActiveSceneView.FrameSelected(); // 聚焦到选中的 GameObject
            }
            else
            {
                EditorUtility.DisplayDialog("警告", "未选择ui的父节点！！", "OK");
            }
        }

        [MenuItem("GameObject/UI工作流/2.创建SubWindow节点", false, 2)]
        //[MenuItem("GameObject/建筑/升级Building_Mat组件2222", false, 20)]
        static public void _01_createSubWindow()
        {
            
        }
    }
}
