using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	class AssetGraphPostprocessor : AssetPostprocessor 
	{
        private Stack<AssetGraphController> m_controllers;
        private Stack<AssetPostprocessorContext> m_contexts;
        private Queue<AssetPostprocessorContext> m_ppQueue;

        private static AssetGraphPostprocessor s_postprocessor;
        public static AssetGraphPostprocessor Postprocessor {
            get {
                if (s_postprocessor == null) {
                    s_postprocessor = new AssetGraphPostprocessor ();
                    s_postprocessor.Init ();
                }
                return s_postprocessor;
            }
        }

        private void Init() {
            m_controllers = new Stack<AssetGraphController> ();
            m_contexts = new Stack<AssetPostprocessorContext> ();
            m_ppQueue = new Queue<AssetPostprocessorContext> ();

            EditorApplication.update += this.OnEditorUpdate;
        }

        private void OnEditorUpdate() {
            if (m_controllers.Count != 0 || m_contexts.Count != 0) {
                return;
            }

            // double check
            if (m_ppQueue.Count > 0 && m_contexts.Count == 0) {
                var ctx = m_ppQueue.Dequeue ();
                DoPostprocessWithContext (ctx);
            }
        }

        public void PushController(AssetGraphController c) {
            m_controllers.Push (c);
        }

        public void PushContext(AssetPostprocessorContext c) {
            m_contexts.Push (c);
        }

        public AssetGraphController GetCurrentGraphController() {
            if (m_controllers.Count == 0) {
                return null;
            }
            return m_controllers.Peek ();
        }

        public void PopController () {
            m_controllers.Pop ();
        }

        public void PopContext () {
            m_contexts.Pop ();
            if (m_contexts.Count == 1 && m_contexts.Peek().IsAdhoc) {
                m_contexts.Pop ();
            }
        }

        public void AddModifiedAsset(AssetReference a) {

            AssetPostprocessorContext ctx = null;

            if (m_contexts.Count == 0) {
                ctx = new AssetPostprocessorContext ();
                m_contexts.Push (ctx);
            } else {
                ctx = m_contexts.Peek ();
            }

            if (!ctx.ImportedAssets.Contains (a)) {
                ctx.ImportedAssets.Add (a);
            }

            AssetProcessEventRecord.GetRecord ().LogModify (a.assetDatabaseId);
        }

        static void OnPostprocessAllAssets (string[] importedAssets, 
            string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            return;
            Postprocessor.HandleAllAssets (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        private void HandleAllAssets(string[] importedAssets, 
            string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            for (int i=0; i<movedFromAssetPaths.Length; i++)
            {
                if (movedFromAssetPaths[i] == DataModel.Version2.Settings.Path.BasePath) {
                    DataModel.Version2.Settings.Path.ResetBasePath (movedAssets[i]);
                }
            }            

            foreach (string str in importedAssets)
            {
                AssetReferenceDatabase.GetReference(str)?.InvalidateTypeCache();
            }

            foreach (string str in deletedAssets) 
            {
                AssetReferenceDatabase.DeleteReference(str);
            }

            for (int i=0; i<movedAssets.Length; i++)
            {
                AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
            }
                
            var ctx = new AssetPostprocessorContext (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, m_contexts);

            if (!ctx.HasValidAssetToPostprocess()) {
                return;
            }

            // if modification happens inside graph, record it.
            if (m_controllers.Count > 0) {
                m_ppQueue.Enqueue (ctx);
                return;
            }

            DoPostprocessWithContext (ctx);
        }

        private void DoPostprocessWithContext(AssetPostprocessorContext ctx) {
            m_contexts.Push (ctx);
            NotifyAssetPostprocessorGraphs (ctx);
            AssetGraphEditorWindow.NotifyAssetsReimportedToAllWindows(ctx);
            m_contexts.Pop ();
        }

        private void NotifyAssetPostprocessorGraphs(AssetPostprocessorContext ctx) 
		{
			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            var executingGraphs = new List<Model.ConfigGraph> ();

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                if (graph != null && graph.UseAsAssetPostprocessor) {
                    bool isAnyNodeAffected = false;
					foreach(var n in graph.Nodes) {
                        isAnyNodeAffected |= n.Operation.Object.OnAssetsReimported(n, null, EditorUserBuildSettings.activeBuildTarget, ctx, true);
					}
                    if (isAnyNodeAffected) {
                        executingGraphs.Add (graph);
                    }
				}
			}

            if (executingGraphs.Count > 0) {
                if (executingGraphs.Count > 1) {
                    executingGraphs.Sort ((l, r) => l.ExecuteOrderPriority - r.ExecuteOrderPriority);
                }

                float currentCount = 0f;
                float totalCount = (float)executingGraphs.Sum (g => g.Nodes.Count);
                Model.NodeData lastNode = null;
                float graphStartTime = Time.realtimeSinceStartup;
                bool progressbarDisplayed = false;
                float progressBarShowDelaySec = 0.3f;

                Action<Model.NodeData, string, float> updateHandler = (node, message, progress) => {

                    if(lastNode != node) {
                        // do not add count on first node visit to 
                        // calcurate percantage correctly
                        if(lastNode != null) {
                            ++currentCount;
                        }
                        lastNode = node;

                        if(!progressbarDisplayed) {
                            if(Time.realtimeSinceStartup - graphStartTime > progressBarShowDelaySec) {
                                progressbarDisplayed = true;
                            }
                        }
                    }

                    if(progressbarDisplayed) {
                        var graphName = GetCurrentGraphController().TargetGraph.GetGraphName();

                        float currentNodeProgress = progress * (1.0f / totalCount);
                        float currentTotalProgress = (currentCount/totalCount) + currentNodeProgress;

                        string title = string.Format("Building {2}[{0}/{1}]", currentCount, totalCount, graphName);
                        string info  = $"Processing {node.Name}:{message}";

                        EditorUtility.DisplayProgressBar(title, info, currentTotalProgress);
                    }
                };

                foreach (var g in executingGraphs) {
                    AssetGraphUtility.ExecuteGraph (g, false, updateHandler);
                }

                if (progressbarDisplayed) {
                    EditorUtility.ClearProgressBar ();
                }

                AssetDatabase.Refresh();
            }
		}
	}
}
