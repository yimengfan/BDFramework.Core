using System;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 设置颗粒，基类
    /// </summary>
    public class SetGranularityBase : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 构建的上下文信息
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }


        public override string Category { get; }
        public override string ActiveStyle { get; }
        public override string InactiveStyle { get; }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            return new SetGranularityBase();
        }


        /// <summary>
        /// 是否包含依赖资产
        /// </summary>
        public bool IsIncludeDependAssets = false;
        
        /// <summary>
        /// 设置依赖等级
        /// </summary>
        public int SetLevel = (int) BuildAssetInfos.SetABPackLevel.Simple;
        
        protected NodeGUI selfNodeGUI;

        public override void OnDrawNodeGUIContent(NodeGUI node)
        { 
            //
            this.selfNodeGUI = node;
            //控制rect
            var rect = node.GetRect();
            if (rect.width < 250)
            {
                rect.width = 250;
                node.UpdateNodeRect();
            }

            //
           
            GUILayout.BeginHorizontal(GUILayout.Height(300));
            {

                GUILayout.Space(15);
               // GUILayout.Label(", GUILayout.Width(30));
                if (IsIncludeDependAssets)
                {
                    GUI.color = Color.green;
                }
                else
                {
                    GUI.color = Color.yellow;
                }

                GUILayout.Label("依赖: "+(IsIncludeDependAssets ? "True" : "False"), GUILayout.Width(80));
                GUI.color = GUI.backgroundColor;
                //space
                GUILayout.Space(1);

                //GUILayout.Label("Level: ", GUILayout.Width(30));
                if ((BuildAssetInfos.SetABPackLevel) this.SetLevel == BuildAssetInfos.SetABPackLevel.Lock || (BuildAssetInfos.SetABPackLevel) this.SetLevel == BuildAssetInfos.SetABPackLevel.Force)
                {
                    GUI.color = Color.green;
                }
                else
                {
                    GUI.color = Color.yellow;
                }

                GUILayout.Label("Level: "+((BuildAssetInfos.SetABPackLevel) this.SetLevel).ToString(),GUILayout.Width(80));
            }
            GUILayout.EndHorizontal();
            GUI.color = GUI.backgroundColor;
        }




        /// <summary>
        /// 预览结果 编辑器连线数据，但是build模式也会执行
        /// 这里只建议设置BuildingCtx的ab颗粒度
        /// </summary>
        /// <param name="target"></param>
        /// <param name="nodeData"></param>
        /// <param name="incoming"></param>
        /// <param name="connectionsToOutput"></param>
        /// <param name="outputFunc"></param>
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            bool isDirty = false;

            inspector.UpdateNodeName(node);
            EditorGUILayout.HelpBox("将该目录下所有文件,打包成一个AB!", MessageType.Info);

            GUILayout.Space(10);
            GUILayout.Label("包含依赖: " + (IsIncludeDependAssets ? "True" : "False"));
            //包含依赖
            bool ret = EditorGUILayout.Toggle("包含依赖:", IsIncludeDependAssets);
            if (this.IsIncludeDependAssets != ret)
            {
                this.IsIncludeDependAssets = ret;
                isDirty = true;
            }

            //设置级别
            var setRet = EditorGUILayout.EnumPopup("设置级别:", (BuildAssetInfos.SetABPackLevel) this.SetLevel);
            if (this.SetLevel != setRet.GetHashCode())
            {
                this.SetLevel = setRet.GetHashCode();
                isDirty = true;
            }

            if (isDirty)
            {
                Debug.Log("更新node!");
                AssetGraphTools.UpdateNodeGraph(node);
            }
        }


    }
}
