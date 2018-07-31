using System.CodeDom;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Code.Core.BDFramework.SimpleGenCSharpCode;
using System.IO;

namespace BDFramework.Editor.UI
{

    public class EditorWindows_UIWorker : EditorWindow
    {
        private GameObject m_target;
        private string createName = "";
        private List<UITool_Attribute> itemList = new List<UITool_Attribute>();
        private string _root;

        //临时存储修改的数据 没有这层 改数据会直接更新
        private List<string> tpNameList = new List<string>();
        private List<bool> tpIsBindList = new List<bool>();
        private List<string> tpBindNameList = new List<string>();
        private string targetName;

        private Dictionary<string, string> pbPaths = new Dictionary<string, string>();


        private void CheckPath()
        {
            string path = Application.dataPath + "/Code/Game/Windows/";
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("失败", string.Format("文件夹不存在:{0}", path), "ok");
                Directory.CreateDirectory(path);
            }
            path = Application.dataPath + "/Code/Game/Windows/Window_MVC/";
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("失败", string.Format("文件夹不存在:{0}", path), "ok");
                Directory.CreateDirectory(path);
            }
        }

        private void OnGUI()
        {
            CheckPath();
            CheckTarget();
            GUILayout.BeginHorizontal();
            OnGUI_WindowSelect();
            Layout_DrawSeparatorV(Color.white, 2);
            OnGUI_EditorTransform();
            Layout_DrawSeparatorV(Color.white, 2);
            OnGUI_EditorButton();
            Layout_DrawSeparatorV(Color.white, 2);
            GUILayout.EndHorizontal();
            Layout_DrawSeparator(Color.white, 2);
        }

        private void ReGetLists()
        {
            //强刷避免增加item 数组越界 缺点修改的内容必须保存不然会重置 没办法TT
            if (!m_target) return;
            itemList.Clear();
            GetRegistViewItems(m_target.transform, ref itemList);
            _root = m_target.name;
            Editor_UITool.CloneValues(itemList, ref tpNameList, ref tpIsBindList, ref tpBindNameList);
        }

        void OnFocus()
        {
            ReGetLists();
        }

        void OnHierarchyChange()
        {
            ReGetLists();
        }

        void OnProjectChange()
        {
            ReGetLists();
        }

        private void CheckTarget()
        {
            if (m_target == null)
            {
                targetName = "";
                return;
            }
            if (targetName == m_target.name) return;
            targetName = m_target.name;
            itemList.Clear();
            GetRegistViewItems(m_target.transform, ref itemList);
            Editor_UITool.CloneValues(itemList, ref tpNameList, ref tpIsBindList, ref tpBindNameList);
        }

        Vector2 windowSelectPosition = Vector2.zero;
        Vector2 editorTransformPosition = Vector2.zero;
        private void OnGUI_WindowSelect()
        {
           
            GUILayout.BeginVertical();
            GUILayout.Label("已有窗口");
            Layout_DrawSeparator(Color.gray, 2);
            pbPaths.Clear();
            Editor_UITool.FindWindows(ref pbPaths);
            //竖列排版
            windowSelectPosition = GUILayout.BeginScrollView(windowSelectPosition,GUILayout.Width(220));
            {
                int count = 1;
                foreach (KeyValuePair<string, string> kv in pbPaths)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format( "{0}.{1}",count,kv.Key),GUILayout.Width(150));
                    if (GUILayout.Button("加载", GUILayout.Width(50)))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resource/Resources/" + kv.Value + ".prefab");
                        if (!prefab)
                        {
                            EditorUtility.DisplayDialog("警告", string.Format("未能在路径{0}下找到{1}prefab", "Assets/Resource/Resources/", kv.Value), "ok");
                        }
                        m_target = prefab;
                        //强制刷新
                        targetName = "";
                        CheckTarget();
                    }
                    GUILayout.EndHorizontal();

                }
               
            }
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }

        private void OnGUI_EditorTransform()
        {
            GUILayout.BeginVertical( GUILayout.Width(600));
            GUILayout.Label("组件列表");
            Layout_DrawSeparator(Color.gray, 2);
            itemList.Clear();
            if (!m_target)
            {
                GUILayout.EndVertical();
                return;
            }
            GetRegistViewItems(m_target.transform, ref itemList);
            //开始滚动列表
            editorTransformPosition = GUILayout.BeginScrollView(editorTransformPosition, GUILayout.Width(600));
            //竖列排版
            {
                GUILayout.BeginVertical();

                for (int i = 0; i < itemList.Count; i++)
                {
                    OnGUI_EditorRegistViewItem(itemList[i], i);
                }
                
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void OnGUI_EditorRegistViewItem(UITool_Attribute item, int index)
        {
            GUILayout.BeginVertical();
            OnGUI_EditorItemName(item, index);
            GUI.color = Color.white;
            OnGUI_EditorItemIsBindPath(item, index);
            OnGUI_EditorItemBindData(item, index);
            OnGUI_EditorItemAutoSetvalue(item, index);
            GUILayout.EndVertical();
            Layout_DrawSeparator(Color.gray, 2);

        }

        private void OnGUI_EditorItemBindData(UITool_Attribute item, int index)
        {
            GUILayout.BeginHorizontal();
            string tpName = tpNameList[index];
            GUILayout.Label("绑定数据:", GUILayout.Width(70));
            GUI.color = item.GenAttitude_BindData == tpBindNameList[index] ? Color.white : Color.yellow;
            tpBindNameList[index] = GUILayout.TextField(tpBindNameList[index], GUILayout.Width(180));
            if (GUILayout.Button("保存", GUILayout.Width(70)))
            {
                item.GenAttitude_BindData = tpBindNameList[index];
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
        }

        private void OnGUI_EditorItemIsBindPath(UITool_Attribute item, int index)
        {
            GUILayout.BeginHorizontal();
            GUI.color = item.GenAttibute_TranformPath == tpIsBindList[index] ? Color.white : Color.yellow;
            string tp = string.Format("  自动设置节点:{0}", tpIsBindList[index] ? Editor_UITool.GetBindPath(item.gameObject, targetName) : "");

            tpIsBindList[index] = GUILayout.Toggle(tpIsBindList[index], "", GUILayout.Width(10));
            //var oc = GUI.color;
            //GUI.color = Color.green;
            GUILayout.Label(tp);
            //GUI.color = oc;
            if (GUILayout.Button("保存", GUILayout.Width(70)))
            {
                item.GenAttibute_TranformPath = tpIsBindList[index];
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
        }
        
        private void OnGUI_EditorItemAutoSetvalue(UITool_Attribute item, int index)
        {
            GUILayout.BeginHorizontal();
            //var oc = GUI.color;
            //GUI.color = Color.green;
            GUILayout.Label("自动赋值,字段名:" + item.ClassFieldName);
         
            GUILayout.EndHorizontal();
        }

        private void OnGUI_EditorItemName(UITool_Attribute item, int index)
        {
            GUILayout.BeginHorizontal();
            string tpName = tpNameList[index];
            int count = (tpNameList.GroupBy(x => x).Where(x => x.Count() > 1)).Where(x => x.Key == tpName).ToList().Count();
            GUI.color = count > 0 ? Color.red : Color.white;
            if (GUI.color != Color.red)
            {
                GUI.color = item.name == tpNameList[index] ? Color.white : Color.yellow;
            }
            GUILayout.Label("Name:", GUILayout.Width(50));
            tpNameList[index] = GUILayout.TextField(tpNameList[index], GUILayout.Width(200));
            if (GUILayout.Button("保存", GUILayout.Width(70)))
            {
                item.name = tpNameList[index];
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
        }

        private void OnGUI_EditorButton()
        {
            GUILayout.BeginVertical(GUILayout.Height(520), GUILayout.Width(350));
            GUILayout.Label("操作");
            Layout_DrawSeparator(Color.gray, 2);
            GUILayout.Label("当前窗口Prefab:");
            m_target = (GameObject)EditorGUILayout.ObjectField(m_target, typeof(GameObject), true, GUILayout.Width(350));
            GUILayout.Space(10);
            GUILayout.Label("生成窗口代码:");
            createName = EditorGUILayout.TextField(createName, GUILayout.Height(15), GUILayout.Width(100));
            if (GUILayout.Button("创建mvc窗口代码"))
            {
                OnCreateButtonClick();
            }

            if (GUILayout.Button("全部保存", GUILayout.Width(100)))
            {
                OnSaveButtonClick();
            }
            GUILayout.EndVertical();
        }

        private void OnSaveButtonClick()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                UITool_Attribute item = itemList[i];
                item.name = tpNameList[i];
                item.GenAttibute_TranformPath = tpIsBindList[i];
                item.GenAttitude_BindData = tpBindNameList[i];
            }
        }


        private void OnCreateButtonClick()
        {
            string goName = createName.Trim();
            if (string.IsNullOrEmpty(goName))
            {
                EditorUtility.DisplayDialog("失败", "输入生成的cs文件名", "确认");
                return;
            }

            if (Editor_UITool.CheckRepeatName(itemList))
            {
                EditorUtility.DisplayDialog("失败", "组件命名重复", "确认");
                return;
            }

            Editor_UITool.CreateViewCS(itemList, goName, targetName);
            Editor_UITool.CreateContrlCS(itemList, goName);
            Editor_UITool.CreateWindowCS(goName, targetName);
            AssetDatabase.Refresh();
        }

        private void GetRegistViewItems(Transform ts, ref List<UITool_Attribute> itemList)
        {
            UITool_Attribute item = ts.GetComponent<UITool_Attribute>();
            if (item)
            {
                itemList.Add(item);
            }

            if (ts.childCount > 0)
            {
                for (int i = 0; i < ts.childCount; i++)
                {
                    GetRegistViewItems(ts.GetChild(i), ref itemList);
                }
            }
        }

        public static void Layout_DrawSeparator(Color color, float height = 4f)
        {

            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(height);
        }

        public static void Layout_DrawSeparatorV(Color color, float width = 4f)
        {

            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(width);
        }
    }
}